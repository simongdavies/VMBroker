namespace VMPerPath;

using VMBroker.Management;
/// <summary>
/// 
/// </summary>
public class AllocatedVirtualMachine

{
    private InUseVirtualMachine _inUseVirtualMachine;
    private DateTime _lastUsedTime;

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool IsExpired()
    {
        return DateTime.UtcNow.Subtract(_lastUsedTime).TotalSeconds > 90;
    }

    /// <summary>
    ///     
    /// </summary>
    public DateTime LastUsedTime => _lastUsedTime;
    
    /// <summary>
    /// 
    /// </summary>
    /// <value></value>
    public InUseVirtualMachine InUseVirtualMachine
    {
        get
        {
            _lastUsedTime = DateTime.UtcNow;
            return _inUseVirtualMachine;
        }
    }

    internal AllocatedVirtualMachine(InUseVirtualMachine inUseVirtualMachine)
    {
        _inUseVirtualMachine = inUseVirtualMachine;
        _lastUsedTime = DateTime.UtcNow;
    }
}