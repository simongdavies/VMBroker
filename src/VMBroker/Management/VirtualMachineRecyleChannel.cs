namespace VMBroker.Management;

using System.Globalization;
using System.Threading.Channels;

using VMBroker.Configuration;
/// <summary>
///  This class provides a channel containing virtual machines waiting to be recycled.
/// </summary>
public class VirtualMachineRecycleChannel
{
    private readonly Channel<VirtualMachineDetails> _channel;
    /// <summary>
    ///  The number of virtual machines waiting to be recycled.
    /// </summary>
    public int Count => _channel.Reader.CanCount ? _channel.Reader.Count : -1;
    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualMachineRecycleChannel"/> class.
    /// </summary>
    public VirtualMachineRecycleChannel() : this(1024) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualMachineRecycleChannel"/> class.
    /// </summary>
    public VirtualMachineRecycleChannel(int capacity)
    {
        var options = new BoundedChannelOptions(capacity) { FullMode = BoundedChannelFullMode.Wait };
        _channel = Channel.CreateBounded<VirtualMachineDetails>(options);
    }

    /// <summary>
    /// Stores details of a virtual machine that needs to be recycled.
    /// </summary>
    /// <param name="virtualMachineDetails"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task Write(VirtualMachineDetails virtualMachineDetails, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(virtualMachineDetails, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the details of a virtual machine that needs to be recycled.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<VirtualMachineDetails> ReadAsync(CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Trys to write the details of a virtual machine that needs to be recycled.
    /// </summary>
    /// <param name="virtualMachineDetails("></param>
    /// <returns></returns>
    public bool TryWrite(VirtualMachineDetails virtualMachineDetails)
    {
        return _channel.Writer.TryWrite(virtualMachineDetails);
    }

    /// <summary>
    ///  Trys to read the details of a virtual machine that needs to be recycled.
    /// </summary>
    /// <returns></returns>
    public (bool, VirtualMachineDetails) TryRead()
    {
        VirtualMachineDetails virtualMachineDetails;
        var result = _channel.Reader.TryRead(out virtualMachineDetails!);
        return (result, virtualMachineDetails);
    }
}
