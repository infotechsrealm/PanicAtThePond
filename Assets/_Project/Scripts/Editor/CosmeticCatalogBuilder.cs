using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

using PanicAtThePond.Shop;

namespace PanicAtThePond.Editor
{
    /// <summary>
    /// Creates and refreshes <see cref="CosmeticCatalog"/> from the assets already in the project,
    /// so the catalog never has to be typed out by hand.
    ///
    /// <para>Run <b>Tools ▸ Panic At The Pond ▸ Rebuild Cosmetic Catalog</b> after adding art.
    /// Rebuilding preserves any entry whose <c>icon</c> or <c>animator</c> you overrode manually —
    /// it only fills in blanks and appends newly-found assets.</para>
    /// </summary>
    public static class CosmeticCatalogBuilder
    {
        private const string CatalogAssetPath = "Assets/_Project/Resources/SO_CosmeticCatalog.asset";
        private const string ShopSpritesFolder = "Assets/_Project/Resources/ShopUI";
        private const string FishermanControllersFolder = "Assets/_Project/Resources/FishermanControllers";
        private const string FishControllersFolder = "Assets/_Project/Resources/FishControllers";

        [MenuItem("Tools/Panic At The Pond/Rebuild Cosmetic Catalog")]
        public static void Rebuild()
        {
            CosmeticCatalog catalog = AssetDatabase.LoadAssetAtPath<CosmeticCatalog>(CatalogAssetPath);
            if (catalog == null)
            {
                string directory = Path.GetDirectoryName(CatalogAssetPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    AssetDatabase.Refresh();
                }

                catalog = ScriptableObject.CreateInstance<CosmeticCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            }

            SerializedObject so = new SerializedObject(catalog);
            SerializedProperty entries = so.FindProperty("_entries");

            // index existing entries by id so manual overrides survive a rebuild
            Dictionary<string, int> existing = new Dictionary<string, int>();
            for (int i = 0; i < entries.arraySize; i++)
            {
                string id = entries.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue;
                if (!string.IsNullOrEmpty(id) && !existing.ContainsKey(id))
                {
                    existing[id] = i;
                }
            }

            int added = 0, filled = 0, kept = 0;

            foreach (Sprite sprite in LoadAll<Sprite>(ShopSpritesFolder))
            {
                if (sprite == null)
                {
                    continue;
                }

                Upsert(entries, existing, sprite.name, CosmeticCategory.UiIcon, sprite, null,
                    ref added, ref filled, ref kept);
            }

            foreach (RuntimeAnimatorController controller in LoadAll<RuntimeAnimatorController>(FishermanControllersFolder))
            {
                if (controller == null)
                {
                    continue;
                }

                Upsert(entries, existing, controller.name, CosmeticCategory.FishermanHat, null, controller,
                    ref added, ref filled, ref kept);
            }

            foreach (RuntimeAnimatorController controller in LoadAll<RuntimeAnimatorController>(FishControllersFolder))
            {
                if (controller == null)
                {
                    continue;
                }

                Upsert(entries, existing, controller.name, CosmeticCategory.FishSpecies, null, controller,
                    ref added, ref filled, ref kept);
            }

            so.ApplyModifiedProperties();
            catalog.Invalidate();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CosmeticCatalogBuilder] {CatalogAssetPath}: {entries.arraySize} entries " +
                      $"(added {added}, filled {filled}, preserved {kept} manual override(s)).");
        }

        /// <summary>Adds a new entry, or fills only the blank fields of an existing one.</summary>
        private static void Upsert(SerializedProperty entries, Dictionary<string, int> index, string id,
            CosmeticCategory category, Sprite icon, RuntimeAnimatorController animator,
            ref int added, ref int filled, ref int kept)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            if (index.TryGetValue(id, out int i))
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                SerializedProperty iconProp = entry.FindPropertyRelative("icon");
                SerializedProperty animProp = entry.FindPropertyRelative("animator");

                bool changed = false;
                if (icon != null && iconProp.objectReferenceValue == null) { iconProp.objectReferenceValue = icon; changed = true; }
                if (animator != null && animProp.objectReferenceValue == null) { animProp.objectReferenceValue = animator; changed = true; }

                if (changed) { filled++; } else { kept++; }
                return;
            }

            entries.arraySize++;
            SerializedProperty created = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            created.FindPropertyRelative("id").stringValue = id;
            created.FindPropertyRelative("displayName").stringValue = Prettify(id);
            created.FindPropertyRelative("category").enumValueIndex = (int)category;
            created.FindPropertyRelative("icon").objectReferenceValue = icon;
            created.FindPropertyRelative("animator").objectReferenceValue = animator;

            // Sprite sheets slice to names like "TurtleHat_0", but shop_config.json and the network
            // payloads use the un-suffixed "TurtleHat". Register the stripped form as an alias so
            // the catalog answers both without anyone having to keep the two spellings in sync.
            SerializedProperty aliases = created.FindPropertyRelative("aliases");
            string stripped = StripFrameSuffix(id);
            if (stripped != id)
            {
                aliases.arraySize = 1;
                aliases.GetArrayElementAtIndex(0).stringValue = stripped;
            }
            else
            {
                aliases.arraySize = 0;
            }

            index[id] = entries.arraySize - 1;
            added++;
        }

        private static IEnumerable<T> LoadAll<T>(string folder) where T : Object
        {
            List<T> results = new List<T>();
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogWarning($"[CosmeticCatalogBuilder] folder not found, skipping: {folder}");
                return results;
            }

            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is T typed)
                    {
                        results.Add(typed);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Removes a trailing sprite-sheet frame index ("TurtleHat_0" → "TurtleHat"). Returns the
        /// input unchanged when there is no such suffix.
        /// </summary>
        private static string StripFrameSuffix(string id)
        {
            int underscore = id.LastIndexOf('_');
            if (underscore <= 0 || underscore == id.Length - 1)
            {
                return id;
            }

            for (int i = underscore + 1; i < id.Length; i++)
            {
                if (!char.IsDigit(id[i]))
                {
                    return id;
                }
            }

            return id.Substring(0, underscore);
        }

        /// <summary>Turns a raw asset name into something readable for UI.</summary>
        private static string Prettify(string id)
        {
            return id.Replace('_', ' ').Replace('-', ' ').Trim();
        }
    }
}
