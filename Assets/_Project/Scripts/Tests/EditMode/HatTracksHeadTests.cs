using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using PanicAtThePond.Data;
using PanicAtThePond.Shop;

namespace PanicAtThePond.Tests.EditMode
{
    /// <summary>
    /// Exhaustive check that a cosmetic hat sits on the head in <b>every frame of every animation</b>,
    /// for <b>every hat in the shop</b>.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this measures the art instead of reading the crown table.</b> The previous version
    /// of this fixture compared the hat's movement against <see cref="HeadCrownTable"/> — but the
    /// runtime positions the hat <i>from that same table</i>
    /// (<c>bob = (baseCrownRow - currentCrown) * unitsPerPixel</c>). Substituting one into the other
    /// cancels <c>baseCrownRow</c> and leaves <c>headMoved ≡ hatMoved</c>: the assertion was
    /// <c>A == A</c> and reported "0 px worst mismatch" no matter what the table contained. A table
    /// baked from the wrong artwork — exactly the bug that shipped — would still have passed.</para>
    ///
    /// <para>So the reference here is re-measured from the PNGs on disk, independently of the baked
    /// asset. The scan is intentionally a second implementation of the same rule rather than a call
    /// into <c>HeadCrownTableBuilder</c>: what needs to be independent is the <i>data</i>, so that a
    /// stale or mis-baked table fails the test instead of defining it.</para>
    ///
    /// <para><b>Why the tolerance is sub-pixel.</b> The old assertion rounded the error to whole
    /// pixels, which silently accepted anything under half a pixel. The hardcoded per-hat branches in
    /// <c>CosmeticRuntimeApplier</c> move the hat 0.035 units where the head moves 0.04 (25 PPU) —
    /// a 12.5% error that rounding absorbed completely.</para>
    /// </remarks>
    [TestFixture]
    public sealed class HatTracksHeadTests
    {
        private const int StepsPerClip = 60;
        private const float StepSeconds = 0.02f;

        /// <summary>
        /// Allowed head-to-hat divergence, in source pixels. The correct value is exactly zero; this
        /// only absorbs float noise. Do NOT widen it to make a hat pass — that is what hid the last
        /// two bugs.
        /// </summary>
        private const float TolerancePixels = 0.02f;

        /// <summary>Child transform the animation clips key and cosmetics parent themselves to.</summary>
        private const string HeadAnchorName = "HeadAnchor";

        private static readonly string[] FishermanHats =
        {
            "FisherMan_Hat_-Blue_Cap",
            "FisherMan_Hat_-Red_Cap",
            "FisherMan_Hat_-Chef_Hat",
            "FisherMan_Hat_-Ranger_Hat",
            "FisherMan_Hat_-Soda_Hat",
            "FisherMan_Hat_-Fish_Hat",
            "TurtleHat",
            "FisherMan_Hat_-Default_-_Fishing_Hat"
        };

        private static readonly string[] FishHats =
        {
            "paper_boat", "cap", "hat", "hat2", "beret"
        };

        private GameObject _subject;
        private readonly Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();

        [TearDown]
        public void TearDown()
        {
            if (_subject != null)
            {
                Object.DestroyImmediate(_subject);
                _subject = null;
            }

            foreach (Texture2D texture in _textureCache.Values)
            {
                if (texture != null)
                {
                    Object.DestroyImmediate(texture);
                }
            }
            _textureCache.Clear();
        }

        // ------------------------------------------------------------------ bake freshness

        [Test]
        public void HeadCrownTable_IsBaked()
        {
            HeadCrownTable table = HeadCrownTable.Active;
            Assert.That(table, Is.Not.Null,
                "SO_HeadCrownTable is missing. Run 'Panic At The Pond > Rebuild Head Crown Table'.");
            Assert.That(table.Count, Is.GreaterThan(100),
                "The crown table looks incomplete — regenerate it after any character art change.");
        }

