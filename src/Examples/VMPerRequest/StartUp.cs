
namespace VMPerRequest;
using System.Runtime.Remoting;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Polly;
using Polly.Retry;
using Polly.Timeout;

using System.Diagnostics;

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
        services.AddSingleton<VirtualMachineManager>();
        services.AddSingleton<AvailableVirtualMachines>();
        services.AddHostedService<AvailableVirtualMachineManagerService>();
        services.AddHttpClient();
        services.AddHttpForwarder();
    }

    /// <summary>
    ///     
    /// </summary>
    public void Configure(IApplicationBuilder applicationBuilder, IWebHostEnvironment env, IHttpForwarder forwarder, ILoggerFactory loggerFactory, AvailableVirtualMachines availableVirtualMachines)
    {
        {
            _= applicationBuilder ?? throw new ArgumentNullException(nameof(applicationBuilder));
            var forwarderRetryPolicy =
               Policy.HandleResult<ForwarderError>(e => e != ForwarderError.None)
                    .RetryAsync(5);

            var logger = loggerFactory?.CreateLogger("VMPerRequest")!;
            var transformer = HttpTransformer.Default;
            var requestOptions = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(1) };

            var VirtualMachines = applicationBuilder.ApplicationServices.GetRequiredService<VirtualMachines>();
            var port = VirtualMachines.ApplicationPort;

            applicationBuilder.UseRouting();
//#pragma warning disable CA2000 // Dispose objects before losing scope

            applicationBuilder.UseEndpoints(endpoints =>
            {
                var socketsHttpHandler = new SocketsHttpHandler()
                {
                    UseProxy = false,
                    AllowAutoRedirect = false,
                    AutomaticDecompression = DecompressionMethods.None,
                    UseCookies = false,
                    PooledConnectionLifetime = TimeSpan.FromSeconds(1),
                };

                var httpClient = new HttpMessageInvoker(socketsHttpHandler);

                endpoints.Map("/{**catch-all}", async httpContext =>
                    {
                        
                        var stopWatch = Stopwatch.StartNew();
                        using var inUseVirtualMachine = availableVirtualMachines.GetAvailableVirtualMachine();

                        var error = await forwarderRetryPolicy.ExecuteAsync(async () =>
                        {
                            return await forwarder.SendAsync(httpContext, $"http://{inUseVirtualMachine.IPAddress}:{port}", httpClient, requestOptions, transformer).ConfigureAwait(false);     
                           
                        }).ConfigureAwait(false);
                        stopWatch.Stop();
                        var elapsed = stopWatch.Elapsed;
                        var msg=$"Request Execution Time: {elapsed.TotalSeconds} Seconds";
                        logger.Trace(msg);
                        if (error != ForwarderError.None)
                        {
                            var errorFeature = httpContext.Features.Get<IForwarderErrorFeature>();
                            logger.Error("Error forwarding request", errorFeature?.Exception);
                        }
                    });
// #pragma warning restore CA2000 // Dispose objects before losing scope
            });
        }
    }
}
