using BattleShipGame.Battleship.Tests.Config;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace BattleShipGame.Battleship.Tests.Logging
{
    public static class TestLogging
    {
        public static ILoggerFactory LoggerFactory { get; }
        public static ILogger<T> CreateLogger<T>() => LoggerFactory.CreateLogger<T>();

        static TestLogging()
        {
            Directory.CreateDirectory(LoggingConfig.LogDirectory);

            var level = LoggingConfig.MinimumLevel?.ToLowerInvariant() switch
            {
                "debug" => LogEventLevel.Debug,
                "verbose" => LogEventLevel.Verbose,
                "warning" => LogEventLevel.Warning,
                "error" => LogEventLevel.Error,
                "fatal" => LogEventLevel.Fatal,
                _ => LogEventLevel.Information
            };

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(level)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    path: Path.Combine(LoggingConfig.LogDirectory, LoggingConfig.FileName),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .CreateLogger();

            LoggerFactory = new SerilogLoggerFactory(Log.Logger);
        }
    }
}
