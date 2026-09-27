using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// One WebGL build that serves both 16:9 (desktop) and 9:16 (phones): the Responsive template
    /// fills the browser window and the game adapts its camera + UI to the aspect ratio at runtime.
    /// CLI: unity run . -- -executeMethod Portfolio.EditorTools.WebBuild.BuildCli
    /// </summary>
    public static class WebBuild
    {
        public const string OutputDir = "Builds/WebGL";

        [MenuItem("Portfolio/Build WebGL")]
        public static void BuildMenu() => Build();

        public static void BuildCli()
        {
            var ok = Build();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Build()
        {
            if (EditorBuildSettings.scenes.Length == 0) PortfolioSceneBuilder.Build();

            PlayerSettings.productName = "Portfolio";
            PlayerSettings.WebGL.template = "PROJECT:Responsive";
            // GitHub Pages can't send Content-Encoding headers, so let the loader decompress in JS.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.runInBackground = false;
            // No "Made with Unity" intro: straight into the workshop (allowed on Personal since Unity 6).
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            if (Directory.Exists(OutputDir)) Directory.Delete(OutputDir, true);

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = OutputDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var ok = report.summary.result == BuildResult.Succeeded;
            Debug.Log($"[Portfolio] WebGL build {report.summary.result}: {report.summary.totalSize / (1024f * 1024f):F1} MB -> {OutputDir}");
            return ok;
        }
    }
}
