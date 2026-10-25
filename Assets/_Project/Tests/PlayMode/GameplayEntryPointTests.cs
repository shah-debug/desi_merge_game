using System.Text.RegularExpressions;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Gameplay;
using DesiMerge.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DesiMerge.Tests.PlayMode
{
    public class GameplayEntryPointTests
    {
        private GameObject _entryObject;
        private GameConfig _gameConfig;
        private GameServices _services;

        [SetUp]
        public void SetUp()
        {
            _gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            _services = GameServicesFactory.Create(_gameConfig, new InMemorySaveStore(), isDevelopmentBuild: false);
            _entryObject = new GameObject("Entry");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_entryObject);
            Object.DestroyImmediate(_gameConfig);
        }

        [Test]
        public void MissingReferences_LogErrorsAndDoNotStart()
        {
            var entry = _entryObject.AddComponent<GameplayEntryPoint>();
            LogAssert.Expect(LogType.Error, new Regex("GameplayConfig is not assigned"));
            LogAssert.Expect(LogType.Error, new Regex("MergeBoard is not assigned"));
            LogAssert.Expect(LogType.Error, new Regex("DropController is not assigned"));
            LogAssert.Expect(LogType.Error, new Regex("GameplayHud is not assigned"));

            entry.Initialize(_services);

            Assert.IsNull(entry.Loop);
            Assert.AreEqual(GameState.Boot, _services.GameState.Current, "A broken scene must not enter Playing.");
        }

        [Test]
        public void InvalidProgression_LogsErrorAndDoesNotStart()
        {
            using (var rig = new GameplayTestRig())
            {
                rig.Progression.Set(new MergeItemDefinition[] { rig.Progression.Get(0), null }, 1);
                var entry = _entryObject.AddComponent<GameplayEntryPoint>();
                var hud = new GameObject("Hud").AddComponent<DesiMerge.UI.GameplayHud>();
                entry.SetReferencesForTests(rig.Config, rig.Board, rig.Drop, hud);
                LogAssert.Expect(LogType.Error, new Regex("Level 1 is missing its definition"));

                entry.Initialize(_services);

                Assert.IsNull(entry.Loop);
                Assert.IsFalse(rig.Drop.InputEnabled);
                Object.DestroyImmediate(hud.gameObject);
            }
        }

        [Test]
        public void SecondInitialize_IsIgnored()
        {
            var entry = _entryObject.AddComponent<GameplayEntryPoint>();
            LogAssert.Expect(LogType.Error, new Regex("GameplayConfig is not assigned"));
            LogAssert.Expect(LogType.Error, new Regex("MergeBoard is not assigned"));
            LogAssert.Expect(LogType.Error, new Regex("DropController is not assigned"));
            LogAssert.Expect(LogType.Error, new Regex("GameplayHud is not assigned"));
            entry.Initialize(_services);

            // A second call (e.g. a scene re-initialized) must not validate, log or start again.
            entry.Initialize(_services);

            LogAssert.NoUnexpectedReceived();
            Assert.IsNull(entry.Loop);
        }
    }
}
