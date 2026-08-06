using System;
using System.Collections.Generic;
using UnityEngine;

namespace PanicAtThePond.Shop
{
    /// <summary>One preview sprite's measured drawn-content bounds, as a fraction of its full rect.</summary>
    [Serializable]
    public sealed class PreviewBoundsEntry
    {
        [Tooltip("Sprite name this measurement belongs to.")]
        public string spriteName;

        [Tooltip("Width of the drawn (non-transparent) content as a fraction of the sprite's full width.")]
        [Range(0.01f, 1f)] public float contentWidthFraction = 1f;

        [Tooltip("Height of the drawn (non-transparent) content as a fraction of the sprite's full height.")]
        [Range(0.01f, 1f)] public float contentHeightFraction = 1f;
    }

    /// <summary>
    /// Measured drawn-content bounds for the fisherman shop-preview sprites.
    ///
    /// <para><b>Why this exists.</b> The nine fisherman preview sprites are all 500x500, but the
    /// fisherman is hand-drawn at a different scale inside each one — measured content ranges from
    /// 251x228 to 293x253, a 17% spread. Because every sprite shares the same rect, uGUI renders
    /// them at the same rect size and the fisherman visibly changes size as you cycle hats.</para>
    ///
    /// <para>These fractions let <see cref="UniformPreviewSizer"/> scale each sprite so the drawn
    /// fisherman — not the transparent canvas around it — is the same size on screen every time.
    /// Values are measured from the source PNGs by
    /// <c>Tools ▸ Panic At The Pond ▸ Rebuild Preview Bounds Catalog</c>; never hand-edit them.</para>
    /// </summary>
    [CreateAssetMenu(fileName = "SO_PreviewBoundsCatalog", menuName = "PanicAtThePond/Shop/Preview Bounds Catalog")]
    public sealed class PreviewBoundsCatalog : ScriptableObject
    {
        private const string ResourcesPath = "SO_PreviewBoundsCatalog";

        [SerializeField] private List<PreviewBoundsEntry> _entries = new List<PreviewBoundsEntry>();

        private Dictionary<string, PreviewBoundsEntry> _lookup;

        private static PreviewBoundsCatalog s_active;

        /// <summary>Every measured entry. Ordered as written by the rebuild tool.</summary>
        public IReadOnlyList<PreviewBoundsEntry> Entries => _entries;

        /// <summary>The catalog loaded from Resources, or null when it has not been built yet.</summary>
        public static PreviewBoundsCatalog Active
        {
            get
            {
                if (s_active == null)
                {
                    s_active = Resources.Load<PreviewBoundsCatalog>(ResourcesPath);
                }

                return s_active;
            }
        }

        /// <summary>Replaces the whole entry list. Editor-tool use only.</summary>
        public void SetEntries(List<PreviewBoundsEntry> entries)
        {
            _entries = entries ?? new List<PreviewBoundsEntry>();
            _lookup = null;
        }

        /// <summary>
        /// Returns the measured content fractions for <paramref name="spriteName"/>.
        /// Falls back to 1,1 (treat the whole rect as content) when the sprite was never measured,
        /// which reproduces the old behaviour rather than guessing.
        /// </summary>
        public bool TryGetFractions(string spriteName, out float widthFraction, out float heightFraction)
        {
            widthFraction = 1f;
            heightFraction = 1f;

            if (string.IsNullOrEmpty(spriteName))
            {
                return false;
            }

            BuildLookupIfNeeded();
            if (!_lookup.TryGetValue(spriteName, out PreviewBoundsEntry entry))
            {
                return false;
            }

            widthFraction = Mathf.Max(0.01f, entry.contentWidthFraction);
            heightFraction = Mathf.Max(0.01f, entry.contentHeightFraction);
            return true;
        }

        /// <summary>Clears the cached static so a rebuilt asset is picked up without a domain reload.</summary>
        public static void ResetCache()
        {
            s_active = null;
        }

        private void BuildLookupIfNeeded()
        {
            if (_lookup != null)
            {
                return;
            }

            _lookup = new Dictionary<string, PreviewBoundsEntry>(StringComparer.Ordinal);
            foreach (PreviewBoundsEntry entry in _entries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.spriteName) && !_lookup.ContainsKey(entry.spriteName))
                {
                    _lookup[entry.spriteName] = entry;
                }
            }
        }
    }
}
