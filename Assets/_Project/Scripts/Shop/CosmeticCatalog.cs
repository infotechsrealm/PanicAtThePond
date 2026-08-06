using System;
using System.Collections.Generic;
using UnityEngine;

namespace PanicAtThePond.Shop
{
    /// <summary>Which slot a catalog entry belongs to.</summary>
    public enum CosmeticCategory
    {
        Unspecified,
        FishHat,
        FishermanHat,
        FishermanHair,
        FishSpecies,
        UiIcon
    }

    /// <summary>
    /// One swappable asset. Change <see cref="icon"/> or <see cref="animator"/> in the Inspector and
    /// the game picks up the new art with no code, JSON or filename changes.
    /// </summary>
    [Serializable]
    public sealed class CosmeticCatalogEntry
    {
        [Tooltip("Stable identifier. This is what gets saved and sent over the network, so it must " +
                 "NOT change when you swap the artwork. Existing IDs match the old sprite names.")]
        public string id;

        [Tooltip("Human-readable name for UI. Safe to change at any time.")]
        public string displayName;

        public CosmeticCategory category = CosmeticCategory.Unspecified;

        [Tooltip("The artwork. Swap this to re-skin the item.")]
        public Sprite icon;

        [Tooltip("Optional animator for cosmetics that drive a character animation set.")]
        public RuntimeAnimatorController animator;

        [Tooltip("Other IDs that should resolve to this entry (old names, spelling variants).")]
        public string[] aliases;
    }

    /// <summary>
    /// Explicit, Inspector-editable registry of every swappable cosmetic and UI asset.
    ///
    /// <para><b>Why this exists.</b> Cosmetics used to be resolved by fuzzy string matching on sprite
    /// filenames (<c>hatName.Contains("blue_cap")</c>) combined with <c>Resources.Load</c> on literal
    /// paths. That made the art impossible to rename or replace safely, because the same strings are
    /// also written to save data and sent over the network. This catalog binds assets by direct
    /// reference instead, so the <b>ID stays stable while the artwork is free to change</b>.</para>
    ///
    /// <para><b>To replace an asset:</b> select the catalog, find the entry, drag a new sprite into
    /// <c>icon</c>. Nothing else needs touching.</para>
    ///
    /// <para><b>To add an asset:</b> add an entry, give it a new unique ID, assign the sprite. If the
    /// item is purchasable, add the same ID to <c>shop_config.json</c>.</para>
    ///
    /// <para>Lookups fall back to the legacy name-matching path when an ID is missing, so an
    /// incomplete catalog degrades to the old behaviour rather than losing the cosmetic.</para>
    /// </summary>
    [CreateAssetMenu(menuName = "PanicAtThePond/Config/Cosmetic Catalog", fileName = "SO_CosmeticCatalog")]
    public sealed class CosmeticCatalog : ScriptableObject
    {
        /// <summary>Resources path the runtime loads the active catalog from.</summary>
        public const string ResourcePath = "SO_CosmeticCatalog";

        [SerializeField] private List<CosmeticCatalogEntry> _entries = new List<CosmeticCatalogEntry>();

        private Dictionary<string, CosmeticCatalogEntry> _lookup;
        private Dictionary<string, CosmeticCatalogEntry> _exactLookup;
        private static CosmeticCatalog s_active;
        private static bool s_activeSearched;

        /// <summary>Every entry, in authoring order. Read-only at runtime.</summary>
        public IReadOnlyList<CosmeticCatalogEntry> Entries => _entries;

        /// <summary>
        /// The catalog the game uses, loaded once from Resources. Null when none has been created
        /// yet, in which case every caller falls back to the legacy lookup.
        /// </summary>
        public static CosmeticCatalog Active
        {
            get
            {
                if (!s_activeSearched)
                {
                    s_activeSearched = true;
                    s_active = Resources.Load<CosmeticCatalog>(ResourcePath);
                }

                return s_active;
            }
        }

        /// <summary>Finds an entry by ID or alias. Matching ignores case, spaces, dashes and underscores.</summary>
        /// <param name="id">The stable cosmetic ID.</param>
        /// <param name="entry">The matching entry, or null.</param>
        /// <returns>True when a match was found.</returns>
        public bool TryGetEntry(string id, out CosmeticCatalogEntry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            BuildLookupIfNeeded();

            // Exact name wins. Normalisation deliberately collapses spaces/underscores/dashes so
            // "FisherMan Hat -Blue Cap" and "fisherman_hat_-blue_cap" resolve alike, but that also
            // merges genuinely distinct assets — "black hair" and "Black_Hair" are different sprites.
            // Trying the exact key first keeps those apart while preserving the loose matching.
            if (_exactLookup.TryGetValue(id, out entry))
            {
                return true;
            }

            return _lookup.TryGetValue(Normalize(id), out entry);
        }

        /// <summary>Returns the icon for an ID, or null when the catalog has no entry for it.</summary>
        public Sprite GetIcon(string id)
        {
            return TryGetEntry(id, out CosmeticCatalogEntry entry) ? entry.icon : null;
        }

        /// <summary>Returns the animator for an ID, or null when the catalog has no entry for it.</summary>
        public RuntimeAnimatorController GetAnimator(string id)
        {
            return TryGetEntry(id, out CosmeticCatalogEntry entry) ? entry.animator : null;
        }

        /// <summary>Rebuilds the lookup. Call after editing entries at runtime or from an Editor tool.</summary>
        public void Invalidate()
        {
            _lookup = null;
        }

        private void OnValidate()
        {
            // Entries edited in the Inspector must not serve stale lookups.
            _lookup = null;
        }

        private void BuildLookupIfNeeded()
        {
            if (_lookup != null)
            {
                return;
            }

            _lookup = new Dictionary<string, CosmeticCatalogEntry>(StringComparer.Ordinal);
            _exactLookup = new Dictionary<string, CosmeticCatalogEntry>(StringComparer.Ordinal);
            for (int i = 0; i < _entries.Count; i++)
            {
                CosmeticCatalogEntry entry = _entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.id))
                {
                    continue;
                }

                Register(entry.id, entry);
                if (entry.aliases == null)
                {
                    continue;
                }

                for (int a = 0; a < entry.aliases.Length; a++)
                {
                    Register(entry.aliases[a], entry);
                }
            }
        }

        private void Register(string key, CosmeticCatalogEntry entry)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (!_exactLookup.ContainsKey(key))
            {
                _exactLookup[key] = entry;
            }

            string normalized = Normalize(key);
            if (!_lookup.ContainsKey(normalized))
            {
                _lookup[normalized] = entry;
            }
        }

        /// <summary>
        /// Collapses the cosmetic-name variants that exist across the codebase (spaces vs
        /// underscores vs dashes, mixed case) onto one key.
        /// </summary>
        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var sb = new System.Text.StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == ' ' || c == '_' || c == '-')
                {
                    continue;
                }

                sb.Append(char.ToLowerInvariant(c));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Clears the cached active catalog. Required because the project may run with Domain Reload
        /// disabled, which would otherwise carry a stale reference across play sessions.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_active = null;
            s_activeSearched = false;
        }
    }
}
