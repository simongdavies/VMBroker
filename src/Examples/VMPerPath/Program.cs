namespace VMPerPath;

/// <summary>
/// 
/// </summary>
public static class Program
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="args"></param>
    public static void Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args);
        builder.ConfigureWebHostDefaults((webBuilder) => webBuilder.UseStartup<Startup>());
        var app = builder.Build();
        app.Run();
    }
}
