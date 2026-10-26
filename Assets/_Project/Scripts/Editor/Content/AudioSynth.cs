using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DesiMerge.Editor.Content
{
    /// <summary>
    /// Synthesizes the placeholder audio as 16-bit mono WAV files: four short effects and two loopable music beds
    /// (A-major pentatonic plucks over a tanpura-like drone with a soft dholak-like pulse). Everything is generated
    /// from code, so there are no licensing concerns; replace the files at the same paths with real audio later.
    /// Existing files are kept.
    /// </summary>
    internal static class AudioSynth
    {
        public const string SfxFolder = "Assets/_Project/Audio/Sfx";
        public const string MusicFolder = "Assets/_Project/Audio/Music";
        public const string DropPath = SfxFolder + "/drop.wav";
        public const string MergePath = SfxFolder + "/merge.wav";
        public const string ButtonPath = SfxFolder + "/button_tap.wav";
        public const string GameOverPath = SfxFolder + "/game_over.wav";
        public const string MenuMusicPath = MusicFolder + "/menu_loop.wav";
        public const string GameplayMusicPath = MusicFolder + "/gameplay_loop.wav";

        private const int SfxRate = 44100;
        private const int MusicRate = 22050;
        private const float Root = 110f; // A2 drone

        // A major pentatonic (Bhupali-like) around A4.
        private static readonly float[] Scale = { 440f, 493.88f, 554.37f, 659.25f, 739.99f, 880f, 987.77f };

        public static void EnsureAll()
        {
            Ensure(DropPath, SfxRate, Drop, false);
            Ensure(MergePath, SfxRate, Merge, false);
            Ensure(ButtonPath, SfxRate, Button, false);
            Ensure(GameOverPath, SfxRate, GameOver, false);
            // Loop lengths make every drone frequency complete whole cycles, so the loop point is seamless.
            Ensure(MenuMusicPath, MusicRate, () => Music(80f, 16, new[] { 0, -1, 2, -1, 3, 2, 1, -1, 0, -1, 4, 3, 2, -1, 1, -1 }, 0.55f), true);
            Ensure(GameplayMusicPath, MusicRate, () => Music(120f, 32, new[]
            {
                0, 2, 3, -1, 4, 3, 2, -1, 3, 4, 5, -1, 4, 3, 2, 1,
                0, 2, 3, -1, 5, 4, 3, -1, 2, 3, 1, -1, 0, -1, 0, -1,
            }, 0.5f), true);
        }

        private static void Ensure(string path, int rate, Func<float[]> render, bool music)
        {
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(path) != null)
            {
                return;
            }

            ContentPaths.EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var samples = render();
            Normalize(samples, 0.7f);
            WriteWav(path, samples, rate);
            AssetDatabase.ImportAsset(path);

            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            importer.forceToMono = true;
            importer.loadInBackground = music;
            importer.defaultSampleSettings = new AudioImporterSampleSettings
            {
                loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad,
                compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM,
                quality = 0.6f,
                sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
            };
            importer.SaveAndReimport();
        }

        private static float[] Drop()
        {
            var samples = Allocate(SfxRate, 0.16f);
            var random = new System.Random(1);
            var phase = 0.0;
            for (var i = 0; i < samples.Length; i++)
            {
                var t = (float)i / SfxRate;
                var freq = Mathf.Lerp(320f, 120f, t / 0.16f);
                phase += 2.0 * Math.PI * freq / SfxRate;
                var body = (float)Math.Sin(phase) * Mathf.Exp(-t * 28f);
                var noise = (float)(random.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 90f) * 0.3f;
                samples[i] = (body + noise) * Attack(t, 0.003f);
            }

            return samples;
        }

        private static float[] Merge()
        {
            var samples = Allocate(SfxRate, 0.32f);
            AddBell(samples, SfxRate, 0f, 659.25f, 18f, 0.8f);
            AddBell(samples, SfxRate, 0.07f, 880f, 12f, 1f);
            return samples;
        }

        private static float[] Button()
        {
            var samples = Allocate(SfxRate, 0.07f);
            for (var i = 0; i < samples.Length; i++)
            {
                var t = (float)i / SfxRate;
                samples[i] = Mathf.Sin(2f * Mathf.PI * 1250f * t) * Mathf.Exp(-t * 70f) * Attack(t, 0.002f);
            }

            return samples;
        }

        private static float[] GameOver()
        {
            var samples = Allocate(SfxRate, 1.6f);
            AddPluck(samples, SfxRate, 0f, 659.25f, 3.5f, 0.9f);
            AddPluck(samples, SfxRate, 0.28f, 554.37f, 3.5f, 0.9f);
            AddPluck(samples, SfxRate, 0.56f, 440f, 2.2f, 1f);
            for (var i = 0; i < samples.Length; i++)
            {
                var t = (float)i / SfxRate;
                samples[i] += Mathf.Sin(2f * Mathf.PI * Root * t) * 0.25f * Mathf.Exp(-t * 1.8f) * Attack(t, 0.05f);
            }

            return samples;
        }

        /// <param name="pattern">Scale degree per beat, -1 = rest.</param>
        private static float[] Music(float bpm, int beats, int[] pattern, float melodyLevel)
        {
            var beat = 60f / bpm;
            var length = beat * beats;
            var samples = Allocate(MusicRate, length);

            // Drone: root, fifth and octave with a slow swell (tanpura-like).
            for (var i = 0; i < samples.Length; i++)
            {
                var t = (float)i / MusicRate;
                var swell = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * t / length * 2f);
                samples[i] = swell * 0.22f * (Mathf.Sin(2f * Mathf.PI * Root * t)
                                              + 0.6f * Mathf.Sin(2f * Mathf.PI * Root * 1.5f * t)
                                              + 0.35f * Mathf.Sin(2f * Mathf.PI * Root * 2f * t));
            }

            // Melody plucks.
            for (var b = 0; b < beats; b++)
            {
                var degree = pattern[b % pattern.Length];
                if (degree >= 0)
                {
                    AddPluck(samples, MusicRate, b * beat, Scale[degree], 4.5f, melodyLevel);
                }
            }

            // Dholak-like pulse: low "dha" on beats 1 and 3, light "na" taps on the off-beats.
            var random = new System.Random(7);
            for (var b = 0; b < beats; b++)
            {
                AddDrum(samples, MusicRate, b * beat, b % 2 == 0, random);
                AddDrum(samples, MusicRate, (b + 0.5f) * beat, false, random);
            }

            return samples;
        }

        private static void AddPluck(float[] samples, int rate, float start, float freq, float decay, float level)
        {
            var first = (int)(start * rate);
            for (var i = first; i < samples.Length; i++)
            {
                var t = (float)(i - first) / rate;
                var envelope = Mathf.Exp(-t * decay) * Attack(t, 0.004f);
                if (envelope < 0.0005f)
                {
                    break;
                }

                var w = 2f * Mathf.PI * freq * t;
                samples[i] += level * envelope * 0.35f * (Mathf.Sin(w) + 0.5f * Mathf.Sin(2f * w) * Mathf.Exp(-t * 3f) + 0.25f * Mathf.Sin(3f * w) * Mathf.Exp(-t * 6f));
            }
        }

        private static void AddBell(float[] samples, int rate, float start, float freq, float decay, float level)
        {
            var first = (int)(start * rate);
            for (var i = first; i < samples.Length; i++)
            {
                var t = (float)(i - first) / rate;
                var w = 2f * Mathf.PI * freq * t;
                samples[i] += level * Mathf.Exp(-t * decay) * Attack(t, 0.002f) * 0.5f * (Mathf.Sin(w) + 0.4f * Mathf.Sin(2.76f * w) + 0.2f * Mathf.Sin(5.4f * w));
            }
        }

        private static void AddDrum(float[] samples, int rate, float start, bool low, System.Random random)
        {
            var first = (int)(start * rate);
            var length = (int)((low ? 0.22f : 0.06f) * rate);
            var phase = 0.0;
            for (var n = 0; n < length && first + n < samples.Length; n++)
            {
                var t = (float)n / rate;
                if (low)
                {
                    phase += 2.0 * Math.PI * Mathf.Lerp(95f, 55f, t / 0.22f) / rate;
                    samples[first + n] += 0.5f * (float)Math.Sin(phase) * Mathf.Exp(-t * 16f) * Attack(t, 0.002f);
                }
                else
                {
                    samples[first + n] += 0.08f * (float)(random.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 70f);
                }
            }
        }

        private static float Attack(float t, float time) => t >= time ? 1f : t / time;

        private static float[] Allocate(int rate, float seconds) => new float[Mathf.RoundToInt(rate * seconds)];

        private static void Normalize(float[] samples, float peak)
        {
            var max = 0f;
            foreach (var sample in samples)
            {
                max = Mathf.Max(max, Mathf.Abs(sample));
            }

            if (max <= 0f)
            {
                return;
            }

            var gain = peak / max;
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] *= gain;
            }
        }

        private static void WriteWav(string path, float[] samples, int rate)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                var dataBytes = samples.Length * 2;
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataBytes);
                writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1); // PCM
                writer.Write((short)1); // mono
                writer.Write(rate);
                writer.Write(rate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataBytes);
                foreach (var sample in samples)
                {
                    writer.Write((short)Mathf.Clamp(Mathf.RoundToInt(sample * short.MaxValue), short.MinValue, short.MaxValue));
                }
            }
        }
    }
}
