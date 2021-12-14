#pragma warning disable CA1002 
namespace VMBroker.Configuration;
/// <summary>
/// This class contains a list of virtual machines to be managed by the VM Broker
/// </summary>
public class VirtualMachines
{
    /// <summary>
    /// Gets or sets the list of virtual machines to be used by the VM Broker
    /// </summary>
    public List<VirtualMachineDetails> VirtualMachineDetails { get; } = new List<VirtualMachineDetails>();

    /// <summary>
    /// Gets or sets the ApplicationPort that the application running in the VM is listening on
    /// </summary>
    public int ApplicationPort { get; set; } = 3000;

    /// <summary>
    /// Gets or sets the health probe path that the application running in the VM is using, the broker will use this to determine if the VM is healthy
    /// </summary>
    public string HealthEndPoint { get; set; } = "/healthz";
}
#pragma warning restore CA1002
