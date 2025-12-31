using Allure.Net.Commons;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using Battleship.Tests.Fixtures;
using Battleship.UI.Services;
using BattleShipGame.Battleship.Tests.Config;
using BattleShipGame.Battleship.Tests.Logging;
using BattleShipGame.Pages;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using NUnit.Framework;
using System.Runtime.CompilerServices;

namespace Battleship.Tests.Tests
{
    [TestFixture]
    [AllureNUnit]
    [AllureSuite("Battleship")]
    [AllureSubSuite("Full Game E2E")]
    public class BattleshipFullGameTests
    {
        private IPage _page;
        private BattleshipGamePage _battleShipGamePage;
        private BattleshipGameService _gameService;
        private ILogger<BattleshipGameService> _logger;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            RuntimeHelpers.RunClassConstructor(typeof(TestConfiguration).TypeHandle);
            Directory.CreateDirectory(AllureConfig.ResultsDirectory);
        }

        [SetUp]
        public async Task SetUp()
        {
            await PlaywrightFixture.InitPlaywright();
            await PlaywrightFixture.BrowserContext.ClearCookiesAsync();
            _page = await PlaywrightFixture.CreatePageAsync();
            _battleShipGamePage = new BattleshipGamePage(_page);

            _logger = TestLogging.CreateLogger<BattleshipGameService>();
            _gameService = new BattleshipGameService(_battleShipGamePage, _logger);
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            try
            {
                if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
                {
                    // Screenshot on failure
                    var screenshotPath = Path.Combine(
                        AllureConfig.ResultsDirectory,
                        $"screenshot_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");

                    await _page.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Path = screenshotPath,
                        FullPage = true
                    });

                    AllureApi.AddAttachment(
                        "Failure screenshot",
                        "image/png",
                        screenshotPath);

                    // Move log attachment
                    var moveLogPath = Path.Combine(
                        AllureConfig.ResultsDirectory,
                        $"movelog_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt");

                    await File.WriteAllLinesAsync(moveLogPath, _gameService.MoveLog.ToArray());

                    // Attach move log to Allure using AddAttachment from Allure.Commons
                    AllureApi.AddAttachment(
                        "Move log",
                        "text/plain",
                        moveLogPath);
                }
            }
            finally
            {
                await Cleanup();
            }
        }

        [TearDown]
        public async Task Cleanup()
        {
            if (_page != null)
            {
                await PlaywrightFixture.CloseBrowserAsync();
                await _page.CloseAsync();
            }
        }

        [Test]
        [AllureTag("e2e", "battleship")]
        [AllureSeverity(SeverityLevel.critical)]
        [AllureDescription("Plays a full game of Battleship. Test PASS only if game ends in victory.")]
        public async Task FullGame_ShouldPassOnlyOnVictory()
        {
            (GameResult result, FailureReason failureReason) = await _gameService.PlayFullGameAsync(
                GameConfig.BaseUrl,
                overallTimeout: TimeSpan.FromMinutes(GameConfig.OverallGameTimeoutMinutes),
                opponentConnectTimeout: TimeSpan.FromSeconds(GameConfig.OpponentConnectTimeoutSeconds));

            if (result == GameResult.Victory)
            {
                Assert.Pass("Game ended in victory – test successful.");
            }

            Assert.Fail($"Game did not end in victory. Result: {result}, reason: {failureReason}");
        }
    }
}
