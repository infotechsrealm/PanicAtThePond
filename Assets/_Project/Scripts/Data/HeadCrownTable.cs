using System.Collections.Generic;
using UnityEngine;

namespace PanicAtThePond.Data
{
    /// <summary>
    /// Where the top of the character's head sits in each animation frame, measured from the actual
    /// sprites and baked at edit time.
    /// </summary>
    /// <remarks>
    /// Cosmetics need to know how far the head moves between frames so a hat can follow it. The old
    /// approach was a hand-maintained 24x4 constant in <c>CosmeticRuntimeApplier</c> that had been
    /// measured from <c>FishermansAnimations-Head_Sheet.png</c> — but the shipping fisherman renders
    /// *composited* sprites (<c>IdleLeft1</c>…), and the two disagree. On <c>AC_CastingLeft</c> the
    /// real head moves 6 px between frames while the table said 0, which is why the hat still looked
    /// detached after the index and magnitude fixes.
    ///
    /// Measuring the sprites that are actually drawn removes the whole class of problem, and the
    /// table regenerates itself when the art changes instead of needing to be re-derived by hand.
    /// Baked rather than measured at runtime because the frame textures are not Read/Write enabled
    /// (104 of them for the fisherman alone).
    ///
    /// Regenerate with <c>Panic At The Pond ▸ Rebuild Head Crown Table</c>.
    /// </remarks>
    public sealed class HeadCrownTable : ScriptableObject
    {
        /// <summary>Resources path the runtime loads this from.</summary>
        public const string ResourcePath = "SO_HeadCrownTable";

        /// <summary>
        /// Minimum width of a continuous opaque run for a row to count as the head rather than the
        /// fishing rod or line, which are only a pixel or two wide.
        /// </summary>
        public const int MinHeadRunPixels = 6;

        [System.Serializable]
        public struct Entry
        {
            public string spriteName;

            /// <summary>Rows from the TOP of the sprite down to the first row of head.</summary>
            public int crownRow;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<string, int> lookup;
        private static HeadCrownTable active;

        /// <summary>The table loaded from Resources, or null when it has never been generated.</summary>
        public static HeadCrownTable Active
        {
            get
            {
                if (active == null)
                {
                    active = Resources.Load<HeadCrownTable>(ResourcePath);
                }
                return active;
            }
        }

        /// <summary>Number of baked entries. Zero means the table needs regenerating.</summary>
        public int Count => entries != null ? entries.Count : 0;

        /// <summary>
        /// Crown row for <paramref name="spriteName"/>, or -1 when the sprite is not in the table.
        /// Callers must treat -1 as "unknown" and apply no bob rather than guessing.
        /// </summary>
        public int GetCrownRow(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                return -1;
            }

            if (lookup == null)
            {
                lookup = new Dictionary<string, int>(entries.Count);
                for (int i = 0; i < entries.Count; i++)
                {
                    lookup[entries[i].spriteName] = entries[i].crownRow;
                }
            }

            return lookup.TryGetValue(spriteName, out int row) ? row : -1;
        }

        /// <summary>Replaces the baked data. Editor-only path, called by the generator.</summary>
        public void SetEntries(List<Entry> newEntries)
        {
            entries = newEntries ?? new List<Entry>();
            lookup = null;
        }

        /// <summary>Drops the cached static so a regenerated asset is picked up without a reload.</summary>
        public static void ClearCache()
        {
            active = null;
        }
    }
}
