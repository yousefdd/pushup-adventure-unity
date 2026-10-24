using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PushupAdventure.EditorTools
{
    /// <summary>Builds the Android APK with IL2CPP (C# compiled to native ARM64
    /// machine code). The CI calls BuildFromCommandLine; in the editor use the
    /// "Push Up Adventure" menu.</summary>
    public static class BuildScript
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string IconPath = "Assets/Resources/Art/flame_hero.png";
        const string AppId = "com.yousefdd.pushupadventure";

        [MenuItem("Push Up Adventure/Build Android APK (IL2CPP)")]
        public static void BuildFromMenu() => Build(BuildTarget.Android, "Builds/Android/PushUpAdventure.apk", null, null, false);

        /// <summary>A browser build of the same game (WebGL also compiles
        /// through IL2CPP), used to preview and record it without a phone.</summary>
        public static void BuildWebGLFromCommandLine()
        {
            var args = Args();
            string path;
            args.TryGetValue("customBuildPath", out path);
            if (string.IsNullOrEmpty(path)) path = "build/WebGL/PushUpAdventure";
            Build(BuildTarget.WebGL, path, null, null, true);
        }

        [MenuItem("Push Up Adventure/Prepare Project")]
        public static void PrepareFromMenu()
        {
            EnsureScene();
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
        }

        /// <summary>game-ci passes -customBuildPath, -buildVersion and
        /// -androidVersionCode; anything missing gets a sensible default.</summary>
        public static void BuildFromCommandLine()
        {
            var args = Args();
            string path, version, code;
            args.TryGetValue("customBuildPath", out path);
            args.TryGetValue("buildVersion", out version);
            args.TryGetValue("androidVersionCode", out code);
            if (string.IsNullOrEmpty(path)) path = "build/Android/PushUpAdventure.apk";
            if (Directory.Exists(path) || !Path.HasExtension(path)) path = Path.Combine(path, "PushUpAdventure.apk");
            Build(BuildTarget.Android, path, version, code, true);
        }

        static void Build(BuildTarget target, string path, string version, string code, bool exit)
        {
            try
            {
                EnsureScene();
                ConfigurePlayer();
                if (!string.IsNullOrEmpty(version) && version != "none") PlayerSettings.bundleVersion = version;
                int vc;
                if (int.TryParse(code, out vc) && vc > 0) PlayerSettings.Android.bundleVersionCode = vc;
                var group = target == BuildTarget.WebGL ? BuildTargetGroup.WebGL : BuildTargetGroup.Android;
                if (target == BuildTarget.Android)
                    EditorUserBuildSettings.buildAppBundle = path.EndsWith(".aab", StringComparison.OrdinalIgnoreCase);
                else
                    // Plain files, so any static server can host the preview.
                    PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = path,
                    target = target,
                    targetGroup = group,
                    options = BuildOptions.None,
                });
                var s = report.summary;
                var line = string.Format("Build {0}: {1} bytes, {2} errors, {3} warnings, {4:0.0}s → {5}",
                    s.result, s.totalSize, s.totalErrors, s.totalWarnings, s.totalTime.TotalSeconds, path);
                Debug.Log(line);
                WriteReport(line, report);
                if (exit) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 101);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                WriteReport("Build threw: " + e, null);
                if (exit) EditorApplication.Exit(102);
                else throw;
            }
        }

        /// <summary>build/build-report.txt: the result and every error and
        /// warning, for CI to keep when the editor log is out of reach.</summary>
        static void WriteReport(string head, BuildReport report)
        {
            try
            {
                var lines = new List<string> { head };
                if (report != null)
                    foreach (var step in report.steps)
                    foreach (var m in step.messages)
                        if (m.type != LogType.Log)
                            lines.Add(string.Format("[{0}] {1}: {2}", m.type, step.name, m.content));
                Directory.CreateDirectory("build");
                File.WriteAllLines("build/build-report.txt", lines);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>The one scene: empty. GameApp starts itself when it loads
        /// and builds the whole interface in code.</summary>
        static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "yousefdd";
            PlayerSettings.productName = "Push Up Adventure";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, AppId);
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "0.1")
                PlayerSettings.bundleVersion = "1.0.0";

            // IL2CPP: the C# is turned into C++ and compiled to native ARM64 code.
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Low);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.Android.startInFullscreen = true;
            PlayerSettings.Android.renderOutsideSafeArea = false;
            PlayerSettings.SplashScreen.backgroundColor = new Color32(0x1F, 0x1F, 0x29, 0xFF);
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null) PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });

            UseClassicInput();
            AssetDatabase.SaveAssets();
        }

        /// <summary>The game reads keys through UnityEngine.Input: make sure the
        /// project uses the classic input manager.</summary>
        static void UseClassicInput()
        {
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").FirstOrDefault();
            if (settings == null) return;
            var so = new SerializedObject(settings);
            var p = so.FindProperty("activeInputHandler");
            if (p == null || p.intValue == 0) return;
            p.intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Dictionary<string, string> Args()
        {
            var o = new Dictionary<string, string>();
            var a = Environment.GetCommandLineArgs();
            for (var i = 0; i < a.Length; i++)
            {
                if (!a[i].StartsWith("-")) continue;
                var key = a[i].TrimStart('-');
                var value = i + 1 < a.Length && !a[i + 1].StartsWith("-") ? a[i + 1] : "";
                o[key] = value;
            }
            return o;
        }
    }
}
