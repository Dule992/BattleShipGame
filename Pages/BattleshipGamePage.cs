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
            await _page.Locator(".battlefield-start-button").ClickAsync();
        }

        public async Task<bool> WaitForTheGameStartedMessageAsync()
        {
            var statusGameStartedOn = await _page.Locator("notification__game-started-move-on").InnerTextAsync();
            var statusGameStartedOff = await _page.Locator("notification__game-started-move-off").InnerTextAsync();
            return statusGameStartedOn.Contains("The game started, your turn.", StringComparison.OrdinalIgnoreCase) 
                || statusGameStartedOff.Contains("The game began, opponent's turn.", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> WaitForOpponentAsync()
        {
            var statusWaitingOn = await _page.Locator(".notification__waiting-for-rival").InnerTextAsync();
            return statusWaitingOn.Contains("Waiting for opponent.", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> IsMyTurnAsync()
        {
            var statusMoveOn = await _page.Locator(".notification__move-on").InnerTextAsync();
            return statusMoveOn.Contains("Your turn.", StringComparison.OrdinalIgnoreCase);
        }

        public async Task FireAtAsync(Coordinate coord)
        {
            // Map board coordinate to cell selector (e.g. data-row/col attributes)
            var cell = _page.Locator($".battlefield__rival .battlefield-table .battlefield-cell-content[data-y='{coord.Row}'][data-x='{coord.Col}']");
            await cell.ClickAsync();
        }

        public async Task<CellState> ReadLastShotResultAsync(Coordinate coord)
        {
            // Could be reading CSS classes on last clicked cell (hit/miss), or reading text
            var isCellHit = await _page.Locator($".battlefield__rival .battlefield-table .battlefield-cell__hit .battlefield-cell-content[data-y='{coord.Row}'][data-x='{coord.Col}']").IsVisibleAsync();
            var isCellSunk = await _page.Locator($".battlefield__rival .battlefield-table .battlefield-cell__done .battlefield-cell-content[data-y='{coord.Row}'][data-x='{coord.Col}']").IsVisibleAsync();

            if (isCellHit == true)
                return CellState.Hit;
            if (isCellSunk == true)
                return CellState.Sunk;

            return CellState.Miss;
        }

        // PSEUDOCODE / PLAN:
        // 1. Locate all elements matching ".notification-message".
        // 2. Try to fetch all inner texts in one call using Locator.AllInnerTextsAsync().
        // 3. If that fails, fallback to iterating elements using CountAsync() and Nth(i).InnerTextAsync().
        // 4. If no texts found, return GameResult.Unknown.
        // 5. Define checks in order of specificity to map found text to a GameResult:
        //    - "you won" or "won" => Victory
        //    - "you lose" or "lose" => Defeat
        //    - "opponent has left" or "left" => OpponentLeft
        //    - "connection lost" or "connection" => ConnectionLost
        //    - "timeout" or "timed out" => Timeout
        // 6. For each non-empty text, perform case-insensitive substring checks and return the matching GameResult immediately.
        // 7. If none match, return GameResult.Unknown.

        public async Task<GameResult> ReadGameResultAsync()
        {
            var locator = _page.Locator(".notification-message");

            string[] texts;
            try
            {
                texts = (string[])await locator.AllInnerTextsAsync();
            }
            catch
            {
                var count = await locator.CountAsync();
                var arr = new string[count];
                for (int i = 0; i < count; i++)
                {
                    arr[i] = await locator.Nth(i).InnerTextAsync() ?? string.Empty;
                }
                texts = arr;
            }

            if (texts == null || texts.Length == 0)
                return GameResult.Unknown;

            foreach (var text in texts)
            {
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if (text.IndexOf("you won", StringComparison.OrdinalIgnoreCase) >= 0
                    || text.IndexOf("won", StringComparison.OrdinalIgnoreCase) >= 0)
                    return GameResult.Victory;

                if (text.IndexOf("you lose", StringComparison.OrdinalIgnoreCase) >= 0
                    || text.IndexOf("lose", StringComparison.OrdinalIgnoreCase) >= 0)
                    return GameResult.Defeat;

                if (text.IndexOf("opponent has left", StringComparison.OrdinalIgnoreCase) >= 0
                    || text.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0)
                    return GameResult.OpponentLeft;

                if (text.IndexOf("connection lost", StringComparison.OrdinalIgnoreCase) >= 0
                    || text.IndexOf("connection", StringComparison.OrdinalIgnoreCase) >= 0)
                    return GameResult.ConnectionLost;

                if (text.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0
                    || text.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0)
                    return GameResult.Timeout;
            }

            return GameResult.Unknown;
        }

        public async Task<bool> IsGameOverAsync()
        {
            // PSEUDOCODE / PLAN:
            // 1. Locate all elements matching ".notification-message".
            // 2. Try to fetch all inner texts in one call using Locator.AllInnerTextsAsync().
            // 3. If that fails, fallback to iterating elements using CountAsync() and Nth(i).InnerTextAsync().
            // 4. If no texts found, return false.
            // 5. Define keywords that indicate the game is over (e.g. "won", "lose", "left", "connection", "timeout").
            // 6. For each non-empty text, perform case-insensitive substring checks against the keywords.
            // 7. If any text contains any keyword, return true; otherwise return false.

            var locator = _page.Locator(".notification-submit.restart");

            string[] texts;
            try
            {
                texts = (string[])await locator.AllInnerTextsAsync();
            }
            catch
            {
                var count = await locator.CountAsync();
                var arr = new string[count];
                for (int i = 0; i < count; i++)
                {
                    arr[i] = await locator.Nth(i).InnerTextAsync() ?? string.Empty;
                }
                texts = arr;
            }

            if (texts == null || texts.Length == 0)
                return false;

            string[] keywords = new[] { "won", "you won", "lose", "you lose", "left", "connection", "timeout" };

            foreach (var text in texts)
            {
                if (string.IsNullOrEmpty(text))
                    continue;

                foreach (var kw in keywords)
                {
                    if (text.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }

            return false;
        }
    }
}
