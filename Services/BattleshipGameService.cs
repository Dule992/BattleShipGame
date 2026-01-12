using BattleShipGame.BattleShip.Core.Strategy;
using BattleShipGame.Pages;
using Microsoft.Extensions.Logging;
namespace Battleship.UI.Services
{
    public class BattleshipGameService
    {
        private readonly BattleshipGamePage _page;
        private readonly BoardState _board;
        private readonly IShotStrategy _strategy;
        private readonly Random _rnd = new();
        private readonly ILogger<BattleshipGameService> _logger;
        private readonly List<string> _moveLog = new();

        public IReadOnlyList<string> MoveLog => _moveLog.AsReadOnly();

        public BattleshipGameService(
            BattleshipGamePage page,
            ILogger<BattleshipGameService> logger)
        {
            _page = page;
            _logger = logger;
            _board = new BoardState(10);
            _strategy = new HuntTargetShotStrategy(_board);
        }

        public async Task<(GameResult Result, FailureReason FailureReason)> PlayFullGameAsync(
            string baseUrl,
            TimeSpan overallTimeout,
            TimeSpan opponentConnectTimeout)
        {
            // Note: opponentConnectTimeout parameter is reserved for future use
            var startTime = DateTime.UtcNow;

            _logger.LogInformation("Starting Battleship game. BaseUrl={BaseUrl}", baseUrl);
            await InitializeGameAsync(baseUrl);

            // Main game loop
            while (!await _page.IsRestartButtonVisibleAsync())
            {
                Thread.Sleep(1000);

                if (await _page.IsMyTurnAsync())
                {
                    var turnResult = await ExecuteTurnSafelyAsync();
                    if (turnResult.HasValue)
                    {
                        return turnResult.Value;
                    }
                }
                else
                {
                    await WaitForOpponentTurnAsync();
                }
            }

            return await HandleGameCompletionAsync();
        }

        /// <summary>
        /// Initializes the game: navigates, selects opponent, randomizes ships, and starts the game.
        /// </summary>
        private async Task InitializeGameAsync(string baseUrl)
        {
            await _page.NavigateAsync(baseUrl);
            _logger.LogInformation("Page loaded. Choosing random opponent.");

            await _page.ChooseRandomOpponentAsync();

            int randomClicks = _rnd.Next(1, 16);
            _logger.LogInformation("Randomising ships {Clicks} time(s).", randomClicks);
            await _page.RandomiseShipsAsync(randomClicks);

            _logger.LogInformation("Clicking Play.");
            await _page.ClickPlayAsync();

            if (await _page.WaitForOpponentAsync())
            {
                _logger.LogInformation("Opponent is ready.");
            }
        }

