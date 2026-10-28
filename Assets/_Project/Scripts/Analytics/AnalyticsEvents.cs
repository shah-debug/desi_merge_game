namespace DesiMerge.Analytics
{
    /// <summary>Canonical event names. Use snake_case and add new names here rather than inline strings.</summary>
    public static class AnalyticsEvents
    {
        public const string AppStarted = "app_started";
        public const string GameStarted = "game_started";
        public const string GameOver = "game_over";
        public const string RewardedAdRequested = "rewarded_ad_requested";
        public const string RewardedAdResult = "rewarded_ad_result";
        public const string PowerUpUsed = "power_up_used";
        public const string Revived = "revived";
    }
}
