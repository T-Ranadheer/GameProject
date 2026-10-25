#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PrototypeBuildValidation
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Prototype/Validate Phase 1")]
    public static void ValidateProject()
    {
        ValidateProjectInternal("Builds/Validation/phase1-validation.txt", "Phase 1");
    }

    public static void ValidatePhase3Project()
    {
        ValidateProjectInternal("Builds/Validation/phase3-validation.txt", "Phase 3");
    }

    public static void ValidatePhase4Project()
    {
        ValidateProjectInternal("Builds/Validation/phase4-validation.txt", "Phase 4");
    }

    public static void ValidatePhase5Project()
    {
        ValidateProjectInternal("Builds/Validation/phase5-validation.txt", "Phase 5");
    }

    public static void BuildWindowsPhase3()
    {
        ValidatePhase3Project();
        Directory.CreateDirectory("Builds/Windows/Phase3");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Windows/Phase3/NovaHeroPrototype.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        WriteBuildReport("Builds/Validation/phase3-windows-build.txt", report);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException("Phase 3 Windows development build failed: " + report.summary.result);
        }
    }

    public static void BuildAndroidPhase3()
    {
        ValidatePhase3Project();
        Directory.CreateDirectory("Builds/Android/Phase3");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Android/Phase3/NovaHeroPrototype.apk",
            target = BuildTarget.Android,
            options = BuildOptions.Development
        });
        WriteBuildReport("Builds/Validation/phase3-android-build.txt", report);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException("Phase 3 Android development build failed: " + report.summary.result);
        }
    }

    public static void BuildWindowsPhase4()
    {
        ValidatePhase4Project();
        Directory.CreateDirectory("Builds/Windows/Phase4");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Windows/Phase4/NovaHeroPrototype.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        WriteBuildReport("Builds/Validation/phase4-windows-build.txt", report);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException("Phase 4 Windows development build failed: " + report.summary.result);
        }
    }

    public static void BuildAndroidPhase4()
    {
        ValidatePhase4Project();
        Directory.CreateDirectory("Builds/Android/Phase4");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Android/Phase4/NovaHeroPrototype.apk",
            target = BuildTarget.Android,
            options = BuildOptions.Development
        });
        WriteBuildReport("Builds/Validation/phase4-android-build.txt", report);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException("Phase 4 Android development build failed: " + report.summary.result);
        }
    }

    public static void BuildWindowsPhase5()
    {
        ValidatePhase5Project();
        Directory.CreateDirectory("Builds/Windows/Phase5");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Windows/Phase5/NovaHeroPrototype.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        WriteBuildReport("Builds/Validation/phase5-windows-build.txt", report);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException("Phase 5 Windows development build failed: " + report.summary.result);
        }
    }

    public static void BuildAndroidPhase5()
    {
        ValidatePhase5Project();
        Directory.CreateDirectory("Builds/Android/Phase5");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Android/Phase5/NovaHeroPrototype.apk",
            target = BuildTarget.Android,
            options = BuildOptions.Development
        });
        WriteBuildReport("Builds/Validation/phase5-android-build.txt", report);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException("Phase 5 Android development build failed: " + report.summary.result);
        }
    }

    private static void ValidateProjectInternal(string validationPath, string phaseName)
    {
        Directory.CreateDirectory("Builds/Validation");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var bootstrap = Object.FindObjectOfType<PrototypeBootstrap>();
        if (bootstrap == null)
        {
            throw new BuildFailedException(phaseName + " validation failed: PrototypeBootstrap is missing from " + ScenePath);
        }

        if (EditorBuildSettings.scenes.Length == 0 || EditorBuildSettings.scenes[0].path != ScenePath || !EditorBuildSettings.scenes[0].enabled)
        {
            throw new BuildFailedException(phaseName + " validation failed: SampleScene is not the first enabled build scene.");
        }

        File.WriteAllText(validationPath, "PASS\nPhase: " + phaseName + "\nScene: " + scene.path + "\nBootstrap: " + bootstrap.name + "\n");
        Debug.Log(phaseName + " scene validation passed.");
    }

    public static void BuildWindows()
    {
        ValidateProject();
        Directory.CreateDirectory("Builds/Windows");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Windows/NovaHeroPrototype.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        WriteBuildReport("Builds/Validation/windows-build.txt", report);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException("Windows development build failed: " + report.summary.result);
        }
    }

    public static void BuildAndroid()
    {
        ValidateProject();
        Directory.CreateDirectory("Builds/Android");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Android/NovaHeroPrototype.apk",
            target = BuildTarget.Android,
            options = BuildOptions.Development
        });
        WriteBuildReport("Builds/Validation/android-build.txt", report);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException("Android development build failed: " + report.summary.result);
        }
    }

    private static void WriteBuildReport(string path, BuildReport report)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, "Result: " + report.summary.result + "\nWarnings: " + report.summary.totalWarnings + "\nErrors: " + report.summary.totalErrors + "\nSize: " + report.summary.totalSize + "\n");
    }
}
#endif