        /// <summary>
        /// Executes a turn with error handling. Returns game result if game ended during execution.
        /// </summary>
        private async Task<(GameResult Result, FailureReason FailureReason)?> ExecuteTurnSafelyAsync()
        {
            try
            {
                await ExecuteTurnAsync();
                return null; // Game continues
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Game has ended"))
            {
                // Game ended during firing - return result immediately
                _logger.LogInformation("Game ended detected: {Message}", ex.Message);
                return await GetFinalGameResultAsync();
            }
            catch (TimeoutException ex)
            {
                // Handle timeout exception - game likely ended during turn execution
                _logger.LogWarning(ex, "Game ended during turn execution");
                return await GetFinalGameResultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during turn execution");
                return await GetFinalGameResultAsync();
            }
        }

        /// <summary>
        /// Reads and returns the final game result.
        /// </summary>
        private async Task<(GameResult Result, FailureReason FailureReason)> GetFinalGameResultAsync()
        {
            var gameResult = await _page.ReadGameResultAsync();
            var failureReason = MapFailureReason(gameResult);
            return (gameResult, failureReason);
        }

        /// <summary>
        /// Handles game completion: reads result, logs outcome, and returns final result.
        /// </summary>
        private async Task<(GameResult Result, FailureReason FailureReason)> HandleGameCompletionAsync()
        {
            var (gameResult, failureReason) = await GetFinalGameResultAsync();

            if (gameResult == GameResult.Victory)
            {
                _logger.LogInformation("✓✓✓ Game ended in VICTORY! ✓✓✓");
                _moveLog.Add("STATE: VICTORY - Game won!");
            }
            else
            {
                _logger.LogWarning("✗✗✗ Game ended without victory ✗✗✗");
                LogGameEndWithoutVictory(gameResult, failureReason);
            }

            return (gameResult, failureReason);
        }

        /// <summary>
        /// Logs game end scenarios that are not victories.
        /// </summary>
        private void LogGameEndWithoutVictory(GameResult gameResult, FailureReason failureReason)
        {
            _logger.LogInformation("Game ended without victory. Result={Result}, Reason={Reason}",
                gameResult, failureReason);

            switch (gameResult)
            {
                case GameResult.OpponentLeft:
                    _logger.LogInformation("Opponent left the game.");
                    _moveLog.Add("STATE: Opponent left the game.");
                    break;

                case GameResult.ConnectionLost:
                    _logger.LogInformation("Connection lost during the game.");
                    _moveLog.Add("STATE: Connection lost.");
                    break;
            }
        }

        /// <summary>
        /// Executes a single turn: selects shot, fires, reads result, and updates strategy.
        /// </summary>
        private async Task ExecuteTurnAsync()
        {
            // Check if restart button is visible before firing (game may have ended)
            if (await _page.IsRestartButtonVisibleAsync())
            {
                _logger.LogInformation("Restart button detected - game has ended");
                throw new InvalidOperationException("Game has ended - cannot fire shot");
            }

            var nextShot = _strategy.GetNextShot(_board);
            _logger.LogInformation("Firing at {Coordinate}", nextShot);
            _moveLog.Add($"SHOT: {nextShot}");

            await _page.FireAtAsync(nextShot);

            // Wait a moment for UI to update after firing
            await Task.Delay(300);

            // Read the result with retry logic to ensure UI has updated
            var result = await ReadShotResultWithRetryAsync(nextShot, maxRetries: 5);

            // Log and record the result
            LogShotResult(nextShot, result);
            _moveLog.Add($"RESULT: {nextShot} => {result}");

            // Update strategy with the result (strategy updates board state internally)
            _strategy.RegisterShotResult(nextShot, result);
        }

        /// <summary>
        /// Reads shot result with retry logic to handle UI update delays.
        /// </summary>
        private async Task<CellState> ReadShotResultWithRetryAsync(Coordinate coord, int maxRetries = 5)
        {
            CellState lastResult = CellState.Miss;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                lastResult = await _page.ReadLastShotResultAsync(coord);

                // If we get a non-Miss result, return it immediately
                if (lastResult != CellState.Miss)
                {
                    return lastResult;
                }

                // If it's still showing as Miss, wait a bit longer and retry
                if (attempt < maxRetries)
                {
                    await Task.Delay(200 * attempt); // Exponential backoff
                }
            }

            // After all retries, return what we got (likely Miss)
            return lastResult;
        }

        /// <summary>
        /// Logs the shot result with appropriate detail level based on result type.
        /// </summary>
        private void LogShotResult(Coordinate coord, CellState result)
        {
            switch (result)
            {
                case CellState.Hit:
                    _logger.LogInformation("✓ HIT at {Coordinate} - Ship damaged!", coord);
                    _moveLog.Add($"STATE: Hit detected at {coord}");
                    break;

                case CellState.Miss:
                    _logger.LogDebug("✗ Miss at {Coordinate}", coord);
                    break;

                default:
                    _logger.LogWarning("? Unknown result at {Coordinate}: {Result}", coord, result);
                    break;
            }
        }

        /// <summary>
        /// Waits for opponent's turn to complete by polling IsMyTurnAsync.
        /// </summary>
        private async Task WaitForOpponentTurnAsync()
        {
            const int maxWaitTime = 30000; // 30 seconds max wait
            const int pollInterval = 500;
            var waited = 0;

            while (waited < maxWaitTime)
            {
                try
                {
                    // Check if game ended during opponent's turn
                    if (await _page.IsGameOverAsync() || await _page.IsRestartButtonVisibleAsync())
                    {
                        return;
                    }

                    // Check if it's our turn now
                    if (await _page.IsMyTurnAsync())
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    // Handle locator errors that may occur when game ends
                    _logger.LogWarning(ex, "Error checking turn status - verifying if game ended");

                    // Verify if game is over (UI elements may have changed/disappeared)
                    try
                    {
                        if (await _page.IsGameOverAsync() || await _page.IsRestartButtonVisibleAsync())
                        {
                            return;
                        }
                    }
                    catch
                    {
                        // If we can't check game status, log and continue waiting
                        _logger.LogWarning("Unable to verify game status, continuing to wait");
                    }
                }

                await Task.Delay(pollInterval);
                waited += pollInterval;
            }

            if (waited >= maxWaitTime)
            {
                _logger.LogWarning("Waited {Seconds} seconds for opponent turn - possible timeout", maxWaitTime / 1000);
            }
        }

        private static FailureReason MapFailureReason(GameResult result) => result switch
        {
            GameResult.Victory => FailureReason.None,
            GameResult.Defeat => FailureReason.Defeat,
            GameResult.OpponentLeft => FailureReason.OpponentLeft,
            GameResult.ConnectionLost => FailureReason.ConnectionLost,
            _ => FailureReason.Unknown
        };
    }
}
