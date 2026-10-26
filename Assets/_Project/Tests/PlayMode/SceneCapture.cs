using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DesiMerge.Gameplay;
using DesiMerge.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DesiMerge.Tests.PlayMode
{
    /// <summary>
    /// Renders a loaded scene (camera + its canvases, temporarily switched to camera space) to a PNG when the
    /// DESIMERGE_CAPTURE_DIR environment variable is set. Used for visual review of layouts; never asserts.
    /// </summary>
    internal static class SceneCapture
    {
        /// <param name="simulateNotch">Pretends the device has a top notch and bottom gesture bar (safe-area check).</param>
        public static IEnumerator Capture(Scene scene, string fileName, int width, int height, bool simulateNotch = false)
        {
            var directory = Environment.GetEnvironmentVariable("DESIMERGE_CAPTURE_DIR");
            if (string.IsNullOrEmpty(directory))
            {
                yield break;
            }

            Camera camera = null;
            var canvases = new List<Canvas>();
            var fitters = new List<SafeAreaFitter>();
            var scalers = new List<ResponsiveScaler>();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (camera == null)
                {
                    camera = root.GetComponentInChildren<Camera>();
                }

                canvases.AddRange(root.GetComponentsInChildren<Canvas>());
                fitters.AddRange(root.GetComponentsInChildren<SafeAreaFitter>());
                scalers.AddRange(root.GetComponentsInChildren<ResponsiveScaler>());
            }

            if (camera == null)
            {
                yield break;
            }

            if (simulateNotch)
            {
                SafeAreaFitter.SimulatedSafeArea = new Rect(0f, Screen.height * 0.025f, Screen.width, Screen.height * 0.93f);
            }

            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 5f;
            }

            // CanvasScaler recomputes in its Update; give it one frame at the new size.
            yield return null;
            Apply(camera, fitters, scalers);

            // In batch mode the first offscreen render was observed to bind sprite textures incorrectly;
            // a warm-up render avoids that.
            camera.Render();
            camera.Render();

            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = null;

            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, fileName), image.EncodeToPNG());
            Object.Destroy(image);

            camera.targetTexture = null;
            target.Release();
            Object.Destroy(target);
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            SafeAreaFitter.SimulatedSafeArea = null;
            yield return null;
            Apply(camera, fitters, scalers);
        }

        /// <summary>Phone 9:16, tall phone 19.5:9 with a simulated notch, and tablet 3:4.</summary>
        public static IEnumerator CaptureAspectRatios(Scene scene, string baseName)
        {
            yield return Capture(scene, baseName + "-9x16.png", 540, 960);
            yield return Capture(scene, baseName + "-19.5x9-notch.png", 540, 1170, simulateNotch: true);
            yield return Capture(scene, baseName + "-3x4.png", 720, 960);
        }

        private static void Apply(Camera camera, List<SafeAreaFitter> fitters, List<ResponsiveScaler> scalers)
        {
            if (camera.TryGetComponent<CameraFit>(out var fit))
            {
                fit.Apply();
            }

            foreach (var fitter in fitters)
            {
                fitter.Apply(true);
            }

            Canvas.ForceUpdateCanvases();
            foreach (var scaler in scalers)
            {
                scaler.Apply();
            }

            Canvas.ForceUpdateCanvases();
        }
    }
}
