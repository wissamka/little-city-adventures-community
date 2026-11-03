using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LittleCity.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/LittleCity/Scenes/LittleCity.unity";
        const string Marker = "ProjectSettings/LittleCitySetup.json";
        static ProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(Marker)) return;
                Configure();
                File.WriteAllText(Marker, "{\"setupVersion\":1}\n");
                if (!Application.isBatchMode) EditorSceneManager.OpenScene(ScenePath);
            };
        }

        [MenuItem("Little City/Configure iPad project")]
        public static void Configure()
        {
            PlayerSettings.companyName = "Little City";
            PlayerSettings.productName = "Little City Adventures Community";
            PlayerSettings.bundleVersion = "0.9.16";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "org.publicgames.littlecity.community");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.iOS, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPadOnly;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultScreenWidth = 1366;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.runInBackground = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });
            GraphicsSettings.defaultRenderPipeline = null;
            QualitySettings.renderPipeline = null;
            QualitySettings.antiAliasing = 2;
            QualitySettings.shadowDistance = 75;
            QualitySettings.pixelLightCount = 1;
            // Legacy Input handles both keyboard and EventSystem multitouch, with no extra input package.
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var activeInput = settings.FindProperty("activeInputHandler");
            if (activeInput != null) activeInput.intValue = 0;
            var timing = settings.FindProperty("enableFrameTimingStats");
            if (timing != null) timing.boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
            if (!File.Exists(ScenePath)) CreateScene();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Little City configured for landscape iPad, Metal and IL2CPP.");
        }

        [MenuItem("Little City/Open and play")]
        public static void Play()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
            EditorApplication.delayCall += () =>
            {
                var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView") ?? Type.GetType("UnityEditor.GameView,UnityEditor");
                if (type == null) return;
                var view = EditorWindow.GetWindow(type); view.maximized = true; view.Focus();
            };
        }

        [MenuItem("Little City/Recreate bootstrap scene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var game = new GameObject("Little City").AddComponent<CityGame>();
            game.sunshineShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/LittleCity/Shaders/Sunshine.shader");
            game.glassShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/LittleCity/Shaders/Glass.shader");
            game.worldTextShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/LittleCity/Shaders/WorldText.shader");
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        [MenuItem("Little City/Export iPad Xcode project")]
        public static void BuildIOS()
        {
            Configure();
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
                throw new InvalidOperationException("Install iOS Build Support for this Unity Editor, then export again.");
            Directory.CreateDirectory("Builds");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, target = BuildTarget.iOS,
                locationPathName = "Builds/iOS", options = Directory.Exists("Builds/iOS/Unity-iPhone.xcodeproj")
                    ? BuildOptions.AcceptExternalModificationsToPlayer : BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("iPad export failed: " + report.summary.result + ", " + report.summary.totalErrors + " errors. See the Editor log.");
            Debug.Log("IPAD_EXPORT_SUCCEEDED " + Path.GetFullPath("Builds/iOS"));
        }

        [MenuItem("Little City/Build Mac app")]
        public static void BuildMac()
        {
            Configure(); CreateScene(); PlayerSettings.productName = "Little City Adventures Community";
            PlayerSettings.bundleVersion = "0.9.16"; PlayerSettings.macRetinaSupport = true;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,"org.publicgames.littlecity.community.mac");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, 1);
            PlayerSettings.defaultScreenWidth = 1366; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            Directory.CreateDirectory("Builds/Mac");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, target = BuildTarget.StandaloneOSX,
                locationPathName = "Builds/Mac/Little City Adventures Community.app", options = BuildOptions.CleanBuildCache
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Mac build failed: " + report.summary.result + ", " + report.summary.totalErrors + " errors.");
            Debug.Log("MAC_BUILD_SUCCEEDED " + Path.GetFullPath("Builds/Mac/Little City Adventures Community.app"));
        }

        [MenuItem("Little City/Capture game screenshot")]
        public static void CaptureScreenshot()
        {
            if (!EditorApplication.isPlaying) { Debug.LogWarning("Start the game before capturing a screenshot."); return; }
            Directory.CreateDirectory("Documentation/Screenshots");
            string path = Path.GetFullPath("Documentation/Screenshots/LittleCity-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png");
            ScreenCapture.CaptureScreenshot(path, 1); Debug.Log("Game screenshot: " + path);
        }
    }
}
