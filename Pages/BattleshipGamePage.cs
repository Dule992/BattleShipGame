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
            try
            {
                // Check for "Your turn." message
                var statusMoveOn = await _page.Locator(".notification__move-on").InnerTextAsync();
                if (statusMoveOn.Contains("Your turn.", StringComparison.OrdinalIgnoreCase)
                    || statusMoveOn.Contains("The game started, your turn.", StringComparison.OrdinalIgnoreCase))
                    return true;

                // Also check for opponent's turn message to confirm it's NOT our turn
                var statusMoveOff = await _page.Locator(".notification__move-off").InnerTextAsync();
                if (statusMoveOff.Contains("Opponent's turn", StringComparison.OrdinalIgnoreCase)
                    || statusMoveOff.Contains("please wait", StringComparison.OrdinalIgnoreCase))
                    return false;

                return false;
            }
            catch
            {
                // If selector fails, try alternative approach
                var allNotifications = await _page.Locator(".notification-message").AllInnerTextsAsync();
                foreach (var notification in allNotifications)
                {
                    if (notification.Contains("Your turn.", StringComparison.OrdinalIgnoreCase)
                        || notification.Contains("The game started, your turn.", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            }
        }

        public async Task FireAtAsync(Coordinate coord)
        {
            // Map board coordinate to cell selector (e.g. data-row/col attributes)
            var cell = _page.Locator($".battlefield__rival .battlefield-table .battlefield-cell-content[data-y='{coord.Row}'][data-x='{coord.Col}']");
            await cell.ClickAsync();
        }

        public async Task<CellState> ReadLastShotResultAsync(Coordinate coord)
        {
            // More reliable approach: Check parent element for hit/sunk classes
            // The cell structure is: .battlefield-cell (parent with hit/done class) > .battlefield-cell-content (child)
            var cellLocator = _page.Locator($".battlefield__rival .battlefield-table .battlefield-cell-content[data-y='{coord.Row}'][data-x='{coord.Col}']");
            
            try
            {
                var hitParentCss = _page.Locator($".battlefield__rival .battlefield-table .battlefield-cell.battlefield-cell__hit .battlefield-cell-content[data-y='{coord.Row}'][data-x='{coord.Col}']");
                if (await hitParentCss.IsVisibleAsync().ConfigureAwait(false))
                    return CellState.Hit;

                var sunkParentCss = _page.Locator($".battlefield__rival .battlefield-table .battlefield-cell.battlefield-cell__done .battlefield-cell-content[data-y='{coord.Row}'][data-x='{coord.Col}']");
                if (await sunkParentCss.IsVisibleAsync().ConfigureAwait(false))
                    return CellState.Sunk;
            }
            catch
            {
                // Fallback: If selectors fail, assume Miss (will be retried by service layer)
            }

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
            // Strategy 1: Try .notification-message (primary selector for game-over messages)
            var locator = _page.Locator(".notification-message");

            string[] texts;
            try
            {
                texts = (string[])await locator.AllInnerTextsAsync();
            }
            catch
            {
                // Strategy 2: Fallback - try to get count and read individually
                try
                {
                    var count = await locator.CountAsync();
                    var arr = new string[count];
                    for (int i = 0; i < count; i++)
                    {
                        arr[i] = await locator.Nth(i).InnerTextAsync() ?? string.Empty;
                    }
                    texts = arr;
                }
                catch
                {
                    // Strategy 3: Alternative - check for restart button presence and read any visible notification text
                    // If restart button exists, game is definitely over, try reading from page text content
                    var restartButton = _page.Locator(".notification-submit.restart");
                    var hasRestartButton = await restartButton.IsVisibleAsync();
                    
                    if (hasRestartButton)
                    {
                        // Try reading from the button's parent container or nearby notification elements
                        var notificationContainer = _page.Locator(".notification-message");
                        var notificationTexts = await notificationContainer.AllInnerTextsAsync();
                        texts = notificationTexts.ToArray();
                    }
                    else
                    {
                        texts = Array.Empty<string>();
                    }
                }
            }

            if (texts == null || texts.Length == 0)
                return GameResult.Unknown;

            // Combine all notification texts into a single string for comprehensive checking
            var textList = new List<string>();
            foreach (var text in texts)
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    textList.Add(text);
                }
            }

            var combinedText = string.Join(" ", textList);

            if (string.IsNullOrWhiteSpace(combinedText))
                return GameResult.Unknown;

            // Check in order of priority (game outcome first, then connection issues)
            
            // PRIORITY 1: Victory - Check for victory messages first (most specific to least specific)
            // Check "you won" first as it's the most specific victory indicator
            if (combinedText.IndexOf("Game over. Congratulations, you won!", StringComparison.OrdinalIgnoreCase) >= 0)
                return GameResult.Victory;

            // PRIORITY 2: Defeat - Check for defeat messages (most specific to least specific)
            // Check "you lose" first as it's the most specific defeat indicator
            if (combinedText.IndexOf("Game over. You lose.", StringComparison.OrdinalIgnoreCase) >= 0)
                return GameResult.Defeat;
                
            // PRIORITY 3: Opponent Left - Must be specific to avoid false matches
            // Only match if it explicitly says "opponent has left" or "your opponent has left"
            // and NOT if it's part of a victory/defeat message
            if (combinedText.IndexOf("Your opponent has left the game.", StringComparison.OrdinalIgnoreCase) >= 0)
                return GameResult.OpponentLeft;

            // PRIORITY 4: Server/Connection issues
            if (combinedText.IndexOf("Unexpected error. Further play is impossible.", StringComparison.OrdinalIgnoreCase) >= 0)
                return GameResult.ConnectionLost;

            return GameResult.Unknown;
        }

        /// <summary>
        /// Checks if the restart button is visible, indicating the game has ended.
        /// </summary>
        public async Task<bool> IsRestartButtonVisibleAsync()
        {
            try
            {
                var restartButton = _page.Locator(".notification-submit.restart");
                return await restartButton.IsVisibleAsync();
            }
            catch
            {
                // If we can't check visibility, assume restart button is not visible
                return false;
            }
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
