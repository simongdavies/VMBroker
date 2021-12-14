#pragma warning disable CA1416

namespace VMBroker.Management;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Management;
using System.Management.Automation;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using VMBroker.Configuration;
using VMBroker.Extensions;

/// <summary>
/// This class recycles VMs by resetting them and then makes them available for use again.
/// </summary>
public class VirtualMachineRecyler
{
    private readonly AvailableVirtualMachines _availableVirtualMachines;

    private readonly ILogger<VirtualMachineRecyler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualMachineRecyler" /> class.
    /// </summary>
    public VirtualMachineRecyler(AvailableVirtualMachines availableVirtualMachines, ILogger<VirtualMachineRecyler> logger)
    {
        _logger = logger;
        _availableVirtualMachines = availableVirtualMachines;
    }

    /// <summary>
    /// Recycles a VM by resetting it and then making it available for use again.
    /// </summary>
    /// <param name="virtualMachineDetails"></param>
    /// <returns></returns>
    public async Task RecycleVMAsync(VirtualMachineDetails virtualMachineDetails)
    {
        try
        {
            _ = virtualMachineDetails ?? throw new ArgumentNullException(nameof(virtualMachineDetails));
            _logger.Trace($"Resetting virtual machine name {virtualMachineDetails.Name} id {virtualMachineDetails.Id}");
            await UpdateVMAsync(virtualMachineDetails.Id, VMState.Reset).ConfigureAwait(false);
            _logger.Trace($"Reset virtual machine name {virtualMachineDetails.Name} id {virtualMachineDetails.Id}");
            await _availableVirtualMachines.MakeVMAvailable(virtualMachineDetails).ConfigureAwait(false);

            //TODO: check the IP address has not changed.
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            //TODO: Replace the Virtual Machine with a new one.
            _logger.Error("Error resetting virtual machine", ex);
        }
    }

    private Task UpdateVMAsync(string id, VMState vmState)
    {
        var taskCompletionSource = new TaskCompletionSource();
        try
        {
            var scope = new ManagementScope(@"\\.\root\virtualization\v2", null);
            var vmWQL = $"SELECT * FROM Msvm_ComputerSystem WHERE Name=\"{id}\"";
            var query = new SelectQuery(vmWQL);
            using var vmSearcher = new ManagementObjectSearcher(scope, query);
            using var vmCollection = vmSearcher.Get();
            if (vmCollection.Count == 0)
            {
                _logger.Error($"Reset VM - No virtual machine found with id {id}");
            }
            else
            {
                var vm = vmCollection.OfType<ManagementObject>().First();

                _logger.Trace($"Changing state of VM with id {id} to {vmState}");
                var parms = vm.GetMethodParameters("RequestStateChange");
                parms["RequestedState"] = (int)vmState;
                ManagementOperationObserver managementOperationObserver = new();
                managementOperationObserver.Completed += (_, args) =>
                {
                    if (args.Status == ManagementStatus.NoError)
                    {
                        _logger.Trace($"Change State Request virtual machine with id {id} succeeded");
                        taskCompletionSource.SetResult();
                    }
                    else
                    {
#pragma warning disable CA2201 // Do not raise reserved exception types
                        taskCompletionSource.SetException(new ApplicationException($"Change state of virtual machine with id {id} failed with error {args.Status}"));
#pragma warning restore CA2201 // Do not raise reserved exception types
                    }
                };

                vm.InvokeMethod(managementOperationObserver, "RequestStateChange", parms, null);
            }
        }

#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
        {
            taskCompletionSource.SetException(ex);
        }
        return taskCompletionSource.Task;
    }

    enum VMState
    {
        Start = 2,
        Stop = 3,
        Reset = 11,
    }

}
#pragma warning restore CA1416
