using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

using PanicAtThePond.Shop;

namespace PanicAtThePond.Editor
{
    /// <summary>
    /// Measures the drawn (non-transparent) bounds of every fisherman preview sprite and bakes the
    /// result into <see cref="PreviewBoundsCatalog"/>.
    ///
    /// <para>PNGs are decoded straight off disk into a throwaway <see cref="Texture2D"/>, so no
    /// import settings are touched — turning on Read/Write for ~250 sprites would bloat memory for
    /// a one-off measurement.</para>
    /// </summary>
    public static class PreviewBoundsCatalogBuilder
    {
        private const string CatalogAssetPath = "Assets/_Project/Resources/SO_PreviewBoundsCatalog.asset";
        private const string PreviewFolder = "Assets/_Project/Resources/ShopUI";
        private const byte AlphaThreshold = 12;

        [MenuItem("Tools/Panic At The Pond/Rebuild Preview Bounds Catalog")]
        public static void Rebuild()
        {
            var entries = new List<PreviewBoundsEntry>();
            var report = new StringBuilder();

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { PreviewFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                if (!IsFishermanPreview(name))
                {
                    continue;
                }

                if (!TryMeasure(path, out float wFrac, out float hFrac, out int cw, out int ch, out int w, out int h))
                {
                    report.AppendLine($"  {name}: could not decode");
                    continue;
                }

                entries.Add(new PreviewBoundsEntry
                {
                    spriteName = name,
                    contentWidthFraction = wFrac,
                    contentHeightFraction = hFrac
                });

                report.AppendLine($"  {name,-28} png={w}x{h} drawn={cw}x{ch}  " +
                                  $"fractions={wFrac:F3},{hFrac:F3}");
            }

            var catalog = AssetDatabase.LoadAssetAtPath<PreviewBoundsCatalog>(CatalogAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PreviewBoundsCatalog>();
                Directory.CreateDirectory(Path.GetDirectoryName(CatalogAssetPath));
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            }

            catalog.SetEntries(entries);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            PreviewBoundsCatalog.ResetCache();

            Debug.Log($"PATP_PREVIEWBOUNDS measured={entries.Count}\n{report}");
        }

        private static bool IsFishermanPreview(string name)
        {
            string n = name.ToLowerInvariant();
            return n.StartsWith("fisherman ") || n.StartsWith("fishermna ") || n.StartsWith("fishaerman ");
        }

        private static bool TryMeasure(string path, out float wFrac, out float hFrac,
                                       out int contentW, out int contentH, out int width, out int height)
        {
            wFrac = 1f; hFrac = 1f; contentW = 0; contentH = 0; width = 0; height = 0;

            byte[] bytes;
            try { bytes = File.ReadAllBytes(path); }
            catch { return false; }

            var tex = new Texture2D(2, 2);
            try
            {
                if (!tex.LoadImage(bytes))
                {
                    return false;
                }

                width = tex.width;
                height = tex.height;
                Color32[] px = tex.GetPixels32();

                int minX = width, minY = height, maxX = -1, maxY = -1;
                for (int y = 0; y < height; y++)
                {
                    int row = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        if (px[row + x].a <= AlphaThreshold)
                        {
                            continue;
                        }

                        if (x < minX) { minX = x; }
                        if (x > maxX) { maxX = x; }
                        if (y < minY) { minY = y; }
                        if (y > maxY) { maxY = y; }
                    }
                }

                if (maxX < 0)
                {
                    return false;
                }

                contentW = maxX - minX + 1;
                contentH = maxY - minY + 1;
                wFrac = (float)contentW / width;
                hFrac = (float)contentH / height;
                return true;
            }
            finally
            {
                Object.DestroyImmediate(tex);
            }
        }
    }
}
