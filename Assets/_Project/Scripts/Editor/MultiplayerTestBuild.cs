using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PanicAtThePond.Editor
{
    /// <summary>
    /// Produces the pair of standalone players used to test a real two-player match on one machine.
    /// </summary>
    /// <remarks>
    /// <para><b>Why two builds rather than two copies of one build.</b> Unity keys PlayerPrefs on
    /// <c>HKCU\Software\&lt;company&gt;\&lt;product&gt;</c>. Two instances of the same executable
    /// therefore share one save: the same coins, the same unlocks and — the reason this matters —
    /// the same selected cosmetics. A bug of the form "the hat shows on the other player but not on
    /// me" cannot even be expressed when both players read the same selection. Giving each build its
    /// own <c>productName</c> gives each its own registry hive, so they are genuinely two players.</para>
    ///
    /// <para><b>Why not a duplicated project.</b> A second copy of the project costs ~5 GB of
    /// <c>Library</c> and a full reimport, needs a second Editor process, and still shares
    /// PlayerPrefs — the Editor keys them on company/product too. It buys nothing over two builds.</para>
    /// </remarks>
    public static class MultiplayerTestBuild
    {
        public const string HostProductName = "PATP TestHost";
        public const string ClientProductName = "PATP TestClient";

        // Deliberately NOT under Build/: something outside the project packages that folder into
        // the tracked Build.zip, and test players sitting there inflated it from 80 MB to 315 MB.
        private const string HostDirectory = "TestBuilds/Host";
        private const string ClientDirectory = "TestBuilds/Client";
        private const string ExecutableName = "PanicAtThePond.exe";

        public static string HostExecutable => Path.GetFullPath(Path.Combine(HostDirectory, ExecutableName));
        public static string ClientExecutable => Path.GetFullPath(Path.Combine(ClientDirectory, ExecutableName));

        [MenuItem("Panic At The Pond/Build Two Test Players")]
        public static void BuildBoth()
        {
            Build(true, true);
        }

        /// <summary>
        /// Builds either or both test players. Returns true when every requested build succeeded.
        /// </summary>
        public static bool Build(bool host, bool client)
        {
            // Two windowed players on one desktop means one of them is always unfocused. Without
            // this the unfocused player stops running its player loop entirely: it stops sending,
            // stops receiving, and eventually times out of the room. Every two-instance test would
            // measure that instead of the thing under test.
            if (!PlayerSettings.runInBackground)
            {
                PlayerSettings.runInBackground = true;
                Debug.Log("[MultiplayerTestBuild] Enabled PlayerSettings.runInBackground.");
            }

            string[] scenes = EnabledScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("[MultiplayerTestBuild] No enabled scenes in Build Settings.");
                return false;
            }

            string originalProduct = PlayerSettings.productName;
            bool allSucceeded = true;

            try
            {
                if (host)
                {
                    allSucceeded &= BuildOne(scenes, HostProductName, HostDirectory);
                }

                if (client)
                {
                    allSucceeded &= BuildOne(scenes, ClientProductName, ClientDirectory);
                }
            }
            finally
            {
                // Restore before anything else can save the project with a test name baked in.
                PlayerSettings.productName = originalProduct;
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[MultiplayerTestBuild] Done. success={allSucceeded} "
                + $"productName restored to '{PlayerSettings.productName}'.");
            return allSucceeded;
        }

        private static bool BuildOne(string[] scenes, string productName, string directory)
        {
            PlayerSettings.productName = productName;

            Directory.CreateDirectory(directory);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(directory, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                // Development build so Debug.Log survives into the player log, which is the only
                // window this harness has into what each side actually did.
                options = BuildOptions.Development | BuildOptions.AllowDebugging,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            bool ok = summary.result == BuildResult.Succeeded;

            Debug.Log($"[MultiplayerTestBuild] '{productName}' -> {directory}: {summary.result}"
                + $" ({summary.totalSize / (1024 * 1024)} MB, {summary.totalTime.TotalSeconds:0} s,"
                + $" {summary.totalErrors} error(s))");
            return ok;
        }

        private static string[] EnabledScenes()
        {
            var list = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && !string.IsNullOrEmpty(scene.path))
                {
                    list.Add(scene.path);
                }
            }

            return list.ToArray();
        }
    }
}
