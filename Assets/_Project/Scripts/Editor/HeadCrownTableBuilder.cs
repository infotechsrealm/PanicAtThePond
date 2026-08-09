using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PanicAtThePond.Data;

namespace PanicAtThePond.Editor
{
    /// <summary>
    /// Measures where the top of the head sits in every character animation frame and bakes the
    /// result into <see cref="HeadCrownTable"/>, so cosmetics can follow the head without a
    /// hand-maintained offset table.
    /// </summary>
    /// <remarks>
    /// Run this whenever character art changes. It reads the PNGs straight off disk, so the sprite
    /// importers do not need Read/Write enabled.
    /// </remarks>
    public static class HeadCrownTableBuilder
    {
        private const string AssetPath = "Assets/_Project/Resources/SO_HeadCrownTable.asset";

        private static readonly string[] CharacterPrefabs = { "Fisherman", "Fish", "Fish 2", "Golden Fish" };

        [MenuItem("Panic At The Pond/Rebuild Head Crown Table")]
        public static void Rebuild()
        {
            var entries = new List<HeadCrownTable.Entry>();
            var seen = new HashSet<string>();
            var textureCache = new Dictionary<string, Texture2D>();
            int skipped = 0;

            foreach (string prefabName in CharacterPrefabs)
            {
                GameObject prefab = Resources.Load<GameObject>(prefabName);
                if (prefab == null)
                {
                    continue;
                }

                Animator animator = prefab.GetComponent<Animator>();
                RuntimeAnimatorController controller = animator != null ? animator.runtimeAnimatorController : null;
                if (controller == null)
                {
                    continue;
                }

                foreach (AnimationClip clip in controller.animationClips)
                {
                    if (clip == null)
                    {
                        continue;
                    }

                    foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                    {
                        foreach (ObjectReferenceKeyframe key in AnimationUtility.GetObjectReferenceCurve(clip, binding))
                        {
                            Sprite sprite = key.value as Sprite;
                            if (sprite == null || !seen.Add(sprite.name))
                            {
                                continue;
                            }

                            int crown = MeasureCrownRow(sprite, textureCache);
                            if (crown < 0)
                            {
                                skipped++;
                                continue;
                            }

                            entries.Add(new HeadCrownTable.Entry { spriteName = sprite.name, crownRow = crown });
                        }
                    }
                }
            }

            foreach (Texture2D tex in textureCache.Values)
            {
                Object.DestroyImmediate(tex);
            }

            HeadCrownTable table = AssetDatabase.LoadAssetAtPath<HeadCrownTable>(AssetPath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<HeadCrownTable>();
                Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
                AssetDatabase.CreateAsset(table, AssetPath);
            }

            table.SetEntries(entries);
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            HeadCrownTable.ClearCache();

            Debug.Log($"[HeadCrownTableBuilder] Baked {entries.Count} sprite crowns to {AssetPath}"
                + (skipped > 0 ? $" ({skipped} skipped — no head-sized opaque run found)" : string.Empty));
        }

        /// <summary>
        /// First row from the top of the sprite whose widest continuous opaque run is at least
        /// <see cref="HeadCrownTable.MinHeadRunPixels"/> wide.
        /// </summary>
        /// <remarks>
        /// The plain topmost-opaque-pixel is not usable: the fishing rod and line reach higher than
        /// the head in the casting frames, which would report the rod tip as the head and swing the
        /// hat wildly. Requiring a run of several pixels skips those thin structures.
        /// </remarks>
        private static int MeasureCrownRow(Sprite sprite, Dictionary<string, Texture2D> cache)
        {
            string path = AssetDatabase.GetAssetPath(sprite.texture);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return -1;
            }

            if (!cache.TryGetValue(path, out Texture2D texture))
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.LoadImage(File.ReadAllBytes(path));
                cache[path] = texture;
            }

            Color32[] pixels = texture.GetPixels32();
            int rectX = (int)sprite.rect.x;
            int rectY = (int)sprite.rect.y;
            int rectW = (int)sprite.rect.width;
            int rectH = (int)sprite.rect.height;

            for (int localY = 0; localY < rectH; localY++)
            {
                int textureY = rectY + rectH - 1 - localY;   // sprite rows are bottom-up
                int run = 0;
                int longest = 0;

                for (int localX = 0; localX < rectW; localX++)
                {
                    if (pixels[textureY * texture.width + (rectX + localX)].a > 10)
                    {
                        run++;
                        if (run > longest)
                        {
                            longest = run;
                        }
                    }
                    else
                    {
                        run = 0;
                    }
                }

                if (longest >= HeadCrownTable.MinHeadRunPixels)
                {
                    return localY;
                }
            }

            return -1;
        }
    }
}
