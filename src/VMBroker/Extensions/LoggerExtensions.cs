#pragma warning disable CS1591
#pragma warning disable IDE1006
namespace VMBroker.Extensions;
using Microsoft.Extensions.Logging;
public static class LoggerExtensions
{
    private static readonly Action<ILogger, string, Exception> trace = LoggerMessage.Define<string>(
                LogLevel.Trace,
                    new EventId(1, nameof(Trace)),
                    "{Message}");

    private static readonly Action<ILogger, string, Exception> error = LoggerMessage.Define<string>(
                LogLevel.Trace,
                    new EventId(2, nameof(Error)),
                    "{Message}");

    public static void Trace(this ILogger logger, string message, Exception? ex = null)
    {
        trace(logger, message, ex!);
    }

    public static void Error(this ILogger logger, string message, Exception? ex = null)
    {
        error(logger, message, ex!);
    }
}

#pragma warning restore IDE1006
#pragma warning restore CS1591
