using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PanicAtThePond.Editor
{
    /// <summary>
    /// Applies the spec's asset naming conventions via <see cref="AssetDatabase.RenameAsset"/>,
    /// which preserves GUIDs so every scene, prefab and catalog reference follows automatically.
    ///
    /// <para><b>What this does not do:</b> it never renames prefabs. Prefab names are resolved at
    /// runtime by <c>PhotonNetwork.Instantiate(prefab.name, …)</c> and several
    /// <c>Resources.Load&lt;GameObject&gt;("Fish")</c> literals, so renaming one is a
    /// networking-visible change that cannot be verified without two live clients.</para>
    ///
    /// <para>Assets referenced by string literals (animator controllers, sprites under Resources)
    /// must have those literals rewritten in the same pass — see the reported map.</para>
    /// </summary>
    public static class AssetRenamer
    {
        private struct Rule
        {
            public string Folder;
            public string Extension;
            public string Prefix;
        }

        /// <summary>Renames the asset types that carry no string-literal dependencies.</summary>
        [MenuItem("Tools/Panic At The Pond/Rename Assets - Safe Batch (audio, clips, materials)")]
        public static void RenameSafeBatch()
        {
            var rules = new List<Rule>
            {
                new Rule { Folder = "Assets/_Project", Extension = ".wav",        Prefix = "SFX_" },
                new Rule { Folder = "Assets/_Project", Extension = ".anim",       Prefix = "AC_"  },
                new Rule { Folder = "Assets/_Project", Extension = ".mat",        Prefix = "M_"   },
            };

            Run(rules, "SAFE BATCH");
        }

        /// <summary>
        /// Renames animator controllers. Their <c>Resources.Load</c> literals must be rewritten to
        /// match — the reported map lists every old→new pair for exactly that purpose.
        /// </summary>
        [MenuItem("Tools/Panic At The Pond/Rename Assets - Animator Controllers")]
        public static void RenameAnimatorControllers()
        {
            var rules = new List<Rule>
            {
                new Rule { Folder = "Assets/_Project", Extension = ".controller", Prefix = "ANIM_" },
            };

            Run(rules, "ANIMATOR CONTROLLERS");
        }

        private static void Run(List<Rule> rules, string label)
        {
            var report = new StringBuilder();
            int renamed = 0, skipped = 0, failed = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (Rule rule in rules)
                {
                    foreach (string guid in AssetDatabase.FindAssets(string.Empty, new[] { rule.Folder }))
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (!path.EndsWith(rule.Extension, System.StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // never touch the recovery snapshot
                        if (path.Contains("/_Recovery/"))
                        {
                            continue;
                        }

                        // FishControllers/* are deliberately named to match hat SPRITE names and are
                        // resolved dynamically as Resources.Load("FishControllers/" + sprite.name)
                        // (CosmeticRuntimeApplier). Prefixing them would break that coupling.
                        if (path.Contains("/FishControllers/"))
                        {
                            skipped++;
                            continue;
                        }

                        string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                        if (fileName.StartsWith(rule.Prefix, System.StringComparison.Ordinal))
                        {
                            skipped++;
                            continue;
                        }

                        string newName = rule.Prefix + Sanitize(fileName);
                        string error = AssetDatabase.RenameAsset(path, newName);
                        if (string.IsNullOrEmpty(error))
                        {
                            report.AppendLine($"  {fileName}  ->  {newName}");
                            renamed++;
                        }
                        else
                        {
                            report.AppendLine($"  FAILED {fileName}: {error}");
                            failed++;
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"PATP_RENAME [{label}] renamed={renamed} alreadyOk={skipped} failed={failed}\n{report}");
        }

        /// <summary>
        /// Strips characters that make an asset awkward to reference from a string path, while
        /// keeping the name recognisable. Spaces and brackets go; the rest is preserved so the
        /// old name stays greppable.
        /// </summary>
        private static string Sanitize(string name)
        {
            var sb = new StringBuilder(name.Length);
            bool upperNext = false;
            foreach (char c in name)
            {
                if (c == ' ' || c == '(' || c == ')' || c == '-')
                {
                    upperNext = true;
                    continue;
                }

                sb.Append(upperNext ? char.ToUpperInvariant(c) : c);
                upperNext = false;
            }

            return sb.Length == 0 ? name : sb.ToString();
        }
    }
}
