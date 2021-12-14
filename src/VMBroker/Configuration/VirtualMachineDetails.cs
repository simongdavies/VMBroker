namespace VMBroker.Configuration;
/// <summary>
/// This class contains details of a virtual machine to be managed by the VM Broker
/// </summary>
public class VirtualMachineDetails
{
    /// <summary>
    /// Gets or sets the name of the virtual machine
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the id of the virtual machine
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Mac Address of the virtual machine
    /// </summary>
    public string PipeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the IP Address of the virtual machine
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

}
