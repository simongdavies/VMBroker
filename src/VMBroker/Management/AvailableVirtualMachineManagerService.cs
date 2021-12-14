namespace VMBroker.Management;

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using VMBroker.Extensions;

/// <summary>
/// This class contains a service that watches a channel of used VMs and recycles them..
/// </summary>
public class AvailableVirtualMachineManagerService : BackgroundService
{
    private readonly VirtualMachineRecycleChannel _channel;
    private readonly VirtualMachineRecyler _virtualMachineManager;
    private readonly ILogger<AvailableVirtualMachineManagerService> _logger;

    //TODO: This should be configurable

    private const int InitialTaskCount = 15;
    private const int QueueLimit = 2;
    private const int MaxTaskCount = 20;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvailableVirtualMachineManagerService"/> class.
    /// </summary>
    public AvailableVirtualMachineManagerService(VirtualMachineRecycleChannel channel, VirtualMachineRecyler virtualMachineManager, ILogger<AvailableVirtualMachineManagerService> logger)
    {
        _channel = channel;
        _logger = logger;
        _virtualMachineManager = virtualMachineManager;
    }

    /// <inheritdoc /> 
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var currentTaskCount = 0;

        async void RecycleVM(object? taskNum)
        {
            var num = (int)taskNum!;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.Trace($"Task {num} Waiting to recycle a VM.");
                    var virtualMachineDetails = await _channel.ReadAsync(stoppingToken).ConfigureAwait(false);
                    _logger.Trace($"Task {num} Recycling VM Task Id: {virtualMachineDetails.Id}");
                    await _virtualMachineManager.RecycleVMAsync(virtualMachineDetails).ConfigureAwait(false);
                    _logger.Trace($"Task {num} Recycling VM Task Id: {virtualMachineDetails.Id} - Complete");
                }
#pragma warning disable CA1031
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    _logger.Error($"Task {num} Error Recycling VM", ex);
                }
            }
            _logger.Trace($"Task {num} Exited.");
        }

        for (currentTaskCount = 0; currentTaskCount < InitialTaskCount; currentTaskCount++)
        {
            _logger.Trace($"Virtual Machine Recycle Task {currentTaskCount} Started.");
            await Task.Factory.StartNew(RecycleVM, currentTaskCount, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).ConfigureAwait(false);
        }

        _logger.Trace($"Started {currentTaskCount} Recycle VM Tasks");

        while (!stoppingToken.IsCancellationRequested)
        {

            if (currentTaskCount >= MaxTaskCount)
            {
                _logger.Trace($"Reached Maximum VM Recycle Task Count: {MaxTaskCount}");
                await Task.Delay(TimeSpan.FromMilliseconds(-1), stoppingToken).ConfigureAwait(false);
            }
            else
            {
                _logger.Trace($"Current VM Task Count: {currentTaskCount} Queue Size: {_channel.Count}");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken).ConfigureAwait(false);
            }

            if (_channel.Count > QueueLimit)
            {
                _logger.Trace($"Starting additional VM Recycle Task {currentTaskCount}. as Queue Size: {_channel.Count} is greater than Limit: {QueueLimit}");
                await Task.Factory.StartNew(RecycleVM, currentTaskCount, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).ConfigureAwait(false);
                currentTaskCount++;
                _logger.Trace($"Started Additional Recycle VM Task currentTaskCount: {currentTaskCount}");
            }

        }
        _logger.Trace("Cancellation Requested");
    }
}
