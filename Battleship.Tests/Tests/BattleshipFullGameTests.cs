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

namespace Battleship.Tests.Tests
{
    [TestFixture]
    [AllureNUnit]
    [AllureSuite("Battleship")]
    [AllureSubSuite("Full Game E2E")]
    public class BattleshipFullGameTests
    {
        private IPage _page = default!;
        private IBrowserContext _context = default!;
        private BattleshipGameService _gameService = default!;
        private ILogger<BattleshipGameService> _logger = default!;

        private string AllureResultsDirectory => TestConfiguration.Allure.ResultsDirectory;

        [SetUp]
        public async Task SetUp()
        {
            Directory.CreateDirectory(AllureResultsDirectory);

            _context = await PlaywrightFixture.Browser.NewContextAsync();
            _page = await _context.NewPageAsync();

            _logger = TestLogging.CreateLogger<BattleshipGameService>();

            var gamePage = new BattleshipGamePage(_page);
            _gameService = new BattleshipGameService(gamePage, _logger);
        }

        [TearDown]
        public async Task TearDown()
        {
            try
            {
                if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
                {
                    // Screenshot on failure
                    var screenshotPath = Path.Combine(
                        AllureResultsDirectory,
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
                        AllureResultsDirectory,
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
                await _context.CloseAsync();
            }
        }

        [Test]
        [AllureTag("e2e", "battleship")]
        [AllureSeverity(SeverityLevel.critical)]
        [AllureDescription("Plays a full game of Battleship. Test PASS only if game ends in victory.")]
        public async Task FullGame_ShouldPassOnlyOnVictory()
        {
            var gameCfg = TestConfiguration.Game;

            (GameResult result, FailureReason failureReason) = await _gameService.PlayFullGameAsync(
                gameCfg.BaseUrl,
                overallTimeout: TimeSpan.FromMinutes(gameCfg.OverallGameTimeoutMinutes),
                opponentConnectTimeout: TimeSpan.FromSeconds(gameCfg.OpponentConnectTimeoutSeconds));

            if (result == GameResult.Victory)
            {
                Assert.Pass("Game ended in victory – test successful.");
            }

            Assert.Fail($"Game did not end in victory. Result: {result}, reason: {failureReason}");
        }
    }
}
