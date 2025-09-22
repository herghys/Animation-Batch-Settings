#if UNITY_EDITOR
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using UnityEditor;

using UnityEngine;

namespace Herghys.AnimationBatchClipHelper.Updater
{
    public static class PackageUpdater
    {
        private static PackageJsonData packageData;

        static PackageUpdater()
        {
            LoadPackageJson();
            EditorApplication.update += RunOnce;
        }

        private static void RunOnce()
        {
            EditorApplication.update -= RunOnce;
            _ = CheckForUpdates(true);
        }

        [MenuItem("Tools/Herghys/Check for Update", false, 1001)]
        private static void ManualCheck()
        {
            _ = CheckForUpdates(false);
        }

        /// <summary>
        /// Load Package JSON
        /// </summary>
        private static void LoadPackageJson()
        {
            packageData = null;

            // 1. Try UPM path first
            string upmPath = Path.Combine("Packages", "com.herghys.animationbatchhelper", "package.json");
            if (File.Exists(upmPath))
            {
                string json = File.ReadAllText(upmPath);
                packageData = JsonUtility.FromJson<PackageJsonData>(json);
                return;
            }

            // 2. Fallback: Search in Assets
            string[] guids = AssetDatabase.FindAssets("package t:TextAsset", new[] { "Assets" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("package.json"))
                {
                    var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    if (textAsset != null)
                    {
                        packageData = JsonUtility.FromJson<PackageJsonData>(textAsset.text);
                        return;
                    }
                }
            }

            if (packageData == null)
            {
                Debug.LogWarning("[PackageUpdater] package.json not found in Packages/ or Assets/");
                packageData = new PackageJsonData(); // empty fallback
            }
        }

        /// <summary>
        /// Check for updates
        /// </summary>
        /// <param name="silent"></param>
        /// <returns></returns>
        private static async Task CheckForUpdates(bool silent)
        {
            if (string.IsNullOrEmpty(packageData.repositoryUrl))
            {
                if (!silent)
                    EditorUtility.DisplayDialog("Update Check", "PackageRepository URL not found in package.json", "OK");
                return;
            }

            // Extract repo owner + name from GitHub URL
            var match = Regex.Match(packageData.repositoryUrl, @"github\.com/([^/]+)/([^/.]+)");
            if (!match.Success)
            {
                if (!silent)
                    EditorUtility.DisplayDialog("Update Check", "Invalid repository URL", "OK");
                return;
            }

            string owner = match.Groups[1].Value;
            string repo = match.Groups[2].Value;

            string latestVersion = await GetLatestReleaseTag(owner, repo);
            if (string.IsNullOrEmpty(latestVersion))
            {
                if (!silent)
                    EditorUtility.DisplayDialog("Update Check", "Could not fetch latest version.", "OK");
                return;
            }

            if (IsNewerVersion(latestVersion, packageData.version))
            {
                if (EditorUtility.DisplayDialog(
                    "Update Available",
                    $"A new version of {packageData.displayName} is available!\n\n" +
                    $"Current: {packageData.version}\nLatest: {latestVersion}\n\n" +
                    "Do you want to open the GitHub releases page?",
                    "Update Now", "Later"))
                {
                    Application.OpenURL($"https://github.com/{owner}/{repo}/releases");
                }
            }
            else if (!silent)
            {
                EditorUtility.DisplayDialog("Update Check", $"{packageData.displayName} is up to date (v{packageData.version}).", "OK");
            }
        }

        /// <summary>
        /// Get Latest Release
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="repo"></param>
        /// <returns></returns>
        private static async Task<string> GetLatestReleaseTag(string owner, string repo)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "UnityEditor");
                    string url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
                    var response = await client.GetStringAsync(url);

                    var match = Regex.Match(response, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                    if (match.Success)
                        return match.Groups[1].Value.TrimStart('v');
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PackageUpdater] Update check failed: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Newwer Version Check
        /// </summary>
        /// <param name="latest"></param>
        /// <param name="current"></param>
        /// <returns></returns>
        private static bool IsNewerVersion(string latest, string current)
        {
            if (System.Version.TryParse(latest, out var latestVer) &&
                System.Version.TryParse(current, out var currentVer))
            {
                return latestVer > currentVer;
            }
            return false;
        }
    }
}
#endif