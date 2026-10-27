using System;

namespace DesiMerge.Persistence
{
    public static class SaveKey
    {
        public const string PlayerProgress = "player_progress";
        public const string Settings = "settings";
        public const string Achievements = "achievements";
        public const string DailyChallenge = "daily_challenge";

        public static void Validate(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Save key must not be empty.", nameof(key));
            }

            foreach (var c in key)
            {
                var valid = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '_';
                if (!valid)
                {
                    throw new ArgumentException($"Save key '{key}' contains invalid character '{c}'.", nameof(key));
                }
            }
        }
    }
}
