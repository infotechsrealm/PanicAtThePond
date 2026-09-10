using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PanicAtThePond.Data;

namespace PanicAtThePond.Editor
{
    /// <summary>
    /// Adds a <c>HeadAnchor</c> child to each character prefab and keys its vertical position inside
    /// every AnimationClip that character plays, so cosmetics can simply be parented to it.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this replaces the runtime placement code.</b> <c>CosmeticRuntimeApplier</c> used to
    /// reconstruct the head position every <c>LateUpdate</c>: read the body sprite's name, parse a
    /// frame index out of it, look up an offset. That is a guess made one frame after the fact, and it
    /// breaks during transition blends, when two clips share a sprite, and when a name does not parse.
    /// It also only ever solved the vertical axis, and needed new hand data for every new cosmetic.</para>
    ///
    /// <para>Keying the anchor inside the clip removes the whole class of problem: the anchor and the
    /// sprite sit on <b>one timeline</b> and are sampled by <b>the same evaluator in the same
    /// frame</b>. There is no code between them, so desync is structurally impossible rather than
    /// merely unlikely.</para>
    ///
    /// <para><b>The curve is stepped, not interpolated.</b> Sprite swaps are object-reference curves,
    /// which are inherently stepped — the sprite changes instantly at the keyframe. If the anchor's
    /// Y interpolated smoothly between those keys the hat would glide while the head jumped, which is
    /// the drift being fixed. Constant tangents keep the two in lockstep.</para>
    ///
    /// <para>This seeds the curves from measured art and is safe to re-run: it overwrites the
    /// <c>HeadAnchor</c> curve and leaves every other curve in the clip untouched. Hand-corrections
    /// made in the Animation window to <i>other</i> properties survive, but hand-corrections to the
    /// anchor's Y do not — re-run this only when the character art changes.</para>
    /// </remarks>
    public static class HeadAnchorBuilder
    {
        /// <summary>Name of the child transform cosmetics parent themselves to.</summary>
        public const string AnchorName = "HeadAnchor";

        private static readonly string[] CharacterPrefabs = { "Fisherman", "Fish", "Fish 2", "Golden Fish" };

        [MenuItem("Panic At The Pond/Rebuild Head Anchors")]
        public static void Rebuild() => Rebuild(null);

        /// <summary>
        /// Rebuilds the anchors. Pass a subset of <see cref="CharacterPrefabs"/> to re-bake only
        /// those characters; pass null for all of them. Re-baking a character whose anchors are
        /// already correct is a needless risk, so a caller migrating one species passes just that one.
        /// </summary>
        public static void Rebuild(string[] only)
        {
            var textureCache = new Dictionary<string, Texture2D>();
            int prefabsTouched = 0;
            int clipsKeyed = 0;
            int keysWritten = 0;
            var skipped = new List<string>();

            try
            {
                foreach (string prefabName in CharacterPrefabs)
                {
                    if (only != null && System.Array.IndexOf(only, prefabName) < 0)
                    {
                        continue;
                    }

                    GameObject prefab = Resources.Load<GameObject>(prefabName);
                    if (prefab == null)
                    {
                        continue;
                    }

                    string assetPath = AssetDatabase.GetAssetPath(prefab);
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        continue;
                    }

                    if (EnsureAnchor(assetPath, textureCache))
                    {
                        prefabsTouched++;
                    }

                    Animator animator = prefab.GetComponent<Animator>();
                    RuntimeAnimatorController controller = animator != null ? animator.runtimeAnimatorController : null;
                    if (controller == null)
                    {
                        continue;
                    }

                    var done = new HashSet<AnimationClip>();
                    foreach (AnimationClip clip in controller.animationClips)
                    {
                        if (clip == null || !done.Add(clip))
                        {
                            continue;
                        }

                        int written = KeyAnchorInto(clip, textureCache, skipped);
                        if (written > 0)
                        {
                            clipsKeyed++;
                            keysWritten += written;
                        }
                    }
                }
            }
            finally
            {
                foreach (Texture2D texture in textureCache.Values)
                {
                    Object.DestroyImmediate(texture);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[HeadAnchorBuilder] {AnchorName} present on {prefabsTouched} prefab(s); "
                + $"keyed {keysWritten} frame(s) across {clipsKeyed} clip(s)."
                + (skipped.Count > 0
                    ? $"\nSkipped {skipped.Count} frame(s) with no measurable head:\n  " + string.Join("\n  ", skipped)
                    : string.Empty));
        }

        /// <summary>
        /// Adds the anchor child to the prefab and parks it at the head height of the prefab's own
        /// rest sprite.
        /// </summary>
        /// <remarks>
        /// <para><b>The rest value is not cosmetic — it is what makes placement correct.</b> A hat's
        /// sit offset is worked out at apply time as <c>authoredPosition − anchor.localPosition</c>,
        /// and cosmetics are routinely applied before the Animator has evaluated anything. With the
        /// anchor left at zero that subtraction is a no-op, so the hat keeps its root-relative
        /// position as if it were anchor-relative and then jumps by the full crown height the moment
        /// a clip starts playing — roughly 0.8 units on the fisherman, a hat floating clear above the
        /// head.</para>
        ///
        /// <para>Seeding the prefab with the real rest height makes the subtraction meaningful
        /// whether or not the animation has been sampled yet.</para>
        /// </remarks>
        private static bool EnsureAnchor(string assetPath, Dictionary<string, Texture2D> textureCache)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(assetPath);
            try
            {
                Transform anchor = root.transform.Find(AnchorName);
                if (anchor == null)
                {
                    var created = new GameObject(AnchorName);
                    created.transform.SetParent(root.transform, false);
                    anchor = created.transform;
                }

                anchor.localRotation = Quaternion.identity;
                anchor.localScale = Vector3.one;

                float restY = 0f;
                SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null)
                {
                    int crownRow = MeasureCrownRow(renderer.sprite, textureCache, out _);
                    if (crownRow >= 0)
                    {
                        restY = CrownLocalY(renderer.sprite, crownRow);
                    }
                }

