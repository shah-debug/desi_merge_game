using System.Collections.Generic;
using DesiMerge.Configuration;
using DesiMerge.Core;
using DesiMerge.Platform;
using DesiMerge.UI;
using UnityEngine;

namespace DesiMerge.Gameplay
{
    /// <summary>
    /// Wires the Gameplay scene. Receives services from the bootstrapper, validates configuration and scene references,
    /// then creates the <see cref="GameplayLoop"/>. This is the only gameplay class that references a concrete UI type.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayEntryPoint : MonoBehaviour, ISceneEntryPoint
    {
        [SerializeField] private GameplayConfig gameplayConfig;
        [SerializeField] private MergeBoard board;
        [SerializeField] private DropController dropController;
        [SerializeField] private GameplayHud hud;

        [Tooltip("Editor/development only: config used when this scene is played without Bootstrap.")]
        [SerializeField] private GameConfig developmentConfig;

        private bool _initializeAttempted;
        private GameServices _ownedServices;
        private BackButton _backButton;

        public GameplayLoop Loop { get; private set; }
        public MergeBoard Board => board;
        public GameplayConfig Config => gameplayConfig;
        public GameplayHud Hud => hud;
        public GameServices Services { get; private set; }

        public void Initialize(GameServices services)
        {
            if (_initializeAttempted)
            {
                return;
            }

            _initializeAttempted = true;
            Services = services;
            var errors = ValidateSetup();
            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    Debug.LogError($"[Gameplay] {error}", this);
                }

                if (dropController != null)
                {
                    dropController.InputEnabled = false;
                }

                if (hud != null)
                {
                    hud.ShowConfigurationError("Gameplay configuration is invalid. See the Console.");
                }

                return;
            }

            board.Initialize(gameplayConfig, () => Time.time);
            dropController.Configure(gameplayConfig, board.transform);
            hud.BindAudio(services.Audio);
            Loop = new GameplayLoop(gameplayConfig, board, dropController, hud, services, () => Time.time);
            Loop.Begin();
        }

        private void Start()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Lets the Gameplay scene be played directly in the Editor without the Bootstrap scene.
            if (!_initializeAttempted)
            {
                Debug.LogWarning("[Gameplay] Started without Bootstrap; using temporary in-memory development services.", this);
                _ownedServices = DevelopmentServices.Create(developmentConfig, gameObject);
                Initialize(_ownedServices);
            }
#endif
        }

        private void OnEnable() => _backButton = new BackButton(OnBackPressed);

        private void OnDisable()
        {
            _backButton?.Dispose();
            _backButton = null;
        }

        private void OnBackPressed() => Loop?.HandleBack();

        /// <summary>Pause when the app is backgrounded. Editor focus changes are ignored so clicking another window doesn't pause.</summary>
        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Loop?.HandleAppInterrupted();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && !Application.isEditor)
            {
                Loop?.HandleAppInterrupted();
            }
        }

        private void OnDestroy()
        {
            Loop?.Dispose();
            Loop = null;
            _ownedServices?.Dispose();
            _ownedServices = null;
        }

        private List<string> ValidateSetup()
        {
            var errors = new List<string>();
            if (gameplayConfig == null)
            {
                errors.Add("GameplayConfig is not assigned on the entry point.");
            }
            else
            {
                errors.AddRange(gameplayConfig.Validate());
            }

            if (board == null)
            {
                errors.Add("MergeBoard is not assigned on the entry point.");
            }

            if (dropController == null)
            {
                errors.Add("DropController is not assigned on the entry point.");
            }

            if (hud == null)
            {
                errors.Add("GameplayHud is not assigned on the entry point.");
            }

            return errors;
        }

        /// <summary>Test hook for building the scene in code.</summary>
        internal void SetReferencesForTests(GameplayConfig config, MergeBoard mergeBoard, DropController drop, GameplayHud gameplayHud)
        {
            gameplayConfig = config;
            board = mergeBoard;
            dropController = drop;
            hud = gameplayHud;
        }
    }
}
