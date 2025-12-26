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
            var cfg = TestConfiguration.Logging;

            Directory.CreateDirectory(cfg.LogDirectory);

            var level = cfg.MinimumLevel?.ToLowerInvariant() switch
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
                    path: Path.Combine(cfg.LogDirectory, cfg.FileName),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .CreateLogger();

            LoggerFactory = new SerilogLoggerFactory(Log.Logger);
        }
    }
}
