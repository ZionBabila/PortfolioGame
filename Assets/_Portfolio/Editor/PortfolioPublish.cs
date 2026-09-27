using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Portfolio.EditorTools
{
    /// <summary>
    /// One-click publishing from the Unity menu:
    ///   Portfolio → Publish To Website       build WebGL, then upload it (deploy.ps1 → gh-pages → GitHub Pages)
    ///   Portfolio → Upload Last Build         upload Builds/WebGL without rebuilding
    ///   Portfolio → Test Build Locally        serve Builds/WebGL on http://localhost:8080 and open it
    ///   Portfolio → Open Website
    /// The upload runs in the background with a progress bar; the Editor stays usable.
    /// </summary>
    public static class PortfolioPublish
    {
        const string SiteUrl = "https://zionbabila.github.io/PortfolioGame/";
        const string LocalUrl = "http://localhost:8080";

        static Process deploy;
        static readonly StringBuilder deployLog = new();
        static double deployStarted;

        static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        [MenuItem("Portfolio/Publish To Website (Build + Upload)", priority = 100)]
        static void BuildAndPublish()
        {
            if (!CanStart()) return;
            if (!EditorUtility.DisplayDialog("Publish To Website",
                    $"Build the game and upload it to:\n{SiteUrl}\n\nThe site is public. The build takes a few minutes, the upload about a minute.",
                    "Build + Upload", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (!WebBuild.Build())
            {
                EditorUtility.DisplayDialog("Publish To Website", "The build failed. See the Console for the errors.", "OK");
                return;
            }
            StartUpload();
        }

        [MenuItem("Portfolio/Upload Last Build", priority = 101)]
        static void UploadOnly()
        {
            if (!CanStart()) return;
            if (!File.Exists(Path.Combine(ProjectRoot, "Builds/WebGL/index.html")))
            {
                EditorUtility.DisplayDialog("Upload Last Build", "There is no build yet. Use Portfolio → Publish To Website (Build + Upload).", "OK");
                return;
            }
            var built = File.GetLastWriteTime(Path.Combine(ProjectRoot, "Builds/WebGL/index.html"));
            if (!EditorUtility.DisplayDialog("Upload Last Build",
                    $"Upload the build from {built:dd/MM HH:mm} to:\n{SiteUrl}", "Upload", "Cancel"))
                return;
            StartUpload();
        }

        [MenuItem("Portfolio/Test Build Locally", priority = 102)]
        static void TestLocally()
        {
            var build = Path.Combine(ProjectRoot, "Builds", "WebGL");
            if (!File.Exists(Path.Combine(build, "index.html")))
            {
                EditorUtility.DisplayDialog("Test Build Locally", "There is no build yet. Use Portfolio → Build WebGL first.", "OK");
                return;
            }
            // A visible console window runs the local web server; close it to stop the server.
            Process.Start(new ProcessStartInfo("cmd.exe", $"/k title Portfolio local server - close to stop && npx -y http-server \"{build}\" -p 8080 -c-1")
            {
                UseShellExecute = true,
                WorkingDirectory = ProjectRoot,
            });
            EditorApplication.delayCall += () => DelayThen(4, () => Application.OpenURL(LocalUrl));
        }

        [MenuItem("Portfolio/Open Website", priority = 103)]
        static void OpenSite() => Application.OpenURL(SiteUrl);

        // ---------- upload ----------

        static bool CanStart()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Portfolio", "Stop Play mode first.", "OK");
                return false;
            }
            if (deploy != null && !deploy.HasExited)
            {
                EditorUtility.DisplayDialog("Portfolio", "An upload is already running.", "OK");
                return false;
            }
            return true;
        }

        static void StartUpload()
        {
            deployLog.Clear();
            var psi = new ProcessStartInfo(FindPowerShell(),
                $"-NoProfile -ExecutionPolicy Bypass -File \"{Path.Combine(ProjectRoot, "deploy.ps1")}\" -SkipBuild")
            {
                WorkingDirectory = ProjectRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            try
            {
                deploy = Process.Start(psi);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Upload", $"Couldn't start PowerShell 7 (pwsh): {e.Message}\n\nInstall it from the Microsoft Store (\"PowerShell\"), then try again.", "OK");
                return;
            }
            deploy.OutputDataReceived += (_, e) => { if (e.Data != null) lock (deployLog) deployLog.AppendLine(e.Data); };
            deploy.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (deployLog) deployLog.AppendLine(e.Data); };
            deploy.BeginOutputReadLine();
            deploy.BeginErrorReadLine();
            deployStarted = EditorApplication.timeSinceStartup;
            EditorApplication.update += PollUpload;
        }

        static void PollUpload()
        {
            if (deploy == null) { EditorApplication.update -= PollUpload; return; }
            if (!deploy.HasExited)
            {
                var elapsed = EditorApplication.timeSinceStartup - deployStarted;
                EditorUtility.DisplayProgressBar("Uploading to the website", $"Pushing the build to GitHub Pages… ({elapsed:0}s)",
                    Mathf.Clamp01((float)(elapsed / 60.0)));
                return;
            }

            EditorApplication.update -= PollUpload;
            EditorUtility.ClearProgressBar();
            string log;
            lock (deployLog) log = deployLog.ToString();
            int code = deploy.ExitCode;
            deploy.Dispose();
            deploy = null;

            bool pushed = log.Contains("-> gh-pages") || log.Contains("Everything up-to-date") || log.Contains("nothing to commit");
            if (code == 0 && pushed)
            {
                Debug.Log($"[Portfolio] Uploaded.\n{log}");
                if (EditorUtility.DisplayDialog("Published",
                        $"The build is uploaded. GitHub Pages updates the site within a minute or two:\n{SiteUrl}\n\nIf you still see the old version, press Ctrl+F5 in the browser.",
                        "Open site", "Close"))
                    Application.OpenURL(SiteUrl);
            }
            else
            {
                Debug.LogError($"[Portfolio] Upload failed (exit {code}):\n{log}");
                EditorUtility.DisplayDialog("Upload failed",
                    "The upload didn't finish. The details are in the Console (search for [Portfolio]).\n\nCommon causes: no internet, or GitHub login expired (open GitHub Desktop and sign in).", "OK");
            }
        }

        static string FindPowerShell()
        {
            foreach (var candidate in new[]
                     {
                         Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe"),
                         Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WindowsApps", "pwsh.exe"),
                     })
                if (File.Exists(candidate)) return candidate;
            return "pwsh";
        }

        static void DelayThen(double seconds, Action action)
        {
            var until = EditorApplication.timeSinceStartup + seconds;
            void Tick()
            {
                if (EditorApplication.timeSinceStartup < until) return;
                EditorApplication.update -= Tick;
                action();
            }
            EditorApplication.update += Tick;
        }
    }
}
