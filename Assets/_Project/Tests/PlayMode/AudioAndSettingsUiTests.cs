using System.Text.RegularExpressions;
using DesiMerge.Audio;
using DesiMerge.Persistence;
using DesiMerge.Settings;
using DesiMerge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DesiMerge.Tests.PlayMode
{
    public class AudioServiceTests
    {
        private GameObject _host;
        private AudioLibrary _library;
        private AudioClip _music;
        private AudioClip _sfx;
        private SettingsService _settings;
        private AudioService _audio;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("AudioHost");
            _music = AudioClip.Create("music", 4410, 1, 44100, false);
            _sfx = AudioClip.Create("sfx", 441, 1, 44100, false);
            _library = ScriptableObject.CreateInstance<AudioLibrary>();
            _library.Set(
                new[] { new AudioLibrary.SfxEntry { cue = SfxCue.Merge, clip = _sfx, volume = 1f } },
                new[] { new AudioLibrary.MusicEntry { track = MusicTrack.Menu, clip = _music, volume = 0.5f } },
                voices: 4);
            _settings = new SettingsService(new InMemorySaveStore(), new SettingsData { musicVolume = 1f, sfxVolume = 1f });
            _audio = new AudioService(_host, _library, _settings);
        }

        [TearDown]
        public void TearDown()
        {
            _audio.Dispose();
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_library);
            Object.DestroyImmediate(_music);
            Object.DestroyImmediate(_sfx);
        }

        [Test]
        public void SourcesAreCreatedOnceAndNeverDuplicated()
        {
            var before = _host.GetComponents<AudioSource>().Length;

            for (var i = 0; i < 20; i++)
            {
                _audio.PlayMusic(MusicTrack.Menu);
                _audio.PlaySfx(SfxCue.Merge, 1f + i * 0.01f);
            }

            Assert.AreEqual(5, before, "1 music + 4 SFX voices.");
            Assert.AreEqual(before, _host.GetComponents<AudioSource>().Length);
        }

        [Test]
        public void PlayingTheSameTrackAgain_KeepsTheCurrentClip()
        {
            _audio.PlayMusic(MusicTrack.Menu);
            var source = _audio.MusicSource;

            _audio.PlayMusic(MusicTrack.Menu);

            Assert.AreSame(_music, source.clip);
            Assert.AreEqual(MusicTrack.Menu, _audio.CurrentTrack);
        }

        [Test]
        public void MusicVolumeFollowsSettingsAndMute()
        {
            _audio.PlayMusic(MusicTrack.Menu);
            Assert.AreEqual(0.5f, _audio.MusicSource.volume, 0.0001f);

            _settings.MusicVolume = 0.4f;
            Assert.AreEqual(0.2f, _audio.MusicSource.volume, 0.0001f);

            _settings.MusicMuted = true;
            Assert.AreEqual(0f, _audio.MusicSource.volume);

            _settings.MusicMuted = false;
            Assert.AreEqual(0.2f, _audio.MusicSource.volume, 0.0001f, "Unmuting restores the previous level.");
        }

        [Test]
        public void SfxMute_SkipsPlayback()
        {
            _settings.SfxMuted = true;

            _audio.PlaySfx(SfxCue.Merge);

            foreach (var source in _host.GetComponents<AudioSource>())
            {
                Assert.AreNotSame(_sfx, source.clip);
            }
        }

        [Test]
        public void MissingClips_WarnOnceAndStaySilent()
        {
            LogAssert.Expect(LogType.Warning, new Regex("No clip assigned for SFX 'Drop'"));
            LogAssert.Expect(LogType.Warning, new Regex("No clip assigned for music 'Gameplay'"));

            for (var i = 0; i < 5; i++)
            {
                _audio.PlaySfx(SfxCue.Drop);
                _audio.PlayMusic(MusicTrack.Gameplay);
            }

            LogAssert.NoUnexpectedReceived();
            Assert.AreEqual(MusicTrack.None, _audio.CurrentTrack);
        }

        [Test]
        public void Dispose_StopsListeningToSettings()
        {
            _audio.PlayMusic(MusicTrack.Menu);
            _audio.Dispose();

            _settings.MusicMuted = true;

            Assert.AreEqual(0.5f, _audio.MusicSource.volume, 0.0001f);
        }
    }

    public class SettingsPanelTests
    {
        private GameObject _root;
        private Slider _music;
        private Slider _sfx;
        private Toggle _musicMute;
        private Toggle _sfxMute;
        private InMemorySaveStore _store;
        private SettingsService _settings;
        private SettingsPanel _panel;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("SettingsPanel", typeof(RectTransform));
            _music = Create<Slider>("Music");
            _sfx = Create<Slider>("Sfx");
            _musicMute = Create<Toggle>("MusicMute");
            _sfxMute = Create<Toggle>("SfxMute");
            _store = new InMemorySaveStore();
            _settings = new SettingsService(_store, new SettingsData { musicVolume = 0.25f, sfxVolume = 0.75f });
            _panel = _root.AddComponent<SettingsPanel>();
            _panel.SetReferencesForTests(_music, _musicMute, _sfx, _sfxMute, null);
            _panel.Bind(_settings, new SilentAudioService());
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void Bind_ShowsStoredValues()
        {
            Assert.AreEqual(0.25f, _music.value, 0.0001f);
            Assert.AreEqual(0.75f, _sfx.value, 0.0001f);
            Assert.IsFalse(_musicMute.isOn);
        }

        [Test]
        public void Controls_WriteToTheExistingSettingsService()
        {
            _music.value = 0.6f;
            _sfxMute.isOn = true;

            Assert.AreEqual(0.6f, _settings.MusicVolume, 0.0001f);
            Assert.IsTrue(_settings.SfxMuted);
        }

        [Test]
        public void ExternalChanges_UpdateControls()
        {
            _settings.SfxVolume = 0.1f;
            _settings.MusicMuted = true;

            Assert.AreEqual(0.1f, _sfx.value, 0.0001f);
            Assert.IsTrue(_musicMute.isOn);
        }

        [Test]
        public void Close_PersistsSettings()
        {
            _panel.Open();
            _music.value = 0.9f;

            _panel.Close();

            Assert.IsFalse(_panel.IsOpen);
            var reloaded = new SettingsService(_store, new SettingsData());
            Assert.AreEqual(0.9f, reloaded.MusicVolume, 0.0001f);
        }

        private T Create<T>(string name) where T : Selectable
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(_root.transform, false);
            return child.AddComponent<T>();
        }
    }
}
