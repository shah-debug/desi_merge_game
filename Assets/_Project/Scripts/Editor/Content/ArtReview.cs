using System;
using System.IO;
using UnityEngine;

namespace DesiMerge.Editor.Content
{
    public static class ArtReview
    {
        private static readonly string[] ItemIds = { "patti", "nimbu", "pyaz", "tamatar", "roti", "aam", "chai", "mango_shake", "rickshaw", "bus" };

        public static void WriteItemSheet()
        {
            var directory = Environment.GetEnvironmentVariable("DESIMERGE_CAPTURE_DIR");
            if (string.IsNullOrEmpty(directory))
            {
                Debug.LogError("[ArtReview] Set DESIMERGE_CAPTURE_DIR.");
                return;
            }

            const int cell = 220;
            const int columns = 5;
            var rows = (ItemIds.Length + columns - 1) / columns;
            var sheet = new Texture2D(cell * columns, cell * rows * 2, TextureFormat.RGBA32, false);
            var cream = ArtCanvas.Hex("#FFF3DC");
            var navy = ArtCanvas.Hex("#1B1F4B");
            for (var i = 0; i < ItemIds.Length; i++)
            {
                var canvas = new ArtCanvas(cell, cell);
                ItemArtGenerator.Draw(ItemIds[i], canvas);
                var texture = canvas.ToTexture();
                for (var band = 0; band < 2; band++)
                {
                    var background = band == 0 ? cream : navy;
                    var ox = (i % columns) * cell;
                    var oy = (rows * 2 - 1 - (i / columns) - band * rows) * cell;
                    for (var y = 0; y < cell; y++)
                    {
                        for (var x = 0; x < cell; x++)
                        {
                            var guide = Mathf.Abs(new Vector2(x + 0.5f - cell * 0.5f, y + 0.5f - cell * 0.5f).magnitude - cell * 0.5f) < 0.6f;
                            var under = guide ? Color.Lerp(background, Color.red, 0.5f) : background;
                            sheet.SetPixel(ox + x, oy + y, ArtCanvas.Over(under, texture.GetPixel(x, y)));
                        }
                    }
                }

                UnityEngine.Object.DestroyImmediate(texture);
            }

            sheet.Apply();
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, "item-sheet.png"), sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
            Debug.Log("[ArtReview] Wrote item-sheet.png");
        }
    }
}
