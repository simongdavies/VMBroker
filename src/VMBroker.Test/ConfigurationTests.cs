namespace VMBroker.Test;

using VMBroker.Test.Extensions;
public class ConfigurationTests
{
    [Fact]
    public void Test_Handles_No_Configuration()
    {
        var (mockLogger, mockLoggerFactory) = TestHelpers.CreateMockLoggerAndFactory();
        var webHostBuilder = TestHelpers.CreateWebHostBuilder(new string[] { "testdata/emptyAppSettings.json" }, mockLoggerFactory);
        var exception = Record.Exception(() => ActivatorUtilities.GetServiceOrCreateInstance<VirtualMachines>(webHostBuilder.Build().Services));
        Assert.NotNull(exception);
        Assert.IsType<OptionsValidationException>(exception);
        Assert.Equal($"No virtual machines configured.{Environment.NewLine}", exception.Message);

        webHostBuilder = TestHelpers.CreateWebHostBuilder(new string[] { "testdata/emptyVirtualMachinesAppSettings.json" }, mockLoggerFactory);
        exception = Record.Exception(() => ActivatorUtilities.GetServiceOrCreateInstance<VirtualMachines>(webHostBuilder.Build().Services));
        Assert.NotNull(exception);
        Assert.IsType<OptionsValidationException>(exception);
        Assert.Equal($"No virtual machines configured.{Environment.NewLine}", exception.Message);

        webHostBuilder = TestHelpers.CreateWebHostBuilder(new string[] { "testdata/missingVirtualMachinesAppSettings.json" }, mockLoggerFactory);
        exception = Record.Exception(() => ActivatorUtilities.GetServiceOrCreateInstance<VirtualMachines>(webHostBuilder.Build().Services));
        Assert.NotNull(exception);
        Assert.IsType<OptionsValidationException>(exception);
        Assert.Equal($"No virtual machines configured.{Environment.NewLine}", exception.Message);
    }

    [Fact]
    public void Test_Handles_Missing_Configuration()
    {
        var (mockLogger, mockLoggerFactory) = TestHelpers.CreateMockLoggerAndFactory();
        var webHostBuilder = TestHelpers.CreateWebHostBuilder(new string[] { "testdata/allPropertiesMissingAppSettings.json" }, mockLoggerFactory);
        var exception = Record.Exception(() => ActivatorUtilities.GetServiceOrCreateInstance<VirtualMachines>(webHostBuilder.Build().Services));
        Assert.NotNull(exception);
        Assert.IsType<OptionsValidationException>(exception);
        Assert.Equal($"No virtual machines configured.{Environment.NewLine}", exception.Message);

        webHostBuilder = TestHelpers.CreateWebHostBuilder(new string[] { "testdata/missingNameAppSettings.json" }, mockLoggerFactory);
        exception = Record.Exception(() => ActivatorUtilities.GetServiceOrCreateInstance<VirtualMachines>(webHostBuilder.Build().Services));
        Assert.NotNull(exception);
        Assert.IsType<OptionsValidationException>(exception);
        Assert.Equal($"VirtualMachineDetails Name Property is required.{Environment.NewLine}", exception.Message);

        webHostBuilder = TestHelpers.CreateWebHostBuilder(new string[] { "testdata/missingIdAppSettings.json" }, mockLoggerFactory);
        exception = Record.Exception(() => ActivatorUtilities.GetServiceOrCreateInstance<VirtualMachines>(webHostBuilder.Build().Services));
        Assert.NotNull(exception);
        Assert.IsType<OptionsValidationException>(exception);
        Assert.Equal($"VirtualMachineDetails Id Property is required.{Environment.NewLine}", exception.Message);

        webHostBuilder = TestHelpers.CreateWebHostBuilder(new string[] { "testdata/missingPipeNameAppSettings.json" }, mockLoggerFactory);
        exception = Record.Exception(() => ActivatorUtilities.GetServiceOrCreateInstance<VirtualMachines>(webHostBuilder.Build().Services));
        Assert.NotNull(exception);
        Assert.IsType<OptionsValidationException>(exception);
        Assert.Equal($"VirtualMachineDetails PipeName Property is required.{Environment.NewLine}", exception.Message);

        webHostBuilder = TestHelpers.CreateWebHostBuilder(new string[] { "testdata/missingIpAddressAppSettings.json" }, mockLoggerFactory);
        exception = Record.Exception(() => ActivatorUtilities.GetServiceOrCreateInstance<VirtualMachines>(webHostBuilder.Build().Services));
        Assert.NotNull(exception);
        Assert.IsType<OptionsValidationException>(exception);
        Assert.Equal($"VirtualMachineDetails IpAddress Property is required.{Environment.NewLine}", exception.Message);
    }

