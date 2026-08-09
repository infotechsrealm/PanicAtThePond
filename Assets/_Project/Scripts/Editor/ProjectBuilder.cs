using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PanicAtThePond.Editor
{
    /// <summary>
    /// One-command Windows player build into the repo's <c>Build/</c> folder — the same output the
    /// team runs a second client from when testing multiplayer locally.
    /// </summary>
    /// <remarks>
    /// Exists as a compiled Editor script rather than an ad-hoc snippet because a delegate queued
    /// from a dynamically-compiled assembly (e.g. an MCP <c>script-execute</c> call) is silently
    /// dropped when that assembly is unloaded, so <c>EditorApplication.delayCall</c> never fires.
    /// </remarks>
    public static class ProjectBuilder
    {
        private const string OutputFolder = "Build";
        private const string ExecutableName = "Panic At The Pond.exe";

        /// <summary>Builds the Windows 64-bit player from the enabled scenes in Build Settings.</summary>
        [MenuItem("Panic At The Pond/Build Windows Player")]
        public static void BuildWindows64()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[ProjectBuilder] No enabled scenes in Build Settings — nothing to build.");
                return;
            }

            string outputPath = Path.Combine(
                Directory.GetCurrentDirectory(), OutputFolder, ExecutableName);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            Debug.Log($"[ProjectBuilder] Building {scenes.Length} scene(s) -> {outputPath}");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            try
            {
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;

                Debug.Log($"[ProjectBuilder] RESULT={summary.result}"
                    + $" errors={summary.totalErrors}"
                    + $" warnings={summary.totalWarnings}"
                    + $" time={summary.totalTime}"
                    + $" size={summary.totalSize / 1024 / 1024}MB"
                    + $" out={summary.outputPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProjectBuilder] Build threw: {e}");
            }
        }
    }
}
