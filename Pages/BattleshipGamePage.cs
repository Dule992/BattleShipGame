using Microsoft.Playwright;

namespace BattleShipGame.Pages
{
    public class BattleshipGamePage
    {
        private readonly IPage _page;
        private readonly string GAME_RESULT_MESSAGE = ".notification:not(.none) .notification-message";
        private readonly string RESTART_BUTTON = ".notification:not(.none) .notification-submit.restart";

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
            await _page.GetByRole(AriaRole.Link, new() { Name = "random" }).ClickAsync();
        }

        public async Task RandomizeShipsAsync(int randomClicks)
        {
            var randomizeLink = _page.GetByText("Randomise");
            for (int i = 0; i < randomClicks; i++)
            {
                await randomizeLink.ClickAsync();
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
            var cell = _page.Locator($".battlefield__rival .battlefield-table .battlefield-cell-content[data-y='{coord.Row}'][data-x='{coord.Col}']");
            await cell.ClickAsync();
        }

        public async Task<CellState> ReadLastShotResultAsync(Coordinate coord)
        {
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

        public async Task<GameResult> ReadGameResultAsync()
        {
            var gameResultMessage = _page.Locator(GAME_RESULT_MESSAGE);
            var isMessageVisible = await gameResultMessage.IsVisibleAsync();

            if (isMessageVisible)
            {
                var combinedText = gameResultMessage.InnerTextAsync().Result.ToString();

                // Check in order of priority (game outcome first, then connection issues)

                // PRIORITY 1: Victory - Check for victory messages first
                if (combinedText.Contains("Game over. Congratulations, you won!"))
                    return GameResult.Victory;

                // PRIORITY 2: Defeat - Check for defeat messages
                if (combinedText.Contains("Game over. You lose."))
                    return GameResult.Defeat;

                // PRIORITY 3: Opponent Left - Must be specific to avoid false matches
                if (combinedText.Contains("Your opponent has left the game."))
                    return GameResult.OpponentLeft;

                // PRIORITY 4: Server/Connection issues
                if (combinedText.Contains("Unexpected error. Further play is impossible."))
                    return GameResult.ConnectionLost;
            }
            return GameResult.Unknown;
        }

        /// <summary>
        /// Checks if the restart button is visible, indicating the game has ended.
        /// </summary>
        public async Task<bool> IsRestartButtonVisibleAsync()
        {
            try
            {
                var restartButton = _page.Locator(RESTART_BUTTON);
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
            var locator = _page.Locator(RESTART_BUTTON);

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