    [Fact]
    public void Test_Handles_Invalid_Configuration_Data()
    {
        var (mockLogger, mockLoggerFactory) = TestHelpers.CreateMockLoggerAndFactory();
        var webHostBuilder = TestHelpers.CreateWebHostBuilder(new string[] { "testdata/invalidIdAppSettings.json" }, mockLoggerFactory);
        var exception = Record.Exception(() => ActivatorUtilities.GetServiceOrCreateInstance<VirtualMachines>(webHostBuilder.Build().Services));
        Assert.NotNull(exception);
        Assert.IsType<OptionsValidationException>(exception);
        Assert.Equal($"VirtualMachineDetails Id Property is not a valid GUID.{Environment.NewLine}", exception.Message);

        webHostBuilder = TestHelpers.CreateWebHostBuilder(new string[] { "testdata/invalidIpAddressAppSettings.json" }, mockLoggerFactory);
        exception = Record.Exception(() => ActivatorUtilities.GetServiceOrCreateInstance<VirtualMachines>(webHostBuilder.Build().Services));
        Assert.NotNull(exception);
        Assert.IsType<OptionsValidationException>(exception);
        Assert.Equal($"VirtualMachineDetails IpAddress Property is not a valid IP address.{Environment.NewLine}", exception.Message);
    }

    [Fact]
    public void Test_Handles_Valid_Configuration_Data()
    {
        var (mockLogger, mockLoggerFactory) = TestHelpers.CreateMockLoggerAndFactory();
        var webHostBuilder = TestHelpers.CreateWebHostBuilder(new string[] { "testdata/ValidAppSettings1.json" }, mockLoggerFactory);
        VirtualMachines virtualMachines = null;
        var exception = Record.Exception(() => virtualMachines = ActivatorUtilities.GetServiceOrCreateInstance<VirtualMachines>(webHostBuilder.Build().Services));
        Assert.Null(exception);
        Assert.NotNull(virtualMachines);
        Assert.Single(virtualMachines.VirtualMachineDetails);
        Assert.Equal("VM1", virtualMachines.VirtualMachineDetails[0].Name);
        Assert.Equal("eca3bda4-3a11-43d7-af13-24ee576dd354", virtualMachines.VirtualMachineDetails[0].Id);
        Assert.Equal("TestPipe", virtualMachines.VirtualMachineDetails[0].PipeName);
        Assert.Equal("172.20.174.194", virtualMachines.VirtualMachineDetails[0].IpAddress);

        webHostBuilder = TestHelpers.CreateWebHostBuilder(new string[] { "testdata/ValidAppSettings2.json" }, mockLoggerFactory);
        exception = Record.Exception(() => virtualMachines = ActivatorUtilities.GetServiceOrCreateInstance<VirtualMachines>(webHostBuilder.Build().Services));
        Assert.Null(exception);
        Assert.NotNull(virtualMachines);
        Assert.Equal(2, virtualMachines.VirtualMachineDetails.Count);
        Assert.Equal("VM1", virtualMachines.VirtualMachineDetails[0].Name);
        Assert.Equal("eca3bda4-3a11-43d7-af13-24ee576dd354", virtualMachines.VirtualMachineDetails[0].Id);
        Assert.Equal("TestPipe", virtualMachines.VirtualMachineDetails[0].PipeName);
        Assert.Equal("172.20.174.194", virtualMachines.VirtualMachineDetails[0].IpAddress);

        Assert.Equal("VM2", virtualMachines.VirtualMachineDetails[1].Name);
        Assert.Equal("eca3bda4-3a11-43d7-af13-24ee576dd323", virtualMachines.VirtualMachineDetails[1].Id);
        Assert.Equal("TestPipe1", virtualMachines.VirtualMachineDetails[1].PipeName);
        Assert.Equal("172.20.174.196", virtualMachines.VirtualMachineDetails[1].IpAddress);
    }
}
