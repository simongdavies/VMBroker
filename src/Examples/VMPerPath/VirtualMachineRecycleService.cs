namespace VMPerPath;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using VMBroker.Extensions;
using VMBroker.Management;

/// <summary>
///
/// </summary>
public class VirtualMachineRecycleService : BackgroundService
{
    private readonly ConcurrentDictionary<string, AllocatedVirtualMachine> _pathPrefixToVM;
    private readonly ILogger<VirtualMachineRecycleService> _logger;

    /// <summary>
    ///
    /// </summary>
    /// <param name="pathPrefixToVM"></param>
    /// <param name="logger"></param>
    public VirtualMachineRecycleService(ConcurrentDictionary<string, AllocatedVirtualMachine> pathPrefixToVM, ILogger<VirtualMachineRecycleService> logger)
    {
        _pathPrefixToVM = pathPrefixToVM;
        _logger = logger;
    }
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Trace("Started Service");
        var delay = TimeSpan.FromSeconds(10);
        Action<object?> recycleVM = async (vm) =>
        {
            InUseVirtualMachine inUseVirtualMachine = (InUseVirtualMachine)vm!;
            try
            {
                // Delay removing a VM for 10 seconds to allow any in-flight requests to complete.
                _logger.Trace($"Waiting for requests to complete before removing VM Id {inUseVirtualMachine.Id}");
                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                _logger.Trace($"Removing VM Id {inUseVirtualMachine.Id}");
                inUseVirtualMachine.Dispose();
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.Error($"Error Recycling VM", ex);
            }
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var count_of_vms_recycled = 0;
                foreach (var item in _pathPrefixToVM)
                {
                    var VM = item.Value;

                    if (VM.IsExpired())
                    {
                        _logger.Trace($"Recycling VM Id {VM.InUseVirtualMachine.Id}");
                        var removed = _pathPrefixToVM.TryRemove(item.Key, out _);

                        if (removed)
                        {
                            await Task.Factory.StartNew(recycleVM, VM.InUseVirtualMachine, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).ConfigureAwait(false);
                            count_of_vms_recycled++;
                        }
                        else
                        {
                            _logger.Error($"Failed to remove VM Id {VM.InUseVirtualMachine.Id}");
                        }

                    }

                }
                _logger.Trace($"Recycled {count_of_vms_recycled} VMs");
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.Error($"Error Recycling VM", ex);
            }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
        }
        _logger.Trace("Cancellation Requested");
    }
}
