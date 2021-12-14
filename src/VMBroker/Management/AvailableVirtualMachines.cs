namespace VMBroker.Management;
using System.Collections.Concurrent;
using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;
using Polly.Timeout;

using VMBroker.Configuration;
using VMBroker.Extensions;

/// <summary>
/// This class manages a queue of available virtual machines.
/// </summary>
public class AvailableVirtualMachines : IDisposable
{
    private readonly BlockingCollection<VirtualMachineDetails> _queue = new();
    private readonly ILogger<AvailableVirtualMachines> _logger;
    private readonly VirtualMachineRecycleChannel _channel;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _httpRetryPolicy;
    private readonly AsyncTimeoutPolicy _timeoutPolicy;
    private readonly int _applicationPort;
    private readonly string _healthEndPoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvailableVirtualMachines"/> class.
    /// </summary>
    public AvailableVirtualMachines(VirtualMachines virtualMachines, VirtualMachineRecycleChannel channel, ILogger<AvailableVirtualMachines> logger, IHttpClientFactory httpClientFactory)
    {
        _ = virtualMachines ?? throw new ArgumentNullException(nameof(virtualMachines));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _applicationPort = virtualMachines.ApplicationPort;
        _healthEndPoint = virtualMachines.HealthEndPoint;

        foreach (var virtualMachineDetails in virtualMachines.VirtualMachineDetails)
        {
            logger.Trace($"Adding virtual machine name {virtualMachineDetails.Name} id {virtualMachineDetails.Id} to available virtual machines queue");
            _queue.Add(virtualMachineDetails);
        }

        _httpRetryPolicy =
            Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .Or<TimeoutRejectedException>()
                .Or<HttpRequestException>()
                .RetryAsync(10);

        _timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(2));

    }

    /// <summary>
    /// Makes a VM available for use.  
    /// </summary>
    public async Task MakeVMAvailable(VirtualMachineDetails virtualMachineDetails)
    {
        _ = virtualMachineDetails ?? throw new ArgumentNullException(nameof(virtualMachineDetails));
        // check VM is healthy
        if (await CheckVMIsHealthyAsync(virtualMachineDetails).ConfigureAwait(false))
        {
            _logger.Trace($"Adding virtual machine name {virtualMachineDetails.Name} id {virtualMachineDetails.Id} to available virtual machines queue");
            _queue.Add(virtualMachineDetails);
        }
        else
        {
            // TODO: handle unhealthy VM
            _logger.Trace($"Virtual machine name {virtualMachineDetails.Name} id {virtualMachineDetails.Id} is not healthy");
        }
    }

    /// <summary>
    ///  Returns an available virtual machine.
    /// </summary>
    /// <returns>InUseVirtualMachine</returns>
    public InUseVirtualMachine GetAvailableVirtualMachine()
    {

        var stopWatch = Stopwatch.StartNew();
        var count = _queue.Count;
        var virtualMachineDetails = _queue.Take();
        stopWatch.Stop();
        var elapsed = stopWatch.Elapsed;
        var msg = $"{count} VMs available - took {elapsed.TotalSeconds} Seconds to get a VM";
        _logger.Trace(msg);
        _logger.Trace($"Providing available machine name {virtualMachineDetails.Name} id {virtualMachineDetails.Id} for use");
        return new InUseVirtualMachine(virtualMachineDetails, _channel);
    }

    /// <summary>
    /// Dispose implementation.
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _queue.Dispose();
        }
    }

    /// <summary>
    /// IDisposable dispose implementation.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private async Task<bool> CheckVMIsHealthyAsync(VirtualMachineDetails virtualMachineDetails)
    {

        try
        {
            _logger.Trace($"Waiting for VM Ready {virtualMachineDetails.Name} id {virtualMachineDetails.Id} IPaddress {virtualMachineDetails.IpAddress}");
            using var httpClient = _httpClientFactory.CreateClient("VMRecycle");
            var uri = new Uri($"http://{virtualMachineDetails.IpAddress}:{_applicationPort}/{_healthEndPoint}");
            _logger.Trace($"Sending request to {uri}");

            _ = await _httpRetryPolicy
                    .ExecuteAsync(async () => await
                        _timeoutPolicy.ExecuteAsync(async (token) =>
                            await httpClient.GetAsync(uri, token).ConfigureAwait(false), CancellationToken.None).ConfigureAwait(false)).ConfigureAwait(false);
            _logger.Trace($"VM Ready {virtualMachineDetails.Name} id {virtualMachineDetails.Id} IPaddress {virtualMachineDetails.IpAddress}");

            return true;
        }

#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            _logger.Error("Error waiting for app ready", ex);

            // TODO deal with the error;
        }
        return false;
    }
}
