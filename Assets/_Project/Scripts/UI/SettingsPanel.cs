using System;
using DesiMerge.Audio;
using DesiMerge.Settings;
using DesiMerge.UI.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace DesiMerge.UI
{
    /// <summary>
    /// Music/SFX volume and mute, bound to the existing <see cref="ISettingsService"/>. Changes apply immediately
    /// (the audio service listens to the settings) and are saved when the panel closes.
    /// No haptics control: haptic feedback is not implemented yet.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private PanelTransition transition;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Toggle musicMuteToggle;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Toggle sfxMuteToggle;
        [SerializeField] private Button closeButton;

        private ISettingsService _settings;
        private IAudioService _audio;
        private bool _opening;

        public event Action Closed;

        public bool IsOpen => transition != null ? transition.IsVisible : gameObject.activeSelf;

        private void Awake()
        {
            // If the panel starts inactive, Awake runs during Open(); don't undo that open.
            if (_opening)
            {
                return;
            }

            if (transition != null)
            {
                transition.SetVisibleImmediate(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void Bind(ISettingsService settings, IAudioService audio)
        {
            Unbind();
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _audio = audio;
            _settings.Changed += Refresh;

            if (musicSlider != null)
            {
                musicSlider.onValueChanged.AddListener(OnMusicVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.AddListener(OnSfxVolume);
            }

            if (musicMuteToggle != null)
            {
                musicMuteToggle.onValueChanged.AddListener(OnMusicMute);
            }

            if (sfxMuteToggle != null)
            {
                sfxMuteToggle.onValueChanged.AddListener(OnSfxMute);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            foreach (var feedback in GetComponentsInChildren<ButtonFeedback>(true))
            {
                feedback.Bind(audio);
            }

            Refresh();
        }

        public void Open()
        {
            Refresh();
            _opening = true;
            if (transition != null)
            {
                transition.Show();
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            _settings?.Flush();
            if (transition != null)
            {
                transition.Hide();
            }
            else
            {
                gameObject.SetActive(false);
            }

            Closed?.Invoke();
        }

        private void OnDestroy() => Unbind();

        private void Unbind()
        {
            if (_settings == null)
            {
                return;
            }

            _settings.Changed -= Refresh;
            if (musicSlider != null)
            {
                musicSlider.onValueChanged.RemoveListener(OnMusicVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveListener(OnSfxVolume);
            }

            if (musicMuteToggle != null)
            {
                musicMuteToggle.onValueChanged.RemoveListener(OnMusicMute);
            }

            if (sfxMuteToggle != null)
            {
                sfxMuteToggle.onValueChanged.RemoveListener(OnSfxMute);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }

            _settings = null;
        }

        /// <summary>Shows the stored values without re-triggering listeners.</summary>
        private void Refresh()
        {
            if (_settings == null)
            {
                return;
            }

            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(_settings.MusicVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(_settings.SfxVolume);
            }

            if (musicMuteToggle != null)
            {
                musicMuteToggle.SetIsOnWithoutNotify(_settings.MusicMuted);
            }

            if (sfxMuteToggle != null)
            {
                sfxMuteToggle.SetIsOnWithoutNotify(_settings.SfxMuted);
            }
        }

        private void OnMusicVolume(float value) => _settings.MusicVolume = value;

        private void OnSfxVolume(float value) => _settings.SfxVolume = value;

        private void OnMusicMute(bool muted) => _settings.MusicMuted = muted;

        private void OnSfxMute(bool muted)
        {
            _settings.SfxMuted = muted;
            if (!muted)
            {
                _audio?.PlaySfx(SfxCue.ButtonTap);
            }
        }

        internal void SetReferencesForTests(Slider music, Toggle musicMute, Slider sfx, Toggle sfxMute, Button close)
        {
            musicSlider = music;
            musicMuteToggle = musicMute;
            sfxSlider = sfx;
            sfxMuteToggle = sfxMute;
            closeButton = close;
        }
    }
}
