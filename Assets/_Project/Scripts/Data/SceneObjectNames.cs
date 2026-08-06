namespace PanicAtThePond.Data
{
    /// <summary>
    /// Every GameObject name the code looks up at runtime, in one place.
    ///
    /// <para><b>Why this exists.</b> These names are load-bearing: <c>transform.Find("head")</c>,
    /// <c>name.Contains("Score")</c> and friends were scattered across a dozen files, so renaming an
    /// object in a scene silently broke the fisherman rig, the score table, cloud parallax or the
    /// shop panel lookups with no compile error. Centralising them means a rename is a one-line
    /// change here plus the scene edit, and every dependent site updates with it.</para>
    ///
    /// <para><b>Rule:</b> never type one of these names as a literal in gameplay code. If you need a
    /// new one, add it here first. Better still, prefer a <c>[SerializeField]</c> reference — a
    /// direct reference cannot break at all. These constants are the fallback for cases where the
    /// object is created or discovered at runtime.</para>
    /// </summary>
    public static class SceneObjectNames
    {
        // ---- Fisherman rig (children of the fisherman prefab) ----

        /// <summary>Head bone/sprite. Lower-case variant is the one most prefabs use.</summary>
        public const string FishermanHeadLower = "head";

        /// <summary>Head bone/sprite, capitalised variant found on some prefabs.</summary>
        public const string FishermanHeadUpper = "Head";

        /// <summary>Chest bone/sprite.</summary>
        public const string FishermanChest = "chest";

        /// <summary>Oar bone/sprite.</summary>
        public const string FishermanOar = "oar";

        /// <summary>Runtime-created hat overlay parented to the head.</summary>
        public const string HatCosmetic = "hat Cosmetic";

        // ---- Gameplay objects ----

        /// <summary>Golden fish, matched by substring on collision.</summary>
        public const string GoldenFish = "Golden Fish";

        /// <summary>Parallax cloud layers in the Play scene.</summary>
        public const string CloudLayerFar = "clouds_1_5";

        /// <summary>Parallax cloud layers in the Play scene.</summary>
        public const string CloudLayerNear = "clouds_1_0";

        // ---- Score table (matched by substring on child labels) ----

        /// <summary>Substring identifying a player-name label in a score row.</summary>
        public const string ScoreRowNameLabel = "Name";

        /// <summary>Substring identifying a score value label in a score row.</summary>
        public const string ScoreRowScoreLabel = "Score";

        /// <summary>Hunger/depletion label in the host lobby.</summary>
        public const string DepletionLabel = "Depletion";

        // ---- Shop panels (resolved with FindGameObjectByNames, which accepts several spellings) ----

        /// <summary>Root of the shop preview area.</summary>
        public const string ShopRoot = "Shop";

        /// <summary>Container holding the per-item cosmetic cells.</summary>
        public const string CosmeticElements = "Elements";

        /// <summary>Runtime-created padlock overlay on a locked cosmetic cell.</summary>
        public const string LockedHatSkull = "Locked Hat Skull";

        /// <summary>Safe-area wrapper inserted under every root canvas.</summary>
        public const string SafeAreaRoot = "SafeAreaRoot";
    }
}
