namespace VMBroker.Test;
public class TestStartUp
{
    private readonly string _sectionName;
    public IConfiguration Configuration { get; }

    public TestStartUp(IConfiguration configuration, string sectionName = null)
    {
        _sectionName = sectionName ?? "VMBroker";
        Configuration = configuration ?? throw new ArgumentException("Configuration is null"); ;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<VirtualMachines>(Configuration.GetSection(_sectionName));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<VirtualMachines>, ValidateConfiguration>());
        services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<VirtualMachines>>().Value);
        services.AddRouting();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
    }
}
