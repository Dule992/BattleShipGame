using Microsoft.Extensions.Configuration;

namespace BattleShipGame.Battleship.Tests.Config
{
    public static class TestConfiguration
    {
        public static IConfigurationRoot RawConfiguration { get; }

        static TestConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(Path.Combine("Battleship.Tests\\Config", "appsettings.json"), optional: false, reloadOnChange: false);
            
            RawConfiguration = builder.Build();

            GameConfig.BaseUrl = RawConfiguration["Game:BaseUrl"];
            GameConfig.OpponentConnectTimeoutSeconds = int.Parse(RawConfiguration["Game:OpponentConnectTimeoutSeconds"]);
            GameConfig.OverallGameTimeoutMinutes = double.Parse(RawConfiguration["Game:OverallGameTimeoutMinutes"]);

            PlaywrightConfig.Browser = RawConfiguration["Playwright:Browser"];
            PlaywrightConfig.Device = RawConfiguration["Playwright:Device"];
            PlaywrightConfig.Headless = bool.Parse(RawConfiguration["Playwright:Headless"]);
            PlaywrightConfig.SlowMoMilliseconds = int.Parse(RawConfiguration["Playwright:SlowMoMilliseconds"]);
            PlaywrightConfig.DefaultTimeoutMilliseconds = int.Parse(RawConfiguration["Playwright:DefaultTimeoutMilliseconds"]);

            LoggingConfig.MinimumLevel = RawConfiguration["Logging:MinimumLevel"];
            LoggingConfig.LogDirectory = RawConfiguration["Logging:LogDirectory"];
            LoggingConfig.FileName = RawConfiguration["Logging:FileName"];

            AllureConfig.ResultsDirectory = RawConfiguration["Allure:ResultsDirectory"];
        }
    }
}