                anchor.localPosition = new Vector3(0f, restY, 0f);

                PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Writes one stepped <c>m_LocalPosition.y</c> key per sprite keyframe, placing the anchor at
        /// the top of the head in that frame. Returns the number of keys written.
        /// </summary>
        private static int KeyAnchorInto(AnimationClip clip, Dictionary<string, Texture2D> textureCache, List<string> skipped)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            if (bindings.Length == 0)
            {
                return 0;
            }

            // Take the head from ONE renderer. A layered character drives several SpriteRenderers
            // from the same clip, and folding all of them into a single curve yields one key per
            // layer per frame -- many keys sharing a timestamp, which Unity rejects outright.
            // Prefer the layer that actually draws the head; fall back to the root renderer.
            EditorCurveBinding headBinding = bindings[0];
            bool found = false;
            foreach (EditorCurveBinding candidate in bindings)
            {
                if (candidate.path.ToLowerInvariant().Contains("head"))
                {
                    headBinding = candidate;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                foreach (EditorCurveBinding candidate in bindings)
                {
                    if (string.IsNullOrEmpty(candidate.path))
                    {
                        headBinding = candidate;
                        break;
                    }
                }
            }

            var yKeys = new List<Keyframe>();
            var xKeys = new List<Keyframe>();

            foreach (ObjectReferenceKeyframe key in AnimationUtility.GetObjectReferenceCurve(clip, headBinding))
            {
                if (!(key.value is Sprite sprite))
                {
                    continue;
                }

                int crownCentreColumn;
                int crownRow = MeasureCrownRow(sprite, textureCache, out crownCentreColumn);
                if (crownRow < 0)
                {
                    skipped.Add($"{clip.name}/{sprite.name}");
                    continue;
                }

                // Constant tangents: the anchor must snap with the sprite, not glide between.
                yKeys.Add(new Keyframe(key.time, CrownLocalY(sprite, crownRow))
                {
                    inTangent = float.PositiveInfinity,
                    outTangent = float.PositiveInfinity
                });
                xKeys.Add(new Keyframe(key.time, CrownLocalX(sprite, crownCentreColumn))
                {
                    inTangent = float.PositiveInfinity,
                    outTangent = float.PositiveInfinity
                });
            }

            if (yKeys.Count == 0)
            {
                return 0;
            }

            yKeys.Sort((a, b) => a.time.CompareTo(b.time));
            xKeys.Sort((a, b) => a.time.CompareTo(b.time));

            // The head travels horizontally too (the fisherman swapping pole sides, a fish bending
            // to fight). Keying Y alone leaves a hat right in height and wrong in X on those frames.
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(AnchorName, typeof(Transform), "m_LocalPosition.y"),
                new AnimationCurve(yKeys.ToArray()));
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(AnchorName, typeof(Transform), "m_LocalPosition.x"),
                new AnimationCurve(xKeys.ToArray()));
            EditorUtility.SetDirty(clip);
            return yKeys.Count;
        }

        /// <summary>
        /// Local X of the centre of the head for this sprite, in the character's own units.
        /// </summary>
        private static float CrownLocalX(Sprite sprite, int crownCentreColumn)
        {
            return (crownCentreColumn - sprite.pivot.x) / sprite.pixelsPerUnit;
        }

        /// <summary>
        /// Local Y of the top of the head for this sprite, in the character's own units.
        /// </summary>
        /// <remarks>
        /// <paramref name="crownRow"/> counts rows down from the top of the sprite; the pivot is the
        /// transform's origin and is given in pixels up from the bottom of the sprite rect. The
        /// absolute value only has to be consistent between frames — a constant bias is absorbed by
        /// each hat's one-time sit offset, whereas the frame-to-frame delta is what has to be exact.
        /// </remarks>
        private static float CrownLocalY(Sprite sprite, int crownRow)
        {
            float unitsPerPixel = 1f / sprite.pixelsPerUnit;
            float crownFromBottomPixels = sprite.rect.height - crownRow;
            return (crownFromBottomPixels - sprite.pivot.y) * unitsPerPixel;
        }

        /// <summary>
        /// First row from the top whose widest continuous opaque run is at least
        /// <see cref="HeadCrownTable.MinHeadRunPixels"/> wide, read from the PNG on disk.
        /// </summary>
        /// <remarks>
        /// A plain topmost-opaque-pixel scan does not find the head: the fishing rod and line reach
        /// higher in the casting frames, and would drag the anchor up with them. Reading file bytes
        /// rather than <c>sprite.texture</c> avoids needing Read/Write enabled on 100+ textures.
        /// </remarks>
        private static int MeasureCrownRow(Sprite sprite, Dictionary<string, Texture2D> cache, out int crownCentreColumn)
        {
            crownCentreColumn = 0;
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
                int longestEnd = 0;

                for (int localX = 0; localX < rectW; localX++)
                {
                    if (pixels[textureY * texture.width + (rectX + localX)].a > 10)
                    {
                        run++;
                        if (run > longest)
                        {
                            longest = run;
                            longestEnd = localX;
                        }
                    }
                    else
                    {
                        run = 0;
                    }
                }

                if (longest >= HeadCrownTable.MinHeadRunPixels)
                {
                    crownCentreColumn = longestEnd - (longest / 2);
                    return localY;
                }
            }

            return -1;
        }
    }
}
