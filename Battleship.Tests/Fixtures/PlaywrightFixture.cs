using BattleShipGame.Battleship.Tests.Config;
using BattleShipGame.Battleship.Tests.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Battleship.Tests.Fixtures
{
    [SetUpFixture]
    public class PlaywrightFixture
    {
        public static IPlaywright Playwright { get; private set; } = default!;
        public static IBrowser Browser { get; private set; } = default!;
        public static ILoggerFactory LoggerFactory => TestLogging.LoggerFactory;

        [OneTimeSetUp]
        public async Task GlobalSetup()
        {
            var cfg = TestConfiguration.Playwright;

            var logger = LoggerFactory.CreateLogger<PlaywrightFixture>();
            logger.LogInformation("Initialising Playwright. Headless={Headless}, SlowMo={SlowMo}ms",
                cfg.Headless, cfg.SlowMoMilliseconds);

            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = cfg.Headless,
                SlowMo = cfg.SlowMoMilliseconds
            });
        }

        [OneTimeTearDown]
        public async Task GlobalTeardown()
        {
            await Browser.CloseAsync();
            Playwright.Dispose();
        }
    }
}
