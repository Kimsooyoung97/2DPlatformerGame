#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NAN2026.Showroom.Editor
{
    /// <summary>
    /// Recovers usable sprite strips from a screenshot of an effect sheet.
    ///
    /// The source has the editor checkerboard baked in as pixels, no alpha channel, row
    /// labels down the left, and compression noise from being scaled. This finds the frame
    /// grid from the coloured artwork itself, drops every grey (checkerboard/label) pixel,
    /// and downsamples 4x - which both restores some crispness and averages the noise out.
    /// </summary>
    public static class SlashSheetExtractor
    {
        private const string SourcePath = "Assets/Art/FX/투사체 이펙트.png";
        private const string OutputFolder = "Assets/Art/FX";
        private const int Downsample = 4;
        private const int SaturationThreshold = 25;

        private static readonly string[] RowNames = { "BASIC", "POWERED", "IMPACT", "SPINNING" };

        [MenuItem("Tools/Biome Showroom/Extract Slash Sheet")]
        public static void Extract()
        {
            byte[] raw = File.ReadAllBytes(SourcePath);
            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!source.LoadImage(raw))
            {
                Debug.LogError("Slash extractor: could not read " + SourcePath);
                return;
            }

            int w = source.width;
            int h = source.height;
            Color32[] pixels = source.GetPixels32();

            bool[] art = new bool[w * h];
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 c = pixels[i];
                int max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                int min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                art[i] = (max - min) > SaturationThreshold;
            }

            List<Vector2Int> columnBands = Bands(art, w, h, true);
            List<Vector2Int> rowBands = Bands(art, w, h, false);

            if (columnBands.Count < 6 || rowBands.Count < 1)
            {
                Debug.LogError("Slash extractor: expected 6 frame columns, found " +
                               columnBands.Count + " columns / " + rowBands.Count + " rows.");
                Object.DestroyImmediate(source);
                return;
            }

            // Uniform column pitch taken from the gaps between frame bands.
            float pitch = 0f;
            for (int i = 1; i < columnBands.Count; i++)
                pitch += (columnBands[i].x + columnBands[i].y) * 0.5f -
                         (columnBands[i - 1].x + columnBands[i - 1].y) * 0.5f;
            pitch /= (columnBands.Count - 1);

            // Rows come out bottom-up from GetPixels32; flip so index 0 is the top row.
            rowBands.Reverse();

            int made = 0;
            for (int r = 0; r < rowBands.Count && r < RowNames.Length; r++)
            {
                if (!ExtractRow(pixels, art, w, h, columnBands, pitch, rowBands[r], RowNames[r]))
                    continue;
                made++;
            }

            Object.DestroyImmediate(source);
            AssetDatabase.Refresh();
            Debug.Log("Slash extractor: wrote " + made + " strips to " + OutputFolder);
        }

        private static bool ExtractRow(
            Color32[] pixels, bool[] art, int w, int h,
            List<Vector2Int> columnBands, float pitch, Vector2Int rowBand, string name)
        {
            int pad = 6;
            int y0 = Mathf.Max(0, rowBand.x - pad);
            int y1 = Mathf.Min(h - 1, rowBand.y + pad);

            int cellW = Mathf.RoundToInt(pitch);
            int cellH = y1 - y0 + 1;

            int outW = cellW / Downsample;
            int outH = cellH / Downsample;
            if (outW < 4 || outH < 4) return false;

            int frames = Mathf.Min(6, columnBands.Count);
            Color32[] outPixels = new Color32[outW * frames * outH];

            for (int f = 0; f < frames; f++)
            {
                // Centre a uniform cell on each detected frame band, so motion inside the
                // cell is preserved instead of every frame being re-centred.
                int centre = Mathf.RoundToInt((columnBands[f].x + columnBands[f].y) * 0.5f);
                int x0 = centre - cellW / 2;

                for (int oy = 0; oy < outH; oy++)
                {
                    for (int ox = 0; ox < outW; ox++)
                    {
                        int rSum = 0, gSum = 0, bSum = 0, count = 0;

                        for (int by = 0; by < Downsample; by++)
                        {
                            for (int bx = 0; bx < Downsample; bx++)
                            {
                                int sx = x0 + ox * Downsample + bx;
                                int sy = y0 + oy * Downsample + by;
                                if (sx < 0 || sx >= w || sy < 0 || sy >= h) continue;

                                int idx = sy * w + sx;
                                if (!art[idx]) continue;              // grey = checkerboard/label

                                Color32 c = pixels[idx];
                                rSum += c.r; gSum += c.g; bSum += c.b;
                                count++;
                            }
                        }

                        int outIndex = oy * (outW * frames) + f * outW + ox;
                        if (count * 3 >= Downsample * Downsample)      // at least ~1/3 covered
                        {
                            outPixels[outIndex] = new Color32(
                                (byte)(rSum / count), (byte)(gSum / count), (byte)(bSum / count), 255);
                        }
                        else
                        {
                            outPixels[outIndex] = new Color32(0, 0, 0, 0);
                        }
                    }
                }
            }

            Texture2D output = new Texture2D(outW * frames, outH, TextureFormat.RGBA32, false);
            output.SetPixels32(outPixels);
            output.Apply();

            string path = OutputFolder + "/Slash_" + name + ".png";
            File.WriteAllBytes(path, output.EncodeToPNG());
            Object.DestroyImmediate(output);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureSprites(path, outW, outH, frames);
            return true;
        }

        private static void ConfigureSprites(string path, int frameW, int frameH, int frames)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = 24f;

            string baseName = Path.GetFileNameWithoutExtension(path);
            List<SpriteMetaData> sheet = new List<SpriteMetaData>();
            for (int i = 0; i < frames; i++)
            {
                SpriteMetaData meta = new SpriteMetaData
                {
                    name = baseName + "_" + i,
                    rect = new Rect(i * frameW, 0, frameW, frameH),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                };
                sheet.Add(meta);
            }

#pragma warning disable 618
            importer.spritesheet = sheet.ToArray();
#pragma warning restore 618
            importer.SaveAndReimport();
        }

        /// <summary>Finds runs of columns (or rows) that contain any coloured artwork.</summary>
        private static List<Vector2Int> Bands(bool[] art, int w, int h, bool byColumn)
        {
            int outer = byColumn ? w : h;
            int inner = byColumn ? h : w;

            bool[] any = new bool[outer];
            for (int o = 0; o < outer; o++)
            {
                for (int i = 0; i < inner; i++)
                {
                    int idx = byColumn ? i * w + o : o * w + i;
                    if (!art[idx]) continue;
                    any[o] = true;
                    break;
                }
            }

            List<Vector2Int> bands = new List<Vector2Int>();
            int start = -1;
            for (int o = 0; o < outer; o++)
            {
                if (any[o] && start < 0) start = o;
                if (!any[o] && start >= 0)
                {
                    if (o - start > 8) bands.Add(new Vector2Int(start, o - 1));
                    start = -1;
                }
            }
            if (start >= 0 && outer - start > 8)
                bands.Add(new Vector2Int(start, outer - 1));

            return bands;
        }
    }
}
#endif
