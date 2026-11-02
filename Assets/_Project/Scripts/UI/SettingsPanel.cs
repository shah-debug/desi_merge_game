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
        [SerializeField] private Button languageButton;
        [SerializeField] private Text languageLabel;
        [SerializeField] private GameObject languageList;
        [SerializeField] private Button languageListBlocker;
        [SerializeField] private Button[] languageOptions = Array.Empty<Button>();
        [SerializeField] private Text[] languageOptionLabels = Array.Empty<Text>();
        [SerializeField] private GameObject[] languageOptionChecks = Array.Empty<GameObject>();

        private ISettingsService _settings;
        private IAudioService _audio;
        private bool _opening;
        private bool _languageWired;

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

            WireLanguagePicker();
            ShowLanguage();

            foreach (var feedback in GetComponentsInChildren<ButtonFeedback>(true))
            {
                feedback.Bind(audio);
            }

            Refresh();
        }

        public string LanguageLabel => languageLabel != null ? languageLabel.text : string.Empty;

        public bool LanguageListOpen => languageList != null && languageList.activeSelf;

        public int LanguageOptionCount => languageOptions.Length;

        /// <summary>Opens or closes the language dropdown.</summary>
        public void ToggleLanguageList()
        {
            if (LanguageListOpen)
            {
                CloseLanguageList();
            }
            else
            {
                OpenLanguageList();
            }
        }

        public void OpenLanguageList()
        {
            if (languageList == null)
            {
                return;
            }

            ShowLanguage();
            languageList.SetActive(true);
            languageList.transform.SetAsLastSibling();
        }

        public void CloseLanguageList()
        {
            if (languageList != null)
            {
                languageList.SetActive(false);
            }
        }

        /// <summary>Changes the UI language at once (every screen updates), saves it and closes the dropdown.</summary>
        public void SelectLanguage(string code)
        {
            CloseLanguageList();
            if (_settings == null || !DesiMerge.Localization.Loc.IsKnown(code))
            {
                return;
            }

            _settings.Language = code;
            DesiMerge.Localization.Loc.SetLanguage(code);
            _settings.Flush();
            ShowLanguage();
        }

        private void WireLanguagePicker()
        {
            if (_languageWired)
            {
                return;
            }

            _languageWired = true;
            languageButton?.onClick.AddListener(ToggleLanguageList);
            languageListBlocker?.onClick.AddListener(CloseLanguageList);
            var languages = DesiMerge.Localization.Loc.Languages;
            for (var i = 0; i < languageOptions.Length && i < languages.Length; i++)
            {
                var code = languages[i].Code;
                languageOptions[i]?.onClick.AddListener(() => SelectLanguage(code));
            }

            CloseLanguageList();
        }

        private void ShowLanguage()
        {
            var languages = DesiMerge.Localization.Loc.Languages;
            if (languageLabel != null)
            {
                languageLabel.text = NativeName(DesiMerge.Localization.Loc.Info(DesiMerge.Localization.Loc.Current));
            }

            // Each option is written in its own language and font, whatever the current UI language is.
            for (var i = 0; i < languageOptionLabels.Length && i < languages.Length; i++)
            {
                var label = languageOptionLabels[i];
                if (label != null)
                {
                    if (_optionFont == null)
                    {
                        _optionFont = label.font;
                    }

                    label.font = DesiMerge.Localization.Loc.FontForLanguage(languages[i].Code, _optionFont);
                    label.text = NativeName(languages[i]);
                }
            }

            for (var i = 0; i < languageOptionChecks.Length && i < languages.Length; i++)
            {
                if (languageOptionChecks[i] != null)
                {
                    languageOptionChecks[i].SetActive(languages[i].Code == DesiMerge.Localization.Loc.Current);
                }
            }
        }

        private Font _optionFont;

        private static string NativeName(DesiMerge.Localization.Loc.LanguageInfo info) =>
            info.RightToLeft ? DesiMerge.Localization.UrduShaper.Shape(info.NativeName) : info.NativeName;

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
            CloseLanguageList();
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
