using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PanicAtThePond.Shop
{
    /// <summary>
    /// Replaces a hand-painted composite shop preview with a live, layered render of the real
    /// prefab (base + hair + hat as separate objects).
    ///
    /// <para>Attach to the existing preview <see cref="Image"/>. It watches which composite sprite
    /// the shop assigns, maps that to the hair/hat it depicts, hides the composite, and shows a
    /// <see cref="CosmeticPreviewRig"/> render in its place. No shop code has to change, so this can
    /// be switched off again by disabling the component.</para>
    ///
    /// <para>Once every combination renders correctly the composite sprites become dead weight —
    /// 18 fisherman and 14 fish images, plus the 14 per-combination animator controllers — and a new
    /// hat needs only its own 64x64 sprite.</para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class LayeredCosmeticPreview : MonoBehaviour
    {
        /// <summary>What a legacy composite sprite depicts, so we can render the same thing live.</summary>
        private struct Depicts
        {
            public string Hair;
            public string Hat;
            public int FishSpecies;   // -1 for the fisherman
        }

        [Tooltip("Off by default. Turn on once the rendered previews have been eyeballed.")]
        [SerializeField] private bool _isEnabled;

        [Tooltip("True for the fisherman preview, false for a fish preview.")]
        [SerializeField] private bool _isFisherman = true;

        private static CosmeticPreviewRig s_rig;

        private Image _image;
        private RawImage _target;
        private Sprite _lastSprite;

        private static readonly Dictionary<string, Depicts> s_map = BuildMap();

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void OnEnable()
        {
            _lastSprite = null;
        }

        private void LateUpdate()
        {
            if (!_isEnabled || _image == null)
            {
                return;
            }

            if (_image.sprite == _lastSprite)
            {
                return;
            }

            _lastSprite = _image.sprite;
            Refresh();
        }

        /// <summary>Re-renders the preview for the sprite currently assigned to the Image.</summary>
        public void Refresh()
        {
            if (!_isEnabled || _image == null || _image.sprite == null)
            {
                return;
            }

            if (!s_map.TryGetValue(_image.sprite.name, out Depicts depicts))
            {
                // Unknown sprite: leave the original composite visible rather than blanking the UI.
                ShowComposite();
                return;
            }

            EnsureRig();
            EnsureTarget();

            if (_isFisherman)
            {
                s_rig.RenderFisherman(depicts.Hair, depicts.Hat);
            }
            else
            {
                s_rig.RenderFish(depicts.FishSpecies, depicts.Hat);
            }

            _target.texture = s_rig.Texture;
            _target.enabled = true;

            // Hide the painted composite without disabling the Image, so any layout or raycast
            // behaviour that depends on it is unchanged.
            Color c = _image.color;
            _image.color = new Color(c.r, c.g, c.b, 0f);
        }

        private void ShowComposite()
        {
            if (_target != null)
            {
                _target.enabled = false;
            }

            if (_image != null)
            {
                Color c = _image.color;
                _image.color = new Color(c.r, c.g, c.b, 1f);
            }
        }

        private static void EnsureRig()
        {
            if (s_rig != null)
            {
                return;
            }

            var go = new GameObject("CosmeticPreviewRig");
            DontDestroyOnLoad(go);
            s_rig = go.AddComponent<CosmeticPreviewRig>();
        }

        private void EnsureTarget()
        {
            if (_target != null)
            {
                return;
            }

            var go = new GameObject("LayeredPreviewOutput", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            _target = go.AddComponent<RawImage>();
            _target.raycastTarget = false;
        }

        /// <summary>
        /// Composite sprite name -> what it depicts. Taken from the shop cosmetic grid, where each
        /// cell's Image is the isolated accessory the composite was painted from.
        /// </summary>
        private static Dictionary<string, Depicts> BuildMap()
        {
            const string Yellow = "FisherMan_Hat_-Default_-_Fishing_Hat";
            var map = new Dictionary<string, Depicts>(System.StringComparer.Ordinal);

            // ---- Fisherman, black hair ----
            AddFisherman(map, "black hair", "Black_Hair", string.Empty);
            AddFisherman(map, "black hair yellow hat", "Black_Hair", Yellow);
            AddFisherman(map, "Black hair Blu cap", "Black_Hair", "FisherMan_Hat_-Blue_Cap");
            AddFisherman(map, "Black Hair Red Cap", "Black_Hair", "FisherMan_Hat_-Red_Cap");
            AddFisherman(map, "black hair turtle", "Black_Hair", "TurtleHat_0");
            AddFisherman(map, "Black Hair Ranger hat", "Black_Hair", "FisherMan_Hat_-Ranger_Hat_0");
            AddFisherman(map, "black hair white soda", "Black_Hair", "FisherMan_Hat_-Chef_Hat");
            AddFisherman(map, "black hair green frog", "Black_Hair", "FisherMan_Hat_-Fish_Hat");
            AddFisherman(map, "black hair headfon", "Black_Hair", "FisherMan_Hat_-Soda_Hat");

            // ---- Fisherman, red hair ----
            AddFisherman(map, "Fisherman Red hair", "Red_Hair", string.Empty);
            AddFisherman(map, "Fisherman Yellow hat", "Red_Hair", Yellow);
            AddFisherman(map, "Fisherman blue cap hat", "Red_Hair", "FisherMan_Hat_-Blue_Cap");
            AddFisherman(map, "Fisherman red hat", "Red_Hair", "FisherMan_Hat_-Red_Cap");
            AddFisherman(map, "Fisherman  Turtle hat", "Red_Hair", "TurtleHat_0");
            AddFisherman(map, "Fisherman griin hat", "Red_Hair", "FisherMan_Hat_-Ranger_Hat_0");
            AddFisherman(map, "fishaerman white hat", "Red_Hair", "FisherMan_Hat_-Chef_Hat");
            AddFisherman(map, "Fishermna Green hat", "Red_Hair", "FisherMan_Hat_-Fish_Hat");
            AddFisherman(map, "Fishermna headphone hat", "Red_Hair", "FisherMan_Hat_-Soda_Hat");

            // ---- Fish: bass (species 0) ----
            AddFish(map, "bass", 0, string.Empty);
            AddFish(map, "fish yellow hat", 0, Yellow);
            AddFish(map, "Fish Boat hat", 0, "paper_boat");
            AddFish(map, "fish polish hat", 0, "beret");
            AddFish(map, "Fish orange hat", 0, "hat");
            AddFish(map, "Fish Black hat", 0, "hat2");
            AddFish(map, "Fish Cap Hat", 0, "cap");

            // ---- Fish: trout (species 1) ----
            AddFish(map, "trout", 1, string.Empty);
            AddFish(map, "trout yellow hat", 1, Yellow);
            AddFish(map, "Trout Boat hat", 1, "paper_boat");
            AddFish(map, "trout polish hat", 1, "beret");
            AddFish(map, "Trout orange hat", 1, "hat");
            AddFish(map, "Trout black hat", 1, "hat2");
            AddFish(map, "Trout Cap hat", 1, "cap");

            return map;
        }

        private static void AddFisherman(Dictionary<string, Depicts> map, string sprite, string hair, string hat)
        {
            map[sprite] = new Depicts { Hair = hair, Hat = hat, FishSpecies = -1 };
        }

        private static void AddFish(Dictionary<string, Depicts> map, string sprite, int species, string hat)
        {
            map[sprite] = new Depicts { Hair = string.Empty, Hat = hat, FishSpecies = species };
        }
    }
}
