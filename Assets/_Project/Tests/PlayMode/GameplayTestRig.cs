using System;
using System.Collections.Generic;
using DesiMerge.Analytics;
using DesiMerge.Audio;
using DesiMerge.Configuration;
using DesiMerge.Cosmetics;
using DesiMerge.Core;
using DesiMerge.Gameplay;
using DesiMerge.Monetization;
using DesiMerge.Persistence;
using DesiMerge.PowerUps;
using DesiMerge.Progression;
using DesiMerge.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DesiMerge.Tests.PlayMode
{
    /// <summary>
    /// Builds a board, drop controller and loop in code with a manual clock and script-driven 2D physics,
    /// so tests control exactly when physics steps and when the board resolves merges.
    /// </summary>
    internal sealed class GameplayTestRig : IDisposable
    {
        public const float Step = 0.02f;

        private readonly List<Object> _created = new List<Object>();
        private readonly SimulationMode2D _previousSimulationMode;

        public GameplayTestRig(int levels = 4, FinalLevelMergeRule finalRule = FinalLevelMergeRule.ClearBoth, float grace = 1.5f, float gameOverDelay = 2f)
        {
            _previousSimulationMode = Physics2D.simulationMode;
            Physics2D.simulationMode = SimulationMode2D.Script;

            var texture = Track(new Texture2D(4, 4));
            var sprite = Track(Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f));

            var definitions = new List<MergeItemDefinition>();
            for (var i = 0; i < levels; i++)
            {
                var definition = Track(ScriptableObject.CreateInstance<MergeItemDefinition>());
                definition.Set($"l{i}", $"L{i}", sprite, Color.white, 0.3f + 0.1f * i, 1f, 10 * (i + 1));
                definitions.Add(definition);
            }

            Progression = Track(ScriptableObject.CreateInstance<MergeProgression>());
            Progression.Set(definitions, Mathf.Min(2, levels - 1));

            Config = Track(ScriptableObject.CreateInstance<GameplayConfig>());
            Config.Set(Progression, null, cooldown: 0.5f, gameOverDelay: gameOverDelay, grace: grace, finalRule: finalRule, finalBonus: 100, seed: 99);

            var template = Track(new GameObject("ItemTemplate"));
            template.SetActive(false);
            template.AddComponent<SpriteRenderer>();
            template.AddComponent<Rigidbody2D>();
            template.AddComponent<CircleCollider2D>().radius = 0.5f;
            var itemTemplate = template.AddComponent<MergeItem>();

            var boardObject = Track(new GameObject("Board"));
            Board = boardObject.AddComponent<MergeBoard>();
            Board.enabled = false; // Tests call Step() themselves instead of FixedUpdate.
            Board.SetPrefabForTests(itemTemplate);
            Board.Initialize(Config, () => Now);

            Drop = Track(new GameObject("Drop")).AddComponent<DropController>();
            Drop.Configure(Config, Board.transform);

            View = new FakeView();
            Store = new InMemorySaveStore();
            Clock = new ManualClock(new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc));

            Achievements = Track(ScriptableObject.CreateInstance<AchievementCatalog>());
            Achievements.Set(new[]
            {
                new AchievementDefinition { id = "first_game", title = "First Game", type = AchievementType.CompleteGames, target = 1 },
                new AchievementDefinition { id = "merges_2", title = "Two Merges", type = AchievementType.TotalMerges, target = 2 },
                new AchievementDefinition { id = "discover_l2", title = "Level 2", type = AchievementType.DiscoverItem, itemId = "l2" },
            });
            DailyChallenges = Track(ScriptableObject.CreateInstance<DailyChallengeCatalog>());
            DailyChallenges.Set(new[]
            {
                new DailyChallengeDefinition { id = "daily_merges_2", title = "Merge twice", type = DailyChallengeType.MergesInDay, target = 2 },
            });

            PowerUpCatalog = Track(ScriptableObject.CreateInstance<PowerUpCatalog>());
            PowerUpCatalog.Set(new[]
            {
                new PowerUpDefinition { id = "hammer", title = "Hammer", kind = PowerUpKind.Hammer, freeUsesPerGame = 1, maxSavedCharges = 3 },
                new PowerUpDefinition { id = "shuffle", title = "Shuffle", kind = PowerUpKind.Shuffle, freeUsesPerGame = 1, maxSavedCharges = 3 },
            });
            Config.SetPowerUpRules();

            Services = BuildServices();
        }

        public PowerUpCatalog PowerUpCatalog { get; }

        /// <summary>Scriptable ad provider used by every BuildServices(); tests decide when and how it calls back.</summary>
        public FakeAdProvider Ads { get; } = new FakeAdProvider();

        public SilentAudioService Audio { get; private set; }
        public FakeNavigator Navigator { get; private set; }
        public ManualClock Clock { get; }
        public AchievementCatalog Achievements { get; }
        public DailyChallengeCatalog DailyChallenges { get; }

        /// <summary>Builds fresh services over the same storage, as if the app had been closed and reopened.</summary>
        public GameServices BuildServices()
        {
            Audio = new SilentAudioService();
            Navigator = new FakeNavigator();
            var progress = new PlayerProgressService(Store, Progression, Clock);
            var powerUps = new PowerUpService(Store, PowerUpCatalog);
            var rewards = new ProgressRewardGranter(progress, powerUps);
            var achievements = new AchievementService(Store, Achievements, progress, Progression, rewards);
            return new GameServices(
                new GameStateMachine(),
                progress,
                new SettingsService(Store, new SettingsData()),
                Audio,
                new NullAnalyticsService(),
                Ads,
                Navigator,
                achievements,
                new DailyChallengeService(Store, DailyChallenges, progress, Progression, Clock, rewards),
                Clock,
                powerUps,
                new CosmeticService(Store, null, achievements),
                Progression);
        }

        /// <summary>Last value the loop applied to Time.timeScale (the real one is never touched by the rig).</summary>
        public float TimeScale { get; private set; } = 1f;

        public float Now { get; set; }
        public MergeProgression Progression { get; }
        public GameplayConfig Config { get; }
        public MergeBoard Board { get; }
        public DropController Drop { get; }
        public FakeView View { get; }
        public InMemorySaveStore Store { get; }
        public GameServices Services { get; set; }
        public GameplayLoop Loop { get; private set; }

        public GameplayLoop CreateLoop()
        {
            Loop = new GameplayLoop(Config, Board, Drop, View, Services, () => Now, scale => TimeScale = scale);
            Loop.Begin();
            return Loop;
        }

        /// <summary>Spawns an item resting on the floor at board-local x.</summary>
        public MergeItem SpawnOnFloor(int level, float x)
        {
            return Board.SpawnLocal(level, new Vector2(x, Progression.Get(level).Radius));
        }

        /// <summary>Advances physics and then lets the board resolve merges, like one FixedUpdate.</summary>
        public void Simulate(int steps = 1)
        {
            for (var i = 0; i < steps; i++)
            {
                Physics2D.Simulate(Step);
                Now += Step;
                Board.Step(Now);
            }
        }

        public void Dispose()
        {
            Loop?.Dispose();
            Services?.Dispose();
            foreach (var created in _created)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            // Pooled item instances are parented to the board, so they were destroyed with it.
            Physics2D.simulationMode = _previousSimulationMode;
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }

    internal sealed class FakeView : IGameplayView
    {
        public event Action RestartRequested;
        public event Action PauseRequested;
        public event Action ResumeRequested;
        public event Action MainMenuRequested;

        public long LastScore = -1;
        public long LastBest = -1;
        public MergeItemDefinition LastNext;
        public int GameOverShown;
        public int GameOverHidden;
        public long LastFinalScore = -1;
        public bool? LastWasNewBest;
        public int PauseShown;
        public int PauseHidden;
        public bool InstructionsVisible;
        public int MergeFeedbackCount;
        public int LastFeedbackPoints;
        public string ConfigurationError;

        /// <summary>Screen positions with y above this value are treated as HUD controls.</summary>
        public float BlockAboveY = float.MaxValue;

        public void SetScore(long score) => LastScore = score;
        public void SetBestScore(long bestScore) => LastBest = bestScore;
        public void SetNextItem(MergeItemDefinition definition) => LastNext = definition;

        public void ShowMergeFeedback(Vector2 worldPosition, int points, MergeItemDefinition createdItem)
        {
            MergeFeedbackCount++;
            LastFeedbackPoints = points;
        }

        public void SetInstructionsVisible(bool visible) => InstructionsVisible = visible;
        public readonly List<string> Toasts = new List<string>();
        public void ShowToast(string message) => Toasts.Add(message);
        public void ShowPause() => PauseShown++;
        public void HidePause() => PauseHidden++;

        public void ShowGameOver(long finalScore, long bestScore, bool isNewBest)
        {
            GameOverShown++;
            LastFinalScore = finalScore;
            LastWasNewBest = isNewBest;
        }

        public void HideGameOver() => GameOverHidden++;

        public readonly List<string> Discoveries = new List<string>();
        public SessionSummary? LastSummary;
        public void ShowSessionSummary(SessionSummary summary) => LastSummary = summary;
        public void ShowDiscovery(MergeItemDefinition item) => Discoveries.Add(item.DisplayName);
        public void ShowConfigurationError(string message) => ConfigurationError = message;
        public bool BlocksPointer(Vector2 screenPosition) => screenPosition.y > BlockAboveY;

        public event Action<string> PowerUpRequested;
        public event Action PowerUpCancelRequested;
        public event Action ReviveRequested;

        public readonly List<PowerUpButtonState> PowerUps = new List<PowerUpButtonState>();
        public bool TargetingShown;
        public ReviveOfferState ReviveOffer = ReviveOfferState.Hidden;
        public readonly List<ReviveOfferState> ReviveOfferHistory = new List<ReviveOfferState>();

        public void SetPowerUps(IReadOnlyList<PowerUpButtonState> states)
        {
            PowerUps.Clear();
            PowerUps.AddRange(states);
        }

        public PowerUpButtonState PowerUp(string id) => PowerUps.Find(state => state.Id == id);

        public void SetTargeting(bool active, string hint) => TargetingShown = active;

        public void SetReviveOffer(ReviveOfferState state)
        {
            ReviveOffer = state;
            ReviveOfferHistory.Add(state);
        }

        public void ClickPowerUp(string id) => PowerUpRequested?.Invoke(id);
        public void ClickCancelPowerUp() => PowerUpCancelRequested?.Invoke();
        public void ClickContinue() => ReviveRequested?.Invoke();

        public void ClickRestart() => RestartRequested?.Invoke();
        public void ClickPause() => PauseRequested?.Invoke();
        public void ClickResume() => ResumeRequested?.Invoke();
        public void ClickMainMenu() => MainMenuRequested?.Invoke();
    }

    /// <summary>
    /// Deterministic ad provider: requests wait until the test calls <see cref="Finish"/>, which may be called
    /// several times to imitate an SDK that reports the same result more than once.
    /// </summary>
    internal sealed class FakeAdProvider : IRewardedAdService
    {
        private readonly List<Action<RewardedAdResult>> _pending = new List<Action<RewardedAdResult>>();

        public bool IsSupported { get; set; } = true;
        public bool Ready { get; set; } = true;
        public int ShowCalls { get; private set; }
        public int PendingCount => _pending.Count;

        public bool IsReady(RewardedPlacement placement) => Ready;

        public void Show(RewardedPlacement placement, Action<RewardedAdResult> onComplete)
        {
            ShowCalls++;
            _pending.Add(onComplete);
        }

        /// <summary>Reports a result for the most recent request; the callback is kept so it can be repeated.</summary>
        public void Finish(RewardedAdResult result, int times = 1)
        {
            var callback = _pending[_pending.Count - 1];
            for (var i = 0; i < times; i++)
            {
                callback(result);
            }
        }
    }

    internal sealed class FakeNavigator : ISceneNavigator
    {
        public int MainMenuLoads;
        public int GameplayLoads;

        public bool IsLoading { get; set; }

        public bool LoadMainMenu()
        {
            if (IsLoading)
            {
                return false;
            }

            IsLoading = true;
            MainMenuLoads++;
            return true;
        }

        public bool LoadGameplay()
        {
            if (IsLoading)
            {
                return false;
            }

            IsLoading = true;
            GameplayLoads++;
            return true;
        }
    }
}
