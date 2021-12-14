namespace VMBroker.Management;
using System.Threading.Channels;

using VMBroker.Configuration;
/// <summary>
/// This class contains details of a virtual machine that has been allocated to serve a requests.
/// When it is disposed it queues the details of the VM for recycling.
/// </summary>
public class InUseVirtualMachine : IDisposable
{
    private readonly VirtualMachineDetails _virtualMachineDetails;
    private readonly VirtualMachineRecycleChannel _channel;
    /// <summary>
    /// Gets the IP address of the virtual machine
    /// </summary>
    public IPAddress IPAddress { get; }

    /// <summary>
    /// Gets the Id of the virtual machine
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InUseVirtualMachine"/> class.
    /// </summary>
    public InUseVirtualMachine(VirtualMachineDetails virtualMachineDetails, VirtualMachineRecycleChannel channel)
    {
        _channel = channel;
        _virtualMachineDetails = virtualMachineDetails;
        IPAddress = IPAddress.Parse(_virtualMachineDetails.IpAddress);
        Id = _virtualMachineDetails.Id;
    }

    // TODO: Implement AsyncDisposable
    /// <summary>
    /// Dispose implementation.
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _channel.Write(_virtualMachineDetails).Wait();
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
}
