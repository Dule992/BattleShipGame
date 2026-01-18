using Allure.Net.Commons;
using BattleShipGame.Battleship.Tests.Config;
using BattleShipGame.Battleship.Tests.Fixtures;
using BattleShipGame.Pages;
using BattleShipGame.Services;
using Microsoft.Playwright;
using NUnit.Framework;
using Reqnroll;
using Reqnroll.BoDi;
using System.Runtime.CompilerServices;
using System.Text;

namespace BattleShipGame.Battleship.Tests.Tests
{
    [Binding]
    public class Hooks
    {
        public readonly IObjectContainer _objectContainer;
        private IPage _page;
        private BattleshipGamePage _battleShipGamePage;
        private BattleshipGameService _gameService;

        public Hooks(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            RuntimeHelpers.RunClassConstructor(typeof(TestConfiguration).TypeHandle);
            // set results dir early (absolute)
            Environment.SetEnvironmentVariable("ALLURE_RESULTS_DIRECTORY", AllureConfig.ResultsDirectory);
            // clear previous results once
            AllureLifecycle.Instance.CleanupResultDirectory();
        }

        [BeforeScenario]
        public async Task BeforeScenario()
        {
            await PlaywrightFixture.InitPlaywright();
            await PlaywrightFixture.BrowserContext.ClearCookiesAsync();
            _page = await PlaywrightFixture.CreatePageAsync();
            _battleShipGamePage = new BattleshipGamePage(_page);

            _gameService = new BattleshipGameService(_battleShipGamePage);

            _objectContainer.RegisterInstanceAs(_page);
            _objectContainer.RegisterInstanceAs(_battleShipGamePage);
            _objectContainer.RegisterInstanceAs(_gameService);
        }

        [AfterScenario]
        public async Task AfterScenario()
        {
            try
            {
                var testStatus = TestContext.CurrentContext.Result.Outcome.Status;
                var testName = TestContext.CurrentContext.Test.Name;
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

                // Attach screenshot for failures or always (configurable)
                if (_page != null)
                {
                    var screenshotPath = Path.Combine(
                        AllureConfig.ResultsDirectory,
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
    }
}
