namespace VMPerPath;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.Remoting;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Polly;
using Polly.Retry;
using Polly.Timeout;

using VMBroker.Configuration;
using VMBroker.Extensions;
using VMBroker.Management;

using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;

/// <summary>
///
/// </summary>
public class Startup
{

    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, AllocatedVirtualMachine> _pathPrefixToVM = new();

    /// <summary>
    ///
    /// </summary>
    /// <param name="configuration"></param>
    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    ///
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = false;
                options.SingleLine = true;
                options.TimestampFormat = "hh:mm:ss:fff ";
            });
        });

        // Add the configuration handlers

        services.Configure<VirtualMachines>(_configuration.GetSection("VMBroker"));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<VirtualMachines>, ValidateConfiguration>());
        services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<VirtualMachines>>().Value);

        // Add the Virtual Machine Managment services

        services.AddSingleton<VirtualMachineRecycleChannel>();
        services.AddSingleton<VirtualMachineRecyler>();
        services.AddSingleton<AvailableVirtualMachines>();
        services.AddHostedService<AvailableVirtualMachineManagerService>();
        services.AddHttpClient();
        services.AddHttpForwarder();

        // Add the per path recycler.

        services.AddSingleton<ConcurrentDictionary<string, AllocatedVirtualMachine>>(_pathPrefixToVM);
        services.AddHostedService<VirtualMachineRecycleService>();
    }

    /// <summary>
    ///
    /// </summary>
    public void Configure(IApplicationBuilder applicationBuilder, IWebHostEnvironment env, IHttpForwarder forwarder, ILoggerFactory loggerFactory, AvailableVirtualMachines availableVirtualMachines)
    {
        {
            _ = applicationBuilder ?? throw new ArgumentNullException(nameof(applicationBuilder));
            var forwarderRetryPolicy =
               Policy.HandleResult<ForwarderError>(e => e != ForwarderError.None)
                    .RetryAsync(5);

            var VirtualMachines = applicationBuilder.ApplicationServices.GetRequiredService<VirtualMachines>();
            var port = VirtualMachines.ApplicationPort;

            var logger = loggerFactory?.CreateLogger("VMPerPath")!;
            var transformer = new RequestTransformer();
            var requestOptions = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(1) };

            applicationBuilder.UseRouting();
            applicationBuilder.UseEndpoints(endpoints =>
            {
                var socketsHttpHandler = new SocketsHttpHandler()
                {
                    UseProxy = false,
                    AllowAutoRedirect = false,
                    AutomaticDecompression = DecompressionMethods.None,
                    UseCookies = false
                };

                endpoints.Map("{pathprefix}/{**catch-all}", async httpContext =>
                    {
                        var httpClient = new HttpMessageInvoker(socketsHttpHandler);
                        var pathprefix = httpContext.Request.RouteValues["pathprefix"]!.ToString();
                        var stopWatch = Stopwatch.StartNew();

                        logger.Trace("Getting VM");
                        var inUseVirtualMachine = _pathPrefixToVM.GetOrAdd(pathprefix!, (_) => new AllocatedVirtualMachine(availableVirtualMachines.GetAvailableVirtualMachine())).InUseVirtualMachine;
                        logger.Trace("Got VM");

                        var error = await forwarderRetryPolicy.ExecuteAsync(async () =>
                        {
                            return await forwarder.SendAsync(httpContext, $"http://{inUseVirtualMachine.IPAddress}:{port}", httpClient, requestOptions, transformer).ConfigureAwait(false);

                        }).ConfigureAwait(false);

                        stopWatch.Stop();

                        var elapsed = stopWatch.Elapsed;
                        var msg = $"Request Execution Time: {elapsed.TotalSeconds} Seconds";
                        logger.Trace(msg);

                        if (error != ForwarderError.None)
                        {
                            var errorFeature = httpContext.Features.Get<IForwarderErrorFeature>();
                            logger.Error("Error forwarding request", errorFeature?.Exception);
                        }

                    });
            });
        }
    }
}