        /// <summary>
        /// The baked table must still agree with the art on disk. This is the check that the old
        /// fixture structurally could not perform, and the one that would have caught the table being
        /// measured from <c>FishermansAnimations-Head_Sheet.png</c> instead of the composited frames.
        /// </summary>
        [TestCase("Fisherman")]
        [TestCase("Fish")]
        public void HeadCrownTable_MatchesSourceArt(string prefabName)
        {
            HeadCrownTable table = HeadCrownTable.Active;
            Assert.That(table, Is.Not.Null, "SO_HeadCrownTable is missing.");

            var mismatches = new List<string>();
            int compared = 0;

            foreach (Sprite sprite in EnumerateBodySprites(prefabName))
            {
                int baked = table.GetCrownRow(sprite.name);
                int measured = MeasureCrownRow(sprite);

                if (measured < 0)
                {
                    continue;
                }

                compared++;
                if (baked != measured)
                {
                    mismatches.Add($"{sprite.name}: table says row {baked}, art says row {measured}");
                }
            }

            Assert.That(compared, Is.GreaterThan(0), $"No {prefabName} sprites could be measured.");
            Assert.That(mismatches, Is.Empty,
                $"{mismatches.Count} of {compared} {prefabName} sprites disagree with the baked table. "
                + "Run 'Panic At The Pond > Rebuild Head Crown Table'.\n  "
                + string.Join("\n  ", mismatches));
        }

        // ------------------------------------------------------------------ the anchor itself

        /// <summary>
        /// The <c>HeadAnchor</c> keyed into each clip must move exactly as the head does, sampled at
        /// arbitrary times rather than only on keyframes.
        /// </summary>
        /// <remarks>
        /// This validates the seeded curves independently of the cosmetic system, and catches the
        /// failure modes a spot-check would not: curve written on the wrong path, keyframe times not
        /// aligned to the sprite swaps, and — the important one — smooth tangents. Sprite curves are
        /// stepped, so an interpolating anchor would be caught drifting between two keys here even
        /// though it matched perfectly on every keyframe.
        /// </remarks>
        [TestCase("Fisherman")]
        [TestCase("Fish")]
        public void HeadAnchor_TracksHead_InEveryFrameOfEveryAnimation(string prefabName)
        {
            GameObject prefab = Resources.Load<GameObject>(prefabName);
            Assert.That(prefab, Is.Not.Null, $"Resources/{prefabName} is missing.");

            _subject = Object.Instantiate(prefab);
            foreach (MonoBehaviour behaviour in _subject.GetComponentsInChildren<MonoBehaviour>(true))
            {
                behaviour.enabled = false;
            }

            Transform anchor = _subject.transform.Find(HeadAnchorName);
            Assert.That(anchor, Is.Not.Null,
                $"'{HeadAnchorName}' is missing from {prefabName}. "
                + "Run 'Panic At The Pond > Rebuild Head Anchors'.");

            var animator = _subject.GetComponent<Animator>();
            var body = _subject.GetComponent<SpriteRenderer>();
            float unitsPerPixel = 1f / body.sprite.pixelsPerUnit;

            var failures = new List<string>();
            int framesChecked = 0;
            float worstError = 0f;

            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip == null)
                {
                    continue;
                }

                animator.Play(clip.name, 0, 0f);
                animator.Update(0f);

                int referenceCrown = MeasureCrownRow(body.sprite);
                if (referenceCrown < 0)
                {
                    continue;
                }

                float referenceAnchorY = anchor.localPosition.y;
                var seen = new HashSet<string>();

                for (int step = 0; step < StepsPerClip; step++)
                {
                    animator.Update(StepSeconds);

                    if (body.sprite == null || !seen.Add(body.sprite.name))
                    {
                        continue;
                    }

                    int crown = MeasureCrownRow(body.sprite);
                    if (crown < 0)
                    {
                        continue;
                    }

                    float headMovedPixels = referenceCrown - crown;
                    float anchorMovedPixels = (anchor.localPosition.y - referenceAnchorY) / unitsPerPixel;
                    float errorPixels = Mathf.Abs(headMovedPixels - anchorMovedPixels);

                    framesChecked++;
                    worstError = Mathf.Max(worstError, errorPixels);

                    if (errorPixels > TolerancePixels)
                    {
                        failures.Add($"{clip.name}/{body.sprite.name}: head moved "
                            + $"{headMovedPixels:+0.00;-0.00;0.00} px but anchor moved "
                            + $"{anchorMovedPixels:+0.00;-0.00;0.00} px ({errorPixels:0.00} px out)");
                    }
                }
            }

