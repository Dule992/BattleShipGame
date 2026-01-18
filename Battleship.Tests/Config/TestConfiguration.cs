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
                .AddJsonFile(Path.Combine("Battleship.Tests/Config", "appsettings.json"), optional: false, reloadOnChange: false);

            RawConfiguration = builder.Build();

            GameConfig.BaseUrl = RawConfiguration["Game:BaseUrl"];
            GameConfig.OpponentConnectTimeoutSeconds = int.Parse(RawConfiguration["Game:OpponentConnectTimeoutSeconds"]);
            GameConfig.OverallGameTimeoutMinutes = double.Parse(RawConfiguration["Game:OverallGameTimeoutMinutes"]);

            PlaywrightConfig.Browser = RawConfiguration["Playwright:Browser"];
            PlaywrightConfig.Device = RawConfiguration["Playwright:Device"];
            PlaywrightConfig.Headless = bool.Parse(RawConfiguration["Playwright:Headless"]);
            PlaywrightConfig.SlowMoMilliseconds = int.Parse(RawConfiguration["Playwright:SlowMoMilliseconds"]);
            PlaywrightConfig.DefaultTimeoutMilliseconds = int.Parse(RawConfiguration["Playwright:DefaultTimeoutMilliseconds"]);

            // Set Allure results directory (use absolute path based on output directory)
            var allureResultsDir = RawConfiguration["Allure:ResultsDirectory"] ?? "allure-results";
            if (!Path.IsPathRooted(allureResultsDir))
            {
                // Convert relative path to absolute path based on test output directory
                AllureConfig.ResultsDirectory = Path.Combine(AppContext.BaseDirectory, allureResultsDir);
            }
        }
    }
}
