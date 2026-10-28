namespace DesiMerge.Gameplay
{
    /// <summary>What the HUD shows for one power-up button.</summary>
    public readonly struct PowerUpButtonState
    {
        public PowerUpButtonState(string id, string title, int available, bool interactable, bool selected)
        {
            Id = id;
            Title = title;
            Available = available;
            Interactable = interactable;
            Selected = selected;
        }

        public string Id { get; }
        public string Title { get; }
        /// <summary>Free uses left this game plus saved charges.</summary>
        public int Available { get; }
        public bool Interactable { get; }
        /// <summary>Waiting for a target (Hammer).</summary>
        public bool Selected { get; }
    }

    /// <summary>The Continue (rewarded-ad revive) offer on the game-over panel.</summary>
    public enum ReviveOfferState
    {
        /// <summary>Not offered: no ad provider in this build, revives turned off, or this game's revive was used.</summary>
        Hidden = 0,
        /// <summary>Tap to watch an ad and continue.</summary>
        Available = 1,
        /// <summary>An ad request is in flight; the button is disabled.</summary>
        Loading = 2,
        /// <summary>No ad is ready right now. The player may try again.</summary>
        NotReady = 3,
        /// <summary>The ad was closed before the reward. No revive; the player may try again.</summary>
        Cancelled = 4,
        /// <summary>The ad failed to load or show (or no fill). No revive; the player may try again.</summary>
        Failed = 5,
        /// <summary>No network connection. No revive; the player may try again.</summary>
        Offline = 6,
    }
}
