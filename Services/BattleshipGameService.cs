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
            var start = DateTime.UtcNow;

            _logger.LogInformation("Starting Battleship game. BaseUrl={BaseUrl}", baseUrl);

            await _page.NavigateAsync(baseUrl);
            _logger.LogInformation("Page loaded. Choosing random opponent.");

            await _page.ChooseRandomOpponentAsync();

            int randomClicks = _rnd.Next(1, 16);
            _logger.LogInformation("Randomising ships {Clicks} time(s).", randomClicks);
            await _page.RandomiseShipsAsync(randomClicks);

            _logger.LogInformation("Clicking Play.");
            await _page.ClickPlayAsync();

            if(await _page.WaitForOpponentAsync())
                _logger.LogInformation("Opponent is ready.");

            //bool isGameOver = await _page.IsGameOverAsync();

            //bool isMyTurn = await _page.IsMyTurnAsync();

            //var gameResult1 = await _page.ReadGameResultAsync();


             
            while (!await _page.IsGameOverAsync())
            {
                if ((DateTime.UtcNow - start) > overallTimeout)
                {
                    _logger.LogWarning("Game timed out after {Minutes} minutes.",
                        overallTimeout);

                    return (GameResult.Timeout, FailureReason.Timeout);
                }

                if (await _page.IsMyTurnAsync())
                {
                    var nextShot = _strategy.GetNextShot(_board);
                    _logger.LogInformation("Firing at {Coordinate}.", nextShot);
                    _moveLog.Add($"SHOT: {nextShot}");

                    await _page.FireAtAsync(nextShot);
                    var result = await _page.ReadLastShotResultAsync(nextShot);

                    _logger.LogInformation("Result at {Coordinate}: {Result}", nextShot, result);
                    _moveLog.Add($"RESULT: {nextShot} => {result}");

                    _strategy.RegisterShotResult(nextShot, result);

                    if (result == CellState.Hit)
                    {
                        _logger.LogInformation("Hit at {Coordinate}.", nextShot);
                    }
                    else if (result == CellState.Sunk)
                    {
                        _logger.LogInformation("Ship sunk at/around {Coordinate}.", nextShot);
                        _moveLog.Add($"STATE: Ship sunk at cluster around {nextShot}");
                    }
                }
                else
                {
                    await Task.Delay(500);
                }
            }

            var gameResult = await _page.ReadGameResultAsync();
            var failureReason = MapFailureReason(gameResult);

            if (gameResult == GameResult.Victory)
            {
                _logger.LogInformation("Game ended in VICTORY.");
            }
            else
            {
                _logger.LogInformation("Game ended without victory. Result={Result}, Reason={Reason}",
                    gameResult, failureReason);

                if (gameResult == GameResult.OpponentLeft)
                {
                    _logger.LogInformation("Opponent left the game.");
                    _moveLog.Add("STATE: Opponent left the game.");
                }
                else if (gameResult == GameResult.ConnectionLost)
                {
                    _logger.LogInformation("Connection lost during the game.");)
                    _moveLog.Add("STATE: Connection lost.");
                }
            }

            return (gameResult, failureReason);
        }

        private static FailureReason MapFailureReason(GameResult result) => result switch
        {
            GameResult.Victory => FailureReason.None,
            GameResult.Defeat => FailureReason.Defeat,
            GameResult.OpponentLeft => FailureReason.OpponentLeft,
            GameResult.ConnectionLost => FailureReason.ConnectionLost,
            GameResult.Timeout => FailureReason.Timeout,
            _ => FailureReason.Unknown
        };
    }
}
