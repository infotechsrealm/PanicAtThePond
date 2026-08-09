using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PanicAtThePond.Data;
using PanicAtThePond.Shop;

namespace PanicAtThePond.Tests.PlayMode
{
    /// <summary>
    /// End-to-end verification that a cosmetic hat travels <b>with</b> the head it sits on.
    ///
    /// These tests instantiate the real shipping prefabs, apply a real cosmetic through the real
    /// public API, let the real Animator play a full cycle, and sample the hat's actual local Y on
    /// every frame. Nothing is stubbed. The expected direction per frame comes from measuring the
    /// source art (see project_overview.md §15.1), not from the implementation.
    /// </summary>
    [TestFixture]
    public sealed class HatFollowsBodyTests
    {
        private const string FishermanPrefab = "Fisherman";
        private const string FishPrefab = "Fish";
        private const string FishermanHat = "FisherMan_Hat_-Blue_Cap";
        private const string FishHat = "cap";

        /// <summary>
        /// Upper bound on frames to sample. The clips are ~0.68 s and an in-Editor PlayMode test
        /// ticks at roughly 5 ms per frame, so a full cycle needs ~140 frames; this leaves a wide
        /// margin and the loop exits as soon as all four frames have been seen.
        /// </summary>
        private const int MaxSampleFrames = 900;
        private const float Tolerance = 0.0005f;

        /// <summary>
        /// Allowed head-to-hat divergence, in source pixels. The correct value is exactly zero; this
        /// only absorbs float noise. Do not widen it to make a hat pass.
        /// </summary>
        private const float TolerancePixels = 0.02f;

        private GameObject _subject;

        [TearDown]
        public void TearDown()
        {
            if (_subject != null)
            {
                Object.Destroy(_subject);
                _subject = null;
            }
        }

        /// <summary>
        /// Instantiates a prefab with every gameplay MonoBehaviour disabled, so the Animator, the
        /// SpriteRenderer and CosmeticRuntimeApplier are the only things running. The gameplay
        /// controllers expect a live network session and would throw in an empty test scene.
        /// </summary>
        private GameObject SpawnInert(string resourcePath)
        {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            Assert.That(prefab, Is.Not.Null, $"Resources/{resourcePath} is missing.");

            GameObject instance = Object.Instantiate(prefab);
            instance.name = "Test_" + resourcePath;

            foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is CosmeticRuntimeApplier)
                {
                    continue;
                }
                behaviour.enabled = false;
            }

