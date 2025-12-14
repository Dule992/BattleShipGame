using BattleShipGame.BattleShip.Core.Strategy;
using BattleShipGame.Pages;

namespace BattleShipGame.Services
{
    public class BattleshipGameService
    {
        private readonly BattleshipGamePage _page;
        private readonly BoardState _board;
        private readonly IShotStrategy _strategy;
        private readonly Random _rnd = new();

        public BattleshipGameService(BattleshipGamePage page)
        {
            _page = page;
            _board = new BoardState(10);
            _strategy = new HuntTargetShotStrategy(_board);
        }

        public async Task<(GameResult Result, FailureReason FailureReason)> PlayFullGameAsync(string baseUrl, TimeSpan overallTimeout)
        {
            var start = DateTime.UtcNow;

            await _page.NavigateAsync(baseUrl);
            await _page.ChooseRandomOpponentAsync();

            int randomClicks = _rnd.Next(1, 16); // 1–15 inclusive
            await _page.RandomiseShipsAsync(randomClicks);

            await _page.ClickPlayAsync();
            await _page.WaitForOpponentAsync(TimeSpan.FromSeconds(60));

            while (!await _page.IsGameOverAsync())
            {
                if (DateTime.UtcNow - start > overallTimeout)
                {
                    return (GameResult.Timeout, FailureReason.Timeout);
                }

                if (await _page.IsMyTurnAsync())
                {
                    var nextShot = _strategy.GetNextShot(_board);
                    await _page.FireAtAsync(nextShot);

                    var result = await _page.ReadLastShotResultAsync(nextShot);
                    _strategy.RegisterShotResult(nextShot, result);
                }
                else
                {
                    // Opponent’s turn - small delay to avoid busy-wait
                    await Task.Delay(500);
                }
            }

            var gameResult = await _page.ReadGameResultAsync();
            var failureReason = MapFailureReason(gameResult);

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
