using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DesiMerge.Core
{
    public sealed class SceneNavigator : ISceneNavigator
    {
        private readonly string _mainMenuScene;
        private readonly string _gameplayScene;
        private readonly Action<AsyncOperation> _onLoaded;

        public SceneNavigator(string mainMenuScene, string gameplayScene)
        {
            _mainMenuScene = mainMenuScene;
            _gameplayScene = gameplayScene;
            _onLoaded = _ => IsLoading = false;
        }

        public bool IsLoading { get; private set; }

        public bool LoadMainMenu() => Load(_mainMenuScene);

        public bool LoadGameplay() => Load(_gameplayScene);

        private bool Load(string sceneName)
        {
            if (IsLoading)
            {
                return false;
            }

            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[Navigation] Scene '{sceneName}' is not in Build Settings.");
                return false;
            }

            IsLoading = true;
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single).completed += _onLoaded;
            return true;
        }
    }
}