            Assert.That(framesChecked, Is.GreaterThan(0), "No frames were checked — the audit did nothing.");
            Assert.That(failures, Is.Empty,
                $"{prefabName}: {failures.Count} of {framesChecked} frames have the anchor off the head "
                + $"(worst {worstError:0.00} px):\n  " + string.Join("\n  ", failures));
        }

        // ------------------------------------------------------------------ coverage guard

        /// <summary>
        /// Stops a newly shipped hat from quietly skipping the audit: every hat in
        /// <c>shop_config.json</c> must appear in one of the lists above.
        /// </summary>
        [Test]
        public void EveryShopHat_IsCovered()
        {
            ShopConfig config = ShopConfig.Load();
            Assert.That(config, Is.Not.Null, "shop_config.json did not load.");
            Assert.That(config.hats, Is.Not.Null.And.Not.Empty, "shop_config.json lists no hats.");

            var covered = new HashSet<string>(FishermanHats);
            covered.UnionWith(FishHats);

            var missing = new List<string>();
            foreach (ShopConfig.HatEntry entry in config.hats)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.id) && !covered.Contains(entry.id))
                {
                    missing.Add($"{entry.id} ({entry.category})");
                }
            }

            Assert.That(missing, Is.Empty,
                "These shop hats are not in the audit lists, so nothing checks that they track the "
                + "head:\n  " + string.Join("\n  ", missing));
        }

        // ------------------------------------------------------------------ the audit

        [Test]
        public void Hat_TracksHead_InEveryFrameOfEveryAnimation(
            [ValueSource(nameof(AllHatCases))] HatCase testCase)
        {
            string childName = testCase.IsFisherman
                ? "Applied Fisherman Hat Cosmetic"
                : "Applied Fish Hat Cosmetic";

            GameObject prefab = Resources.Load<GameObject>(testCase.PrefabName);
            Assert.That(prefab, Is.Not.Null, $"Resources/{testCase.PrefabName} is missing.");

            _subject = Object.Instantiate(prefab);

            Animator sourceAnimator = _subject.GetComponent<Animator>();
            string defaultController = sourceAnimator != null && sourceAnimator.runtimeAnimatorController != null
                ? sourceAnimator.runtimeAnimatorController.name
                : "<none>";

            foreach (MonoBehaviour behaviour in _subject.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!(behaviour is CosmeticRuntimeApplier))
                {
                    behaviour.enabled = false;
                }
            }

            if (testCase.IsFisherman)
            {
                CosmeticRuntimeApplier.ApplyFishermanCosmeticsByName(_subject, testCase.HatId, null);
            }
            else
            {
                CosmeticRuntimeApplier.ApplyFishHatByName(_subject, testCase.HatId);
            }

            var animator = _subject.GetComponent<Animator>();

            // The hat hangs off HeadAnchor once the character has been rebuilt, and off the root
            // before that, so search the whole hierarchy rather than assuming either.
            Transform hat = null;
            foreach (Transform candidate in _subject.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == childName)
                {
                    hat = candidate;
                    break;
                }
            }

            if (hat == null)
            {
                // A pre-baked hat is painted into the body frames themselves and correctly has no
                // child object — it tracks the head by construction. Only the default fishing hat
                // ships this way (ANIM_FisherManYellowHat is the sole baked hat controller in
                // Resources/FishermanControllers); every other hat flagged pre-baked falls back to a
                // child sprite because its controller asset is missing. Assert the swap actually
                // happened so a hat that simply failed to apply still fails the test.
                string controllerName = animator != null && animator.runtimeAnimatorController != null
                    ? animator.runtimeAnimatorController.name
                    : "<none>";

                Assert.That(controllerName, Is.Not.EqualTo(defaultController),
                    $"'{childName}' was not created and the animator controller is still "
                    + $"'{controllerName}' — the hat '{testCase.HatId}' never applied at all.");

                Assert.Pass($"'{testCase.HatId}' is pre-baked into controller '{controllerName}'. "
                    + "The hat is part of the body artwork, so head tracking is structural.");
            }

            var applier = hat.GetComponent<CosmeticRuntimeApplier>();
            Assert.That(applier, Is.Not.Null, "The applied hat has no CosmeticRuntimeApplier.");

            var body = _subject.GetComponent<SpriteRenderer>();
            MethodInfo lateUpdate = typeof(CosmeticRuntimeApplier)
                .GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lateUpdate, Is.Not.Null, "CosmeticRuntimeApplier.LateUpdate was renamed.");

            float unitsPerPixel = 1f / body.sprite.pixelsPerUnit;
            var failures = new List<string>();
            var unmeasured = new List<string>();
            int framesChecked = 0;
            float worstError = 0f;

            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip == null)
                {
                    continue;
                }

                animator.Play(clip.name, 0, 0f);
                animator.Update(0f);
                lateUpdate.Invoke(applier, null);

                int referenceCrown = MeasureCrownRow(body.sprite);
                if (referenceCrown < 0)
                {
                    unmeasured.Add($"{clip.name}: reference frame '{SpriteName(body.sprite)}' has no head-sized run");
                    continue;
                }

                // Measured in the character's own space, not the hat's parent's. Once the hat is
                // parented to the animated anchor its localPosition is a constant sit offset, so
                // reading that would report "never moves" instead of "moves with the head". This is
                // what makes the audit indifferent to *how* the hat gets positioned.
                float referenceHatY = HatYInCharacterSpace(hat);
                var seen = new HashSet<string>();

                for (int step = 0; step < StepsPerClip; step++)
                {
                    animator.Update(StepSeconds);
                    lateUpdate.Invoke(applier, null);

                    if (body.sprite == null || !seen.Add(body.sprite.name))
                    {
                        continue;
                    }

                    int crown = MeasureCrownRow(body.sprite);
                    if (crown < 0)
                    {
                        unmeasured.Add($"{clip.name}/{SpriteName(body.sprite)}: no head-sized run");
                        continue;
                    }

                    // Crown rows count DOWN from the top, so a smaller row means the head moved UP.
                    float headMovedPixels = referenceCrown - crown;
                    float hatMovedPixels = (HatYInCharacterSpace(hat) - referenceHatY) / unitsPerPixel;
                    float errorPixels = Mathf.Abs(headMovedPixels - hatMovedPixels);

                    framesChecked++;
                    if (errorPixels > worstError)
                    {
                        worstError = errorPixels;
                    }

                    if (errorPixels > TolerancePixels)
                    {
                        failures.Add($"{clip.name}/{body.sprite.name}: head moved "
                            + $"{headMovedPixels:+0.00;-0.00;0.00} px but hat moved "
                            + $"{hatMovedPixels:+0.00;-0.00;0.00} px ({errorPixels:0.00} px out)");
                    }
                }
            }

            Assert.That(framesChecked, Is.GreaterThan(0),
                "No frames were checked — the audit did nothing.\n  "
                + string.Join("\n  ", unmeasured));

            Assert.That(failures, Is.Empty,
                $"{testCase.PrefabName} + {testCase.HatId}: {failures.Count} of {framesChecked} frames "
                + $"have the hat out of sync (worst {worstError:0.00} px):\n  "
                + string.Join("\n  ", failures));
        }

        /// <summary>
        /// The hat must actually sit <b>on</b> the head, not merely move with it.
        /// </summary>
        /// <remarks>
        /// <para>The tracking audit above compares frame-to-frame movement, so a constant offset
        /// error is invisible to it — every frame is wrong by the same amount and the deltas still
        /// match perfectly. That is not hypothetical: parenting hats to <c>HeadAnchor</c> shipped with
        /// the sit offset computed against an unevaluated anchor sitting at zero, which left every
        /// fisherman hat floating a full 0.80 units (20 px) above the head while this fixture
        /// reported 0 px on all 102 frames.</para>
        ///
        /// <para>The bound is deliberately loose — hats legitimately sit anywhere from just below the
        /// crown (a cap's brim overlaps the forehead) to a little above it (a soda hat perches). It is
        /// there to catch a placement that has come adrift by a whole head, not to police art.</para>
        /// </remarks>
        [Test]
        public void Hat_SitsOnTheHead_NotMerelyParallelToIt(
            [ValueSource(nameof(AllHatCases))] HatCase testCase)
        {
            const float MaxOffsetPixels = 16f;

            string childName = testCase.IsFisherman
                ? "Applied Fisherman Hat Cosmetic"
                : "Applied Fish Hat Cosmetic";

            GameObject prefab = Resources.Load<GameObject>(testCase.PrefabName);
            _subject = Object.Instantiate(prefab);
            foreach (MonoBehaviour behaviour in _subject.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!(behaviour is CosmeticRuntimeApplier))
                {
                    behaviour.enabled = false;
                }
            }

            if (testCase.IsFisherman)
            {
                CosmeticRuntimeApplier.ApplyFishermanCosmeticsByName(_subject, testCase.HatId, null);
            }
            else
            {
                CosmeticRuntimeApplier.ApplyFishHatByName(_subject, testCase.HatId);
            }

            Transform hat = null;
            foreach (Transform candidate in _subject.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == childName)
                {
                    hat = candidate;
                    break;
                }
            }

            if (hat == null)
            {
                Assert.Pass($"'{testCase.HatId}' is pre-baked into the body artwork.");
            }

            var animator = _subject.GetComponent<Animator>();
            var body = _subject.GetComponent<SpriteRenderer>();
            MethodInfo lateUpdate = typeof(CosmeticRuntimeApplier)
                .GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            var applier = hat.GetComponent<CosmeticRuntimeApplier>();

            float unitsPerPixel = 1f / body.sprite.pixelsPerUnit;
            var failures = new List<string>();
            int framesChecked = 0;

            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip == null)
                {
                    continue;
                }

                animator.Play(clip.name, 0, 0f);
                animator.Update(0f);
                lateUpdate.Invoke(applier, null);

                int crown = MeasureCrownRow(body.sprite);
                if (crown < 0)
                {
                    continue;
                }

                float crownY = CrownLocalY(body.sprite, crown);
                float offsetPixels = (HatYInCharacterSpace(hat) - crownY) / unitsPerPixel;

                framesChecked++;
                if (Mathf.Abs(offsetPixels) > MaxOffsetPixels)
                {
                    failures.Add($"{clip.name}/{body.sprite.name}: hat sits {offsetPixels:+0.0;-0.0;0.0} px "
                        + "from the top of the head");
                }
            }

            Assert.That(framesChecked, Is.GreaterThan(0), "No frames were checked.");
            Assert.That(failures, Is.Empty,
                $"{testCase.PrefabName} + {testCase.HatId}: the hat is not on the head "
                + $"({failures.Count} of {framesChecked} frames):\n  " + string.Join("\n  ", failures));
        }

        // ------------------------------------------------------------------ case source

        public struct HatCase
        {
            public string PrefabName;
            public string HatId;
            public bool IsFisherman;

            public override string ToString() => $"{PrefabName}+{HatId}";
        }

        public static IEnumerable<HatCase> AllHatCases()
        {
            foreach (string id in FishermanHats)
            {
                yield return new HatCase { PrefabName = "Fisherman", HatId = id, IsFisherman = true };
            }

            foreach (string id in FishHats)
            {
                yield return new HatCase { PrefabName = "Fish", HatId = id, IsFisherman = false };
            }
        }

        // ------------------------------------------------------------------ measurement

        /// <summary>Every distinct sprite keyed into any clip on the given character prefab.</summary>
        private static IEnumerable<Sprite> EnumerateBodySprites(string prefabName)
        {
            GameObject prefab = Resources.Load<GameObject>(prefabName);
            if (prefab == null)
            {
                yield break;
            }

            Animator animator = prefab.GetComponent<Animator>();
            RuntimeAnimatorController controller = animator != null ? animator.runtimeAnimatorController : null;
            if (controller == null)
            {
                yield break;
            }

            var seen = new HashSet<string>();
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
                        if (key.value is Sprite sprite && seen.Add(sprite.name))
                        {
                            yield return sprite;
                        }
                    }
                }
            }
        }

        private static string SpriteName(Sprite sprite) => sprite != null ? sprite.name : "<null>";

        /// <summary>
        /// Local Y of the top of the head for this sprite, in the character's own units — the same
        /// conversion <c>HeadAnchorBuilder</c> bakes into the clips.
        /// </summary>
        private static float CrownLocalY(Sprite sprite, int crownRow)
        {
            return (sprite.rect.height - crownRow - sprite.pivot.y) / sprite.pixelsPerUnit;
        }

        /// <summary>Vertical position of the hat expressed in the character root's local space.</summary>
        private float HatYInCharacterSpace(Transform hat)
        {
            return _subject.transform.InverseTransformPoint(hat.position).y;
        }

        /// <summary>
        /// First row from the top of the sprite whose widest continuous opaque run is at least
        /// <see cref="HeadCrownTable.MinHeadRunPixels"/> wide, read from the PNG on disk.
        /// </summary>
        /// <remarks>
        /// A plain topmost-opaque-pixel scan does not find the head: the fishing rod and line reach
        /// higher in the casting frames and would swing the reference wildly. Reading the file bytes
        /// rather than <c>sprite.texture</c> avoids needing Read/Write enabled on 100+ textures.
        /// </remarks>
        private int MeasureCrownRow(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return -1;
            }

            string path = AssetDatabase.GetAssetPath(sprite.texture);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return -1;
            }

            if (!_textureCache.TryGetValue(path, out Texture2D texture))
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.LoadImage(File.ReadAllBytes(path));
                _textureCache[path] = texture;
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