            return instance;
        }

        /// <summary>
        /// Finds an applied cosmetic wherever it lives — under <c>HeadAnchor</c> on a rebuilt
        /// character, or directly under the root before that.
        /// </summary>
        private static Transform FindCosmetic(GameObject subject, string childName)
        {
            foreach (Transform candidate in subject.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == childName)
                {
                    return candidate;
                }
            }
            return null;
        }

        /// <summary>
        /// Hat position in the character root's space. Reading <c>hat.localPosition</c> would report a
        /// constant once the hat is parented to the animated anchor, which is the sit offset rather
        /// than where the hat actually sits.
        /// </summary>
        private static float HatYInCharacterSpace(GameObject subject, Transform hat)
        {
            return subject.transform.InverseTransformPoint(hat.position).y;
        }

        /// <summary>One sampled animation frame: where the head was, and where the hat was.</summary>
        private struct Sample
        {
            public string SpriteName;
            public float HatY;
        }

        /// <summary>
        /// Plays <paramref name="stateName"/> and records the body sprite and the hat's position for
        /// each distinct frame, keyed by the 1-based number in the body sprite's name.
        /// </summary>
        private static IEnumerator SampleCycle(
            GameObject subject,
            Animator animator,
            SpriteRenderer body,
            Transform hat,
            string stateName,
            Dictionary<int, Sample> samplesByFrameNumber)
        {
            animator.Play(stateName, 0, 0f);
            yield return null;

            for (int i = 0; i < MaxSampleFrames && samplesByFrameNumber.Count < 4; i++)
            {
                yield return null;

                if (body.sprite == null)
                {
                    continue;
                }

                int frameNumber = TrailingNumber(body.sprite.name);
                if (frameNumber > 0 && !samplesByFrameNumber.ContainsKey(frameNumber))
                {
                    samplesByFrameNumber[frameNumber] = new Sample
                    {
                        SpriteName = body.sprite.name,
                        HatY = HatYInCharacterSpace(subject, hat)
                    };
                }
            }
        }

        /// <summary>
        /// Requires the hat to have moved exactly as far as the head did, in source pixels, on every
        /// sampled frame.
        /// </summary>
        /// <remarks>
        /// <para>This replaces per-frame expectations that were written by hand from the old
        /// <c>HeadCenterYGrid</c> — a table measured from the wrong artwork, so the numbers encoded
        /// motion the shipping sprites do not have. Asserting only the <i>direction</i> of movement
        /// was the other half of the problem: a bob four times too small still moves the right way,
        /// which is how a badly broken build passed.</para>
        ///
        /// <para>The reference is <see cref="HeadCrownTable"/>, which an EditMode test independently
        /// checks against the PNGs on disk. The hat's position now comes from a curve inside the
        /// AnimationClip — a different source entirely — so comparing the two is a real measurement
        /// rather than an identity.</para>
        /// </remarks>
        private static void AssertHatTracksHead(
            Dictionary<int, Sample> samples, string stateName, SpriteRenderer body)
        {
            HeadCrownTable table = HeadCrownTable.Active;
            Assert.That(table, Is.Not.Null, "SO_HeadCrownTable is missing.");

            float unitsPerPixel = 1f / body.sprite.pixelsPerUnit;
            Sample reference = samples[1];
            int referenceCrown = table.GetCrownRow(reference.SpriteName);
            Assert.That(referenceCrown, Is.GreaterThanOrEqualTo(0),
                $"'{reference.SpriteName}' is not in the crown table.");

            for (int frame = 2; frame <= 4; frame++)
            {
                Sample sample = samples[frame];
                int crown = table.GetCrownRow(sample.SpriteName);
                Assert.That(crown, Is.GreaterThanOrEqualTo(0),
                    $"'{sample.SpriteName}' is not in the crown table.");

                float headMovedPixels = referenceCrown - crown;
                float hatMovedPixels = (sample.HatY - reference.HatY) / unitsPerPixel;

                Assert.That(hatMovedPixels, Is.EqualTo(headMovedPixels).Within(TolerancePixels),
                    $"{stateName} frame {frame} ({sample.SpriteName}): head moved "
                    + $"{headMovedPixels:+0.00;-0.00;0.00} px but hat moved "
                    + $"{hatMovedPixels:+0.00;-0.00;0.00} px");
            }
        }

        private static int TrailingNumber(string name)
        {
            int start = name.Length;
            while (start > 0 && name[start - 1] >= '0' && name[start - 1] <= '9')
            {
                start--;
            }

            if (start == name.Length)
            {
                return 0;
            }

            int value = 0;
            for (int i = start; i < name.Length; i++)
            {
                value = value * 10 + (name[i] - '0');
            }
            return value;
        }

        private static void AssertAllFramesSampled(Dictionary<int, Sample> samples, string stateName)
        {
            for (int frame = 1; frame <= 4; frame++)
            {
                Assert.That(samples.ContainsKey(frame), Is.True,
                    $"Frame {frame} of '{stateName}' never rendered — the animator did not complete a cycle.");
            }
        }

        /// <summary>
        /// The fisherman's hat must track the head through a real idle cycle driven by the real
        /// frame loop — not just under a hand-stepped animator.
        /// </summary>
        [UnityTest]
        public IEnumerator FishermanHat_TracksHeadThroughIdleCycle()
        {
            _subject = SpawnInert(FishermanPrefab);
            CosmeticRuntimeApplier.ApplyFishermanCosmeticsByName(_subject, FishermanHat, null);
            yield return null;

            Transform hat = FindCosmetic(_subject, "Applied Fisherman Hat Cosmetic");
            Assert.That(hat, Is.Not.Null, "Hat cosmetic was not created on the fisherman.");

            Animator animator = _subject.GetComponent<Animator>();
            SpriteRenderer body = _subject.GetComponent<SpriteRenderer>();

            var samples = new Dictionary<int, Sample>();
            yield return SampleCycle(_subject, animator, body, hat, "AC_IdelLeft", samples);
            AssertAllFramesSampled(samples, "AC_IdelLeft");
            AssertHatTracksHead(samples, "AC_IdelLeft", body);
        }

        /// <summary>The same, for the fish's swim cycle.</summary>
        [UnityTest]
        public IEnumerator FishHat_TracksHeadThroughIdleCycle()
        {
            _subject = SpawnInert(FishPrefab);
            CosmeticRuntimeApplier.ApplyFishHatByName(_subject, FishHat);
            yield return null;

            Transform hat = FindCosmetic(_subject, "Applied Fish Hat Cosmetic");
            Assert.That(hat, Is.Not.Null, "Hat cosmetic was not created on the fish.");

            Animator animator = _subject.GetComponent<Animator>();
            SpriteRenderer body = _subject.GetComponent<SpriteRenderer>();

            var samples = new Dictionary<int, Sample>();
            yield return SampleCycle(_subject, animator, body, hat, "AC_Fish1Idel", samples);
            AssertAllFramesSampled(samples, "AC_Fish1Idel");
            AssertHatTracksHead(samples, "AC_Fish1Idel", body);
        }

        /// <summary>
        /// The hat must never sit in the same place on two frames whose heads are at different
        /// heights — that is the signature of an index that wrapped or saturated.
        /// </summary>
        [UnityTest]
        public IEnumerator FishermanHat_DoesNotFlattenAcrossCycle()
        {
            _subject = SpawnInert(FishermanPrefab);
            CosmeticRuntimeApplier.ApplyFishermanCosmeticsByName(_subject, FishermanHat, null);
            yield return null;

            Transform hat = FindCosmetic(_subject, "Applied Fisherman Hat Cosmetic");
            Animator animator = _subject.GetComponent<Animator>();
            SpriteRenderer body = _subject.GetComponent<SpriteRenderer>();

            var samples = new Dictionary<int, Sample>();
            yield return SampleCycle(_subject, animator, body, hat, "AC_IdelLeft", samples);
            AssertAllFramesSampled(samples, "AC_IdelLeft");

            HeadCrownTable table = HeadCrownTable.Active;
            Assert.That(table, Is.Not.Null, "SO_HeadCrownTable is missing.");

            // Any two frames whose heads sit at different heights must place the hat differently.
            // Comparing against the measured crown rather than assuming which frames differ keeps
            // this honest if the art changes.
            for (int a = 1; a <= 4; a++)
            {
                for (int b = a + 1; b <= 4; b++)
                {
                    if (table.GetCrownRow(samples[a].SpriteName) == table.GetCrownRow(samples[b].SpriteName))
                    {
                        continue;
                    }

                    Assert.That(samples[b].HatY, Is.Not.EqualTo(samples[a].HatY).Within(Tolerance),
                        $"frames {a} and {b} have different head heights, so the hat must differ too");
                }
            }
        }
    }
}
