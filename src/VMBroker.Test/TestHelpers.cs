namespace VMBroker.Test;

class TestHelpers
{
    protected internal static TestServer CreateTestServer(string[] filenames, Mock<ILoggerFactory> mockLoggerFactory, Func<WebHostBuilderContext, TestStartUp> testStartUpFactory = null)
        => new(CreateWebHostBuilder(filenames, mockLoggerFactory, testStartUpFactory));


    protected internal static (Mock<ILogger>, Mock<ILoggerFactory>) CreateMockLoggerAndFactory()
    {
        var mockLogger = new Mock<ILogger>();
        mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => mockLogger.Object);
        return (mockLogger, mockLoggerFactory);
    }


    protected internal static IWebHostBuilder CreateWebHostBuilder(string[] filenames, Mock<ILoggerFactory> mockLoggerFactory, Func<WebHostBuilderContext, TestStartUp> testStartUpFactory = null)
       => new WebHostBuilder()
         .ConfigureAppConfiguration((context, builder) =>
         {
             foreach (var filename in filenames)
             {
                 builder.AddJsonFile(filename, false);

             }
         })
         .ConfigureServices(services =>
         {
             services.AddSingleton<ILoggerFactory>(mockLoggerFactory.Object);
         })
         .UseStartup<TestStartUp>((context) =>
         {
             if (testStartUpFactory is null)
             {
                 return new TestStartUp(context.Configuration);
             }
             return testStartUpFactory(context);
         });

}
