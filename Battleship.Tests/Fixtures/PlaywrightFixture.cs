using BattleShipGame.Battleship.Tests.Config;
using Microsoft.Playwright;
using NUnit.Framework;
using BrowserType = BattleShipGame.Battleship.Tests.Config.Enums.BrowserType;

namespace BattleShipGame.Battleship.Tests.Fixtures
{
    [SetUpFixture]
    public class PlaywrightFixture
    {
        private static IBrowser _browser;
        private static IBrowserContext _browserContext;
        public static IBrowser Browser => _browser;
        public static IBrowserContext BrowserContext => _browserContext;

        public static async Task InitPlaywright()
        {
            var browserType = (BrowserType)Enum.Parse(typeof(BrowserType), PlaywrightConfig.Browser);

            var playwright = await Playwright.CreateAsync().ConfigureAwait(false);
            _browser = await CreateBrowserInstance(playwright, browserType).ConfigureAwait(false);
            _browserContext = await CreateBrowserContext(playwright, _browser).ConfigureAwait(false);

            Console.WriteLine($"Initialising Playwright. Headless={PlaywrightConfig.Headless}, SlowMo={PlaywrightConfig.SlowMoMilliseconds}ms");

            CreateBrowserInstance(playwright, BrowserType.CHROME).GetAwaiter().GetResult();
        }

        /// <param name="playwright">Playwright object instance</param>
        /// <param name="browserType">An option from BrowserType enum {CHROMIUM, CHROME, MSEDGE, FIREFOX, SAFARI}</param>
        private static async Task<IBrowser> CreateBrowserInstance(IPlaywright playwright, BrowserType browserType)
        {
            var options = CreateBrowserOptions();
            return browserType switch
            {
                BrowserType.CHROMIUM => await playwright.Chromium.LaunchAsync(options).ConfigureAwait(false),
                BrowserType.CHROME => await playwright.Chromium.LaunchAsync(CreateBrowserOptions(BrowserType.CHROME.ToString().ToLower())).ConfigureAwait(false),
                BrowserType.MSEDGE => await playwright.Chromium.LaunchAsync(CreateBrowserOptions(BrowserType.MSEDGE.ToString().ToLower())).ConfigureAwait(false),
                BrowserType.FIREFOX => await playwright.Firefox.LaunchAsync(options).ConfigureAwait(false),
                BrowserType.SAFARI => await playwright.Webkit.LaunchAsync(options),
                _ => throw new ApplicationException("Unsupported BrowserType was provided: " + browserType),
            };
        }

        public static async Task<IPage> CreatePageAsync()
        {
            return await _browserContext.NewPageAsync();
        }

        public static async Task CloseBrowserAsync()
        {
            foreach (var context in Browser.Contexts)
            {
                await context.CloseAsync().ConfigureAwait(false);
            }

            await Browser.CloseAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Setup browser's Launch options
        /// </summary>
        private static BrowserTypeLaunchOptions CreateBrowserOptions(string channel = null)
        {
            return new BrowserTypeLaunchOptions
            {
                DownloadsPath = Directory.GetCurrentDirectory(),
                Timeout = PlaywrightConfig.SlowMoMilliseconds,
                Headless = PlaywrightConfig.Headless,
                Channel = channel
            };
        }

        /// <summary>
        /// Creates a browser context
        /// </summary>
        /// <param name="playwright">A playwright instance</param>
        /// <param name="browser">A browser instance</param>
        private static async Task<IBrowserContext> CreateBrowserContext(IPlaywright playwright, IBrowser browser)
        {
            var contextOptions = new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize
                {
                    Width = 1920,
                    Height = 1080
                },
                IgnoreHTTPSErrors = true,
            };

            var deviceName = PlaywrightConfig.Device;

            if (!string.IsNullOrWhiteSpace(deviceName))
            {
                contextOptions = playwright.Devices[deviceName];
            }

            var context = await browser.NewContextAsync(contextOptions).ConfigureAwait(false);

            context.SetDefaultNavigationTimeout(PlaywrightConfig.SlowMoMilliseconds);
            context.SetDefaultTimeout(PlaywrightConfig.DefaultTimeoutMilliseconds);

            return context;
        }
    }
}
