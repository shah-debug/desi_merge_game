using DesiMerge.Audio;
using DesiMerge.Persistence;
using DesiMerge.Settings;
using DesiMerge.UI;
using DesiMerge.UI.Animation;
using NUnit.Framework;
using UnityEngine;

namespace DesiMerge.Tests.EditMode
{
    public class PresentationLogicTests
    {
        [Test]
        public void Settings_MuteFlagsPersistAndRaiseChanged()
        {
            var store = new InMemorySaveStore();
            var settings = new SettingsService(store, new SettingsData());
            var changes = 0;
            settings.Changed += () => changes++;

            settings.MusicMuted = true;
            settings.SfxMuted = true;
            settings.MusicMuted = true; // no change
            settings.Flush();

            Assert.AreEqual(2, changes);
            var reloaded = new SettingsService(store, new SettingsData());
            Assert.IsTrue(reloaded.MusicMuted);
            Assert.IsTrue(reloaded.SfxMuted);
        }

        [Test]
        public void Settings_Version1SaveLoadsWithMuteOffAndKeepsVolumes()
        {
            var store = new InMemorySaveStore();
            store.Write(SaveKey.Settings, "{\"schemaVersion\":1,\"musicVolume\":0.3,\"sfxVolume\":0.6,\"hapticsEnabled\":false}");

            var settings = new SettingsService(store, new SettingsData());

            Assert.IsFalse(settings.MusicMuted);
            Assert.IsFalse(settings.SfxMuted);
            Assert.AreEqual(0.3f, settings.MusicVolume, 0.0001f);
            Assert.AreEqual(0.6f, settings.SfxVolume, 0.0001f);
        }

        [TestCase(1f, 1f, false, 1f)]
        [TestCase(0.5f, 0.8f, false, 0.4f)]
        [TestCase(1f, 1f, true, 0f)]
        [TestCase(2f, -1f, false, 0f)]
        public void AudioMix_CombinesBaseVolumeChannelAndMute(float baseVolume, float channel, bool muted, float expected)
        {
            Assert.AreEqual(expected, AudioMix.Effective(baseVolume, channel, muted), 0.0001f);
        }

        [Test]
        public void AudioLibrary_EntriesWithoutClipsAreTreatedAsMissing()
        {
            var library = ScriptableObject.CreateInstance<AudioLibrary>();
            var clip = AudioClip.Create("test", 100, 1, 44100, false);
            library.Set(
                new[] { new AudioLibrary.SfxEntry { cue = SfxCue.Drop, clip = null, volume = 1f }, new AudioLibrary.SfxEntry { cue = SfxCue.Merge, clip = clip, volume = 0.5f } },
                new AudioLibrary.MusicEntry[0]);

            Assert.IsFalse(library.TryGetSfx(SfxCue.Drop, out _, out _));
            Assert.IsTrue(library.TryGetSfx(SfxCue.Merge, out var found, out var volume));
            Assert.AreSame(clip, found);
            Assert.AreEqual(0.5f, volume);
            Assert.IsFalse(library.TryGetMusic(MusicTrack.Menu, out _, out _));

            Object.DestroyImmediate(library);
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void SafeArea_FullScreenGivesFullAnchors()
        {
            SafeAreaMath.ToAnchors(new Rect(0f, 0f, 1080f, 2340f), 1080f, 2340f, out var min, out var max);

            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }

        [Test]
        public void SafeArea_NotchAndRoundedCornersAreInset()
        {
            // 1080 x 2340 phone with a 100 px notch at the top and a 60 px gesture area at the bottom.
            SafeAreaMath.ToAnchors(new Rect(0f, 60f, 1080f, 2180f), 1080f, 2340f, out var min, out var max);

            Assert.AreEqual(0f, min.x, 0.0001f);
            Assert.AreEqual(60f / 2340f, min.y, 0.0001f);
            Assert.AreEqual(1f, max.x, 0.0001f);
            Assert.AreEqual(2240f / 2340f, max.y, 0.0001f);
        }

        [Test]
        public void SafeArea_InvalidScreenFallsBackToFullAnchors()
        {
            SafeAreaMath.ToAnchors(new Rect(0f, 0f, 10f, 10f), 0f, 0f, out var min, out var max);

            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }

        [TestCase(1080f, 1920f, 1f)]   // 9:16 reference
        [TestCase(1080f, 2340f, 1f)]   // taller phone: never scaled up
        [TestCase(1080f, 1440f, 0.75f)] // 3:4 tablet: shrink to fit height
        [TestCase(900f, 1920f, 0.8333f)]
        [TestCase(0f, 0f, 1f)]         // not laid out yet
        public void ResponsiveScaler_ShrinksOnlyWhenNeeded(float width, float height, float expected)
        {
            Assert.AreEqual(expected, ResponsiveScaler.ScaleFor(new Vector2(width, height), new Vector2(1080f, 1920f)), 0.001f);
        }

        [TestCase(9f / 16f, 8f)]        // 9:16 keeps the designed 16-unit-tall view
        [TestCase(9f / 19.5f, 8.667f)] // tall phone: grow so 8 units of width fit
        [TestCase(3f / 4f, 8f)]         // tablet: width already fits
        public void CameraFit_KeepsBoardWidthVisible(float aspect, float expectedSize)
        {
            Assert.AreEqual(expectedSize, DesiMerge.Gameplay.CameraFit.RequiredOrthographicSize(aspect, 8f, 16f), 0.001f);
        }

        [Test]
        public void Ease_CurvesStartAtZeroAndEndAtOne()
        {
            Assert.AreEqual(0f, Ease.OutCubic(0f), 0.0001f);
            Assert.AreEqual(1f, Ease.OutCubic(1f), 0.0001f);
            Assert.AreEqual(0f, Ease.InCubic(0f), 0.0001f);
            Assert.AreEqual(1f, Ease.InCubic(1f), 0.0001f);
            Assert.AreEqual(0f, Ease.OutBack(0f), 0.0001f);
            Assert.AreEqual(1f, Ease.OutBack(1f), 0.0001f);
            Assert.Greater(Ease.OutBack(0.7f), 1f, "OutBack should overshoot.");
            Assert.AreEqual(0f, Ease.Punch(0f), 0.0001f);
            Assert.AreEqual(0f, Ease.Punch(1f), 0.0001f);
        }
    }
}
