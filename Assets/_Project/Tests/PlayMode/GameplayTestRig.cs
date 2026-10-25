using System;
using System.Collections.Generic;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Gameplay;
using DesiMerge.Persistence;
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
            var gameConfig = Track(ScriptableObject.CreateInstance<GameConfig>());
            Services = GameServicesFactory.Create(gameConfig, Store, isDevelopmentBuild: false);
        }

        public float Now { get; set; }
        public MergeProgression Progression { get; }
        public GameplayConfig Config { get; }
        public MergeBoard Board { get; }
        public DropController Drop { get; }
        public FakeView View { get; }
        public InMemorySaveStore Store { get; }
        public GameServices Services { get; }
        public GameplayLoop Loop { get; private set; }

        public GameplayLoop CreateLoop()
        {
            Loop = new GameplayLoop(Config, Board, Drop, View, Services, () => Now);
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

        public long LastScore = -1;
        public long LastBest = -1;
        public MergeItemDefinition LastNext;
        public int GameOverShown;
        public int GameOverHidden;
        public bool? LastWasNewBest;
        public string ConfigurationError;

        public void SetScore(long score) => LastScore = score;
        public void SetBestScore(long bestScore) => LastBest = bestScore;
        public void SetNextItem(MergeItemDefinition definition) => LastNext = definition;

        public void ShowGameOver(long finalScore, long bestScore, bool isNewBest)
        {
            GameOverShown++;
            LastWasNewBest = isNewBest;
        }

        public void HideGameOver() => GameOverHidden++;
        public void ShowConfigurationError(string message) => ConfigurationError = message;
        public void ClickRestart() => RestartRequested?.Invoke();
    }
}
