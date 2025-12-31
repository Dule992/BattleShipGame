namespace BattleShipGame.Battleship.Tests.Config
{
    public class PlaywrightConfig
    {
        public static string Browser {  get; set; }
        public static string Device { get; set; }
        public static bool Headless { get; set; }
        public static int SlowMoMilliseconds { get; set; }
        public static int DefaultTimeoutMilliseconds { get; set; }
    }
}
