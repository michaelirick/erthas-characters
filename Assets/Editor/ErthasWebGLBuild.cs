using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public static class ErthasWebGLBuild
{
    private const string CreatorScene = "Assets/BoZo_StylizedModularCharacters/Scenes/StylizedCharacterCreator.unity";

    public static void Build()
    {
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(CreatorScene, true) };
        // Next's static public directory does not attach per-file Content-Encoding
        // headers, so emit plain files rather than Unity's .unityweb fallback format.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.dataCaching = true;

        var output = System.Environment.GetEnvironmentVariable("ERTHAS_WEBGL_OUTPUT");
        if (string.IsNullOrWhiteSpace(output)) output = Path.Combine(Directory.GetCurrentDirectory(), "Build/character-unity");

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { CreatorScene },
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        });

        Debug.Log($"Erthas WebGL build result: {report.summary.result}; output: {report.summary.outputPath}");
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception($"WebGL build failed: {report.summary.result}");
    }
}
