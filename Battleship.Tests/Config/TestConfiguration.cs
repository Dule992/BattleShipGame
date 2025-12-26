using Microsoft.Extensions.Configuration;

namespace BattleShipGame.Battleship.Tests.Config
{
    public static class TestConfiguration
    {
        public static IConfigurationRoot RawConfiguration { get; }
        public static GameConfig Game { get; }
        public static PlaywrightConfig Playwright { get; }
        public static LoggingConfig Logging { get; }
        public static AllureConfig Allure { get; }

        static TestConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("Config/appsettings.json", optional: false, reloadOnChange: false);
            
            RawConfiguration = builder.Build();

            GameConfig? gameConfig = RawConfiguration.GetSection("Game") as GameConfig;
            Game = gameConfig;
            PlaywrightConfig? playwright = RawConfiguration.GetSection("Playwright") as PlaywrightConfig;
            Playwright = playwright;
            LoggingConfig? logging = RawConfiguration.GetSection("Logging") as LoggingConfig;
            Logging = logging;
            AllureConfig? allure = RawConfiguration.GetSection("Allure") as AllureConfig;
            Allure = allure;
        }
    }
}
