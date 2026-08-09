using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using PanicAtThePond.Shop;

namespace PanicAtThePond.Tests.EditMode
{
    /// <summary>
    /// A fish cosmetic chosen in the shop must be stored as the <b>cosmetic</b>, never as the shop's
    /// composite preview picture.
    /// </summary>
    /// <remarks>
    /// The shop shows each fish hat as a whole fish already wearing it ("Fish Cap Hat", "Trout Boat
    /// hat"). Those previews have to be resolved back to the wearable sprite ("cap", "paper_boat")
    /// before being stored, and <c>IsPreviewSprite</c> decided that by name: it matched anything
    /// starting with "fisherman ", "fishermna ", "fishaerman " or containing "preview". Every
    /// fisherman preview matches one of those and <b>no fish preview matches any of them</b>, so fish
    /// previews were stored and rendered verbatim — the fish wore a picture of a fish wearing a hat.
    /// That is why fisherman cosmetics stuck and fish cosmetics appeared not to.
    /// </remarks>
    [TestFixture]
    public sealed class FishCosmeticSelectionTests
    {
        private const string FishPreviewFolder = "ShopUI/Fish preview";

        private static readonly HashSet<string> WearableFishHats =
            new HashSet<string> { "paper_boat", "cap", "hat", "hat2", "beret" };

        private string _savedSelection;

        [SetUp]
        public void SetUp()
        {
            _savedSelection = PlayerPrefs.GetString(CosmeticRuntimeApplier.SelectedFishHatPrefKey, string.Empty);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.SetString(CosmeticRuntimeApplier.SelectedFishHatPrefKey, _savedSelection);
            PlayerPrefs.Save();
        }

        [Test]
        public void FishPreviewSprites_AreNeverStoredAsTheSelectedHat()
        {
            Sprite[] previews = Resources.LoadAll<Sprite>(FishPreviewFolder);
            Assert.That(previews, Is.Not.Empty, $"Resources/{FishPreviewFolder} is empty.");

            var failures = new List<string>();

            foreach (Sprite preview in previews)
            {
                // Start from a known good selection so a refusal is distinguishable from a store.
                CosmeticRuntimeApplier.SelectFishHat(FindWearable("cap"));

                CosmeticRuntimeApplier.SelectFishHat(preview);
                Sprite stored = CosmeticRuntimeApplier.GetSelectedFishHat();
                string storedName = stored != null ? stored.name : "<null>";

                if (storedName == preview.name)
                {
                    failures.Add($"'{preview.name}' was stored verbatim — the fish would wear the "
                        + "preview picture instead of the hat");
                }
            }

            Assert.That(failures, Is.Empty,
                $"{failures.Count} of {previews.Length} fish previews were not resolved:\n  "
                + string.Join("\n  ", failures));
        }

        /// <summary>
        /// The previews that depict one of the shop's fish hats must resolve to exactly that hat.
        /// </summary>
        /// <remarks>
        /// Species portraits ("bass", "trout") and the yellow hat — which maps to a fisherman
        /// cosmetic and is refused by the slot guard — are excluded: they legitimately leave the
        /// previous selection alone. What matters here is that a preview which does depict a fish hat
        /// resolves to a hat that can actually be worn.
        /// </remarks>
        [Test]
        public void FishHatPreviews_ResolveToTheHatTheyDepict()
        {
            Sprite[] previews = Resources.LoadAll<Sprite>(FishPreviewFolder);
            var failures = new List<string>();
            int resolved = 0;

            foreach (Sprite preview in previews)
            {
                string name = preview.name.ToLowerInvariant();
                if (!name.Contains("hat") || name.Contains("yellow"))
                {
                    continue;
                }

                CosmeticRuntimeApplier.SelectFishHat(preview);
                Sprite stored = CosmeticRuntimeApplier.GetSelectedFishHat();
                string storedName = stored != null ? stored.name : "<null>";

                resolved++;
                if (!WearableFishHats.Contains(storedName))
                {
                    failures.Add($"'{preview.name}' resolved to '{storedName}', which is not a "
                        + "wearable fish hat");
                }
            }

            Assert.That(resolved, Is.GreaterThan(0), "No fish hat previews were exercised.");
            Assert.That(failures, Is.Empty,
                $"{failures.Count} of {resolved} fish hat previews resolved wrongly:\n  "
                + string.Join("\n  ", failures));
        }

        private static Sprite FindWearable(string spriteName)
        {
            foreach (Sprite sprite in Resources.LoadAll<Sprite>("ShopUI"))
            {
                if (sprite != null && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            Assert.Fail($"Could not find the wearable sprite '{spriteName}' in Resources/ShopUI.");
            return null;
        }
    }
}
