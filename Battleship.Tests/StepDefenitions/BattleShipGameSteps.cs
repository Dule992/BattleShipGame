using Allure.Net.Commons;
using BattleShipGame.Battleship.Tests.Config;
using BattleShipGame.Services;
using NUnit.Framework;
using Reqnroll;
using Reqnroll.BoDi;
using System.Text;

namespace BattleShipGame.Battleship.Tests.StepDefenitions
{
    [Binding]
    public class BattleShipGameSteps
    {
        private readonly IObjectContainer _objectContainer;
        private BattleshipGameService _gameService;

        public BattleShipGameSteps(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
            _gameService = _objectContainer.Resolve<BattleshipGameService>();
        }

        [Given("I open Battle Ship online game")]
        public async Task GivenIOpenBattleShipOnlineGame()
        {
            Console.WriteLine("Starting full game test");
            await _gameService.OpenTheGame(GameConfig.BaseUrl);
        }

        [When("I click play button to start game")]
        public async Task WhenIClickPlay()
        {
            await _gameService.InitializeGameAsync();
        }

        [Then("I will play a game until it's finished with victory")]
        public async Task ThenIWillWaitPlayAGameUntilItsFinishedWithVictory()
        {
            (GameResult result, FailureReason failureReason) = await AllureApi.Step(
                "Play full Battleship game",
                async () => await _gameService.PlayFullGameAsync(
                    overallTimeout: TimeSpan.FromMinutes(GameConfig.OverallGameTimeoutMinutes),
                    opponentConnectTimeout: TimeSpan.FromSeconds(GameConfig.OpponentConnectTimeoutSeconds)));

            await AllureApi.Step("Verify game result", () =>
            {
                // Log the actual result for debugging
                Console.WriteLine($"Game completed. Result: {result}, FailureReason: {failureReason}");

                // Add game result as attachment
                var resultSummary = $"Game Result: {result}\nFailure Reason: {failureReason}";
                AllureApi.AddAttachment(
                    "Game Result Summary",
                    "text/plain",
                    Encoding.UTF8.GetBytes(resultSummary));

                // Explicitly check for Victory - ensure we're comparing enum values correctly
                bool isVictory = result == GameResult.Victory;

                // Log the actual enum value and comparison result for debugging
                Console.WriteLine($"Result enum value: {result}, IsVictory: {isVictory}");

                // Only pass if result is explicitly Victory
                if (isVictory && result == GameResult.Victory)
                {
                    Console.WriteLine("✓ Victory confirmed - Test will PASS");
                    AllureApi.AddAttachment(
                        "Victory Confirmation",
                        "text/plain",
                        Encoding.UTF8.GetBytes("✓ Game ended in VICTORY - Test PASSED"));

                    Assert.That(result.Equals(GameResult.Victory), "Game did not end in Victory as expected.");
                }
                else
                {
                    // Log non-victory result with explicit details
                    Console.WriteLine($"✗ Game did NOT end in victory. Result: {result}, Reason: {failureReason} - Test will FAIL");

                    var failureDetails = $"Game did not end in victory.\nResult: {result}\nReason: {failureReason}";
                    AllureApi.AddAttachment(
                        "Failure Details",
                        "text/plain",
                        Encoding.UTF8.GetBytes(failureDetails));

                    Assert.Fail(failureDetails);
                }

                return Task.CompletedTask;
            });
        }
    }
}
