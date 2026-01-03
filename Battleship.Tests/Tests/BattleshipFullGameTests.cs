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
using System.Text;

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
            
            // Ensure Allure results directory exists
            if (!string.IsNullOrEmpty(AllureConfig.ResultsDirectory))
            {
                Directory.CreateDirectory(AllureConfig.ResultsDirectory);
            }
            else
            {
                // Fallback to default location if not configured
                var defaultResultsDir = Path.Combine(AppContext.BaseDirectory, "allure-results");
                Directory.CreateDirectory(defaultResultsDir);
                AllureConfig.ResultsDirectory = defaultResultsDir;
            }
            
            // Configure Allure to use the results directory
            AllureLifecycle.Instance.CleanupResultDirectory();
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

        [TearDown]
        public async Task TearDown()
        {
            try
            {
                var testStatus = TestContext.CurrentContext.Result.Outcome.Status;
                var testName = TestContext.CurrentContext.Test.Name;
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

                // Always attach move log (for both pass and fail)
                if (_gameService?.MoveLog != null && _gameService.MoveLog.Count > 0)
                {
                    var moveLogPath = Path.Combine(
                        AllureConfig.ResultsDirectory ?? AppContext.BaseDirectory,
                        $"movelog_{testName}_{timestamp}.txt");

                    await File.WriteAllLinesAsync(moveLogPath, _gameService.MoveLog.ToArray());

                    AllureApi.AddAttachment(
                        "Move Log",
                        "text/plain",
                        moveLogPath);
                }

                // Attach screenshot for failures or always (configurable)
                if (_page != null)
                {
                    var screenshotPath = Path.Combine(
                        AllureConfig.ResultsDirectory ?? AppContext.BaseDirectory,
                        $"screenshot_{testName}_{timestamp}.png");

                    try
                    {
                        await _page.ScreenshotAsync(new PageScreenshotOptions
                        {
                            Path = screenshotPath,
                            FullPage = true
                        });

                        if (testStatus == NUnit.Framework.Interfaces.TestStatus.Failed)
                        {
                            AllureApi.AddAttachment(
                                "Failure Screenshot",
                                "image/png",
                                screenshotPath);
                        }
                        else
                        {
                            // Optionally attach screenshot for passed tests too
                            AllureApi.AddAttachment(
                                "Test Screenshot",
                                "image/png",
                                screenshotPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail if screenshot fails
                        TestContext.WriteLine($"Failed to capture screenshot: {ex.Message}");
                    }
                }

                // Add test result summary
                if (testStatus == NUnit.Framework.Interfaces.TestStatus.Failed)
                {
                    var failureMessage = TestContext.CurrentContext.Result.Message;
                    AllureApi.AddAttachment(
                        "Test Failure Details",
                        "text/plain",
                        Encoding.UTF8.GetBytes(failureMessage ?? "Test failed"));
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Error in TearDown: {ex.Message}");
            }
            finally
            {
                await Cleanup();
            }
        }

        private async Task Cleanup()
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
            await AllureApi.Step("Initialize and start Battleship game", async () =>
            {
                _logger.LogInformation("Starting full game test");
            });

            (GameResult result, FailureReason failureReason) = await AllureApi.Step(
                "Play full Battleship game",
                async () => await _gameService.PlayFullGameAsync(
                    GameConfig.BaseUrl,
                    overallTimeout: TimeSpan.FromMinutes(GameConfig.OverallGameTimeoutMinutes),
                    opponentConnectTimeout: TimeSpan.FromSeconds(GameConfig.OpponentConnectTimeoutSeconds)));

            await AllureApi.Step("Verify game result", async () =>
            {
                // Log the actual result for debugging
                _logger.LogInformation("Game completed. Result: {Result}, FailureReason: {FailureReason}", result, failureReason);
                
                // Add game result as attachment
                var resultSummary = $"Game Result: {result}\nFailure Reason: {failureReason}\nTotal Moves: {_gameService.MoveLog.Count}";
                AllureApi.AddAttachment(
                    "Game Result Summary",
                    "text/plain",
                    Encoding.UTF8.GetBytes(resultSummary));

                // Explicitly check for Victory - ensure we're comparing enum values correctly
                bool isVictory = result == GameResult.Victory;
                
                // Log the actual enum value and comparison result for debugging
                _logger.LogInformation("Result enum value: {Result}, IsVictory: {IsVictory}", 
                    result, isVictory);
                
                // Only pass if result is explicitly Victory
                if (isVictory && result == GameResult.Victory)
                {
                    _logger.LogInformation("✓ Victory confirmed - Test will PASS");
                    AllureApi.AddAttachment(
                        "Victory Confirmation",
                        "text/plain",
                        Encoding.UTF8.GetBytes("✓ Game ended in VICTORY - Test PASSED"));
                    
                    Assert.Pass("Game ended in victory – test successful.");
                }
                else
                {
                    // Log non-victory result with explicit details
                    _logger.LogWarning("✗ Game did NOT end in victory. Result: {Result}, Reason: {Reason} - Test will FAIL", 
                        result, failureReason);
                    
                    var failureDetails = $"Game did not end in victory.\nResult: {result}\nReason: {failureReason}";
                    AllureApi.AddAttachment(
                        "Failure Details",
                        "text/plain",
                        Encoding.UTF8.GetBytes(failureDetails));
                    
                    Assert.Fail(failureDetails);
                }
            });
        }
    }
}
