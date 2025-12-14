using Microsoft.Playwright;

namespace BattleShipGame.Pages
{
    public class BattleshipGamePage
    {
        private readonly IPage _page;

        public BattleshipGamePage(IPage page)
        {
            _page = page;
        }

        public async Task NavigateAsync(string baseUrl)
        {
            await _page.GotoAsync(baseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        }

        public async Task ChooseRandomOpponentAsync()
        {
            // Example selector – adjust to actual text/id
            await _page.GetByRole(AriaRole.Link , new() { Name = "random" }).ClickAsync();
        }

        public async Task RandomiseShipsAsync(int randomClicks)
        {
            var randomiseLink = _page.GetByText("Randomise");
            for (int i = 0; i < randomClicks; i++)
            {
                await randomiseLink.ClickAsync();
            }
        }

        public async Task ClickPlayAsync()
        {
            await _page.GetByText("Play").ClickAsync();
        }

        public async Task WaitForOpponentAsync(TimeSpan timeout)
        {
            // Suppose there’s a status label saying "Waiting for opponent" -> "Your turn"
            await _page.Locator(".notification__game-started-move-on") // adjust selector
                .Filter(new LocatorFilterOptions { HasTextRegex = new System.Text.RegularExpressions.Regex("The game started, your turn.    ", System.Text.RegularExpressions.RegexOptions.IgnoreCase) })
                .WaitForAsync(new LocatorWaitForOptions { Timeout = (float)timeout.TotalMilliseconds });
        }

        public async Task<bool> IsMyTurnAsync()
        {
            // You need a reliable UI indicator: e.g. "Your move" in some status text
            var status = await _page.Locator("#status-text-or-similar").InnerTextAsync();
            return status.Contains("Your", StringComparison.OrdinalIgnoreCase)
                   && status.Contains("turn", StringComparison.OrdinalIgnoreCase);
        }

        public async Task FireAtAsync(Coordinate coord)
        {
            // Map board coordinate to cell selector (e.g. data-row/col attributes)
            var table = _page.Locator(".battlefield__rival");
            var cell = table.Locator($"td[data-y='{coord.Row}'][data-x='{coord.Col}']");
            await cell.ClickAsync();
        }

        public async Task<CellState> ReadLastShotResultAsync(Coordinate coord)
        {
            // Could be reading CSS classes on last clicked cell (hit/miss), or reading text
            // Example:
            var table = _page.Locator(".battlefield__rival");
            var cell = table.Locator($"td[data-y='{coord.Row}'][data-x='{coord.Col}']");
            var classAttribute = await cell.GetAttributeAsync("class");

            if (classAttribute?.Contains("hit") == true)
                return CellState.Hit;
            if (classAttribute?.Contains("sunk") == true)
                return CellState.Sunk;

            return CellState.Miss;
        }

        public async Task<GameResult> ReadGameResultAsync()
        {
            var status = await _page.Locator(".notification-message").InnerTextAsync();

            if (status.Contains("you won", StringComparison.OrdinalIgnoreCase))
                return GameResult.Victory;
            if (status.Contains("You lose", StringComparison.OrdinalIgnoreCase))
                return GameResult.Defeat;
            if (status.Contains("opponent left", StringComparison.OrdinalIgnoreCase))
                return GameResult.OpponentLeft;
            if (status.Contains("connection lost", StringComparison.OrdinalIgnoreCase))
                return GameResult.ConnectionLost;

            return GameResult.Unknown;
        }

        public async Task<bool> IsGameOverAsync()
        {
            var status = await _page.Locator(".notification-message").InnerTextAsync();
            return status.IndexOf("won", StringComparison.OrdinalIgnoreCase) >= 0
                || status.IndexOf("lose", StringComparison.OrdinalIgnoreCase) >= 0
                || status.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0
                || status.IndexOf("connection", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
