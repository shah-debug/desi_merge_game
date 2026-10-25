using System.Collections;
using System.Text.RegularExpressions;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DesiMerge.Tests.PlayMode
{
    public class BootstrapTests
    {
        private GameConfig _config;
        private InMemorySaveStore _store;
        private GameObject _bootstrapObject;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<GameConfig>();
            _store = new InMemorySaveStore();
        }

        [TearDown]
        public void TearDown()
        {
            // Immediate, so the bootstrapper's singleton slot is free before the next test starts.
            if (_bootstrapObject != null)
            {
                Object.DestroyImmediate(_bootstrapObject);
            }

            Object.DestroyImmediate(_config);
        }

        [UnityTest]
        public IEnumerator Bootstrapper_BuildsServicesAndSurvivesSceneLoads()
        {
            var bootstrapper = CreateBootstrapper(_config);

            yield return null;

            Assert.IsNotNull(bootstrapper.Services);
            Assert.AreEqual(GameState.Boot, bootstrapper.Services.GameState.Current);
            Assert.AreEqual("DontDestroyOnLoad", bootstrapper.gameObject.scene.name);
        }

        [UnityTest]
        public IEnumerator SecondBootstrapper_DestroysItself()
        {
            var first = CreateBootstrapper(_config);
            var secondObject = new GameObject("Duplicate");
            secondObject.SetActive(false);
            secondObject.AddComponent<GameBootstrapper>().Configure(_config, _store);
            secondObject.SetActive(true);

            yield return null;

            Assert.IsTrue(secondObject == null, "Duplicate bootstrapper should be destroyed.");
            Assert.IsNotNull(first.Services);
        }

        [UnityTest]
        public IEnumerator MissingConfig_LogsErrorAndDisables()
        {
            LogAssert.Expect(LogType.Error, new Regex("GameConfig is not assigned"));

            var bootstrapper = CreateBootstrapper(null);
            yield return null;

            Assert.IsFalse(bootstrapper.enabled);
            Assert.IsNull(bootstrapper.Services);
        }

        [UnityTest]
        public IEnumerator ApplicationPause_FlushesPendingProgress()
        {
            var bootstrapper = CreateBootstrapper(_config);
            yield return null;

            bootstrapper.Services.Progress.AddCoins(25);
            bootstrapper.gameObject.SendMessage("OnApplicationPause", true);

            Assert.IsTrue(_store.TryRead(SaveKey.PlayerProgress, out var json));
            StringAssert.Contains("25", json);
        }

        [UnityTest]
        public IEnumerator SceneEntryPoints_AreInitializedOnlyWithinTheGivenScene()
        {
            var bootstrapper = CreateBootstrapper(_config);
            yield return null;

            var scene = SceneManager.CreateScene("EntryPointTestScene");
            var inScene = new GameObject("InScene").AddComponent<RecordingEntryPoint>();
            SceneManager.MoveGameObjectToScene(inScene.gameObject, scene);
            var nested = new GameObject("Nested").AddComponent<RecordingEntryPoint>();
            nested.transform.SetParent(inScene.transform);
            var outside = new GameObject("Outside").AddComponent<RecordingEntryPoint>();

            var count = SceneEntryPoints.InitializeAll(scene, bootstrapper.Services);

            Assert.AreEqual(2, count);
            Assert.AreSame(bootstrapper.Services, inScene.Received);
            Assert.AreSame(bootstrapper.Services, nested.Received);
            Assert.IsNull(outside.Received);

            Object.Destroy(outside.gameObject);
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        private GameBootstrapper CreateBootstrapper(GameConfig config)
        {
            _bootstrapObject = new GameObject("Bootstrap");
            _bootstrapObject.SetActive(false);
            var bootstrapper = _bootstrapObject.AddComponent<GameBootstrapper>();
            bootstrapper.Configure(config, _store);
            _bootstrapObject.SetActive(true);
            return bootstrapper;
        }
    }
}
