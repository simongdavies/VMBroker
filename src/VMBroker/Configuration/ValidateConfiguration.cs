namespace VMBroker.Configuration;
using Microsoft.Extensions.Options;

/// <summary>
/// Validates the configuration.
/// </summary>
public class ValidateConfiguration : IValidateOptions<VirtualMachines>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string name, VirtualMachines options)
    {
        StringBuilder result = new();
        _ = options?.VirtualMachineDetails ?? throw new ArgumentException("VirtualMachineDetails configuration is required.");

        if (options.VirtualMachineDetails.Count == 0)
        {
            result.AppendLine($"No virtual machines configured.");
            return ValidateOptionsResult.Fail(result.ToString());
        }

        foreach (var vm in options.VirtualMachineDetails)
        {

            if (string.IsNullOrEmpty(vm.Name))
            {
                result.AppendLine("VirtualMachineDetails Name Property is required.");
            }

            if (string.IsNullOrEmpty(vm.PipeName))
            {
                result.AppendLine("VirtualMachineDetails PipeName Property is required.");
            }

            if (string.IsNullOrEmpty(vm.IpAddress))
            {
                result.AppendLine("VirtualMachineDetails IpAddress Property is required.");
            }

            if (!string.IsNullOrEmpty(vm.IpAddress) && IPAddress.TryParse(vm.IpAddress, out var _) == false)
            {
                result.AppendLine($"VirtualMachineDetails IpAddress Property is not a valid IP address.");
            }

            if (string.IsNullOrEmpty(vm.Id))
            {
                result.AppendLine("VirtualMachineDetails Id Property is required.");
            }

            if (!string.IsNullOrEmpty(vm.Id) && Guid.TryParse(vm.Id, out var _) == false)
            {
                result.AppendLine("VirtualMachineDetails Id Property is not a valid GUID.");
            }

        }

        return result.Length == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(result.ToString());

    }
}
