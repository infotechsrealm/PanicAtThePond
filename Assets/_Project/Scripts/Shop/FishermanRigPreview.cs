using System;
using UnityEngine;
using UnityEngine.UI;

namespace PanicAtThePond.Shop
{
    /// <summary>
    /// The shop's fisherman display, drawn from the v2 rig's own layers instead of one painted
    /// picture per hat and hair colour.
    /// </summary>
    /// <remarks>
    /// <para><b>What it shows.</b> One frame of the rig (<see cref="frameSuffix"/>, by default
    /// <c>Idle_L_0</c>, which is the artist's shop picture pixel for pixel) with the player's
    /// <i>actual</i> selection: the hat's frame from its v2 sheet and the chosen hair colour. It reads
    /// the selection every frame, so it can never disagree with what the player will wear, and a new
    /// hat needs no picture: its sheet already holds this frame.</para>
    ///
    /// <para><b>Why it replaced the painted pictures.</b> Those were one image per combination of the
    /// old fisherman. They had drifted from the hats they named (the Red Cap floated above his head,
    /// the Ranger Hat showed the Fish Hat), newer hats such as the Gold Cap had none at all, and the
    /// character was the old art. The host Image keeps its rect and raycast behaviour and is only
    /// made transparent, so the shop code that still assigns those pictures is harmless.</para>
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class FishermanRigPreview : MonoBehaviour
    {
        [Serializable]
        public sealed class Frame
        {
            public Sprite body;
            public Sprite boat;
            public Sprite oars;
            public Sprite arms;
            public Sprite head;
            public Sprite hairRed;
            public Sprite hairBlack;
            public Sprite rods;
        }

        [Tooltip("The rig frame every layer is taken from; the hat sheet is looked up by the same suffix.")]
        public string frameSuffix = "Idle_L_0";

        public Frame frame = new Frame();

        private Image host;
        private Image hair, hat;
        private Sprite shownHat;
        private bool shownBlack;
        private bool built;
        private bool hasShown;

        private void Awake()
        {
            host = GetComponent<Image>();

            // The earlier live preview renders the old modular fisherman into a RawImage over this
            // Image; with both on, the old character and its misplaced hats covered this one.
            LayeredCosmeticPreview legacy = GetComponent<LayeredCosmeticPreview>();
            if (legacy != null)
            {
                legacy.enabled = false;
            }
        }

        private void OnEnable()
        {
            hasShown = false;
        }

        private void LateUpdate()
        {
            if (host == null)
            {
                return;
            }

            if (!built)
            {
                Build();
            }

            // The shop still assigns its painted pictures to the host; keep them invisible.
            if (host.color.a != 0f)
            {
                Color c = host.color;
                host.color = new Color(c.r, c.g, c.b, 0f);
            }

            Sprite selectedHat = CosmeticRuntimeApplier.GetSelectedFishermanHat();
            Sprite selectedHair = CosmeticRuntimeApplier.GetSelectedFishermanHair();
            bool black = selectedHat == null && selectedHair != null
                && selectedHair.name.ToLowerInvariant().Contains("black");

            if (hasShown && selectedHat == shownHat && black == shownBlack)
            {
                return;
            }

            hasShown = true;
            shownHat = selectedHat;
            shownBlack = black;

            hair.sprite = black ? frame.hairBlack : frame.hairRed;
            hat.sprite = FindHatFrame(selectedHat);
            hat.enabled = hat.sprite != null;
        }

        private Sprite FindHatFrame(Sprite selectedHat)
        {
            foreach (Sprite sprite in CosmeticRuntimeApplier.LoadFishermanHatSheet(selectedHat))
            {
                if (sprite != null && sprite.name.EndsWith("_" + frameSuffix, StringComparison.Ordinal))
                {
                    return sprite;
                }
            }

            return null;
        }

        /// <summary>Stacks one Image per layer, in the rig's own sorting order.</summary>
        private void Build()
        {
            built = true;
            AddLayer("Body", frame.body);
            AddLayer("Boat", frame.boat);
            AddLayer("Oars", frame.oars);
            AddLayer("Arms", frame.arms);
            AddLayer("Head", frame.head);
            hair = AddLayer("Hair", frame.hairRed);
            hat = AddLayer("Hat", null);
            AddLayer("Rods", frame.rods);
        }

        private Image AddLayer(string layerName, Sprite sprite)
        {
            var go = new GameObject("RigPreview_" + layerName, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true; // every layer is the same 320x320 canvas, so they stay registered
            image.raycastTarget = false;
            image.enabled = sprite != null;
            return image;
        }
    }
}
