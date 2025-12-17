using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic; 
using SwiftlyS2.Core;

namespace Speedometer
{
    public static class DiscordWebhook
    {
        private static DateTime _lastWebhookTime = DateTime.MinValue;

        public static void StartRecording() { }
        public static void StopRecording() { }
        public static string GetCurrentDemoFile() { return ""; }
        public static Task CheckAndCleanOldDemo(string s) { return Task.CompletedTask; }

        public static async Task SendDiscordWebhook(string playerName, string mapName, int speed)
        {
            if ((DateTime.Now - _lastWebhookTime).TotalSeconds < 15) return;
            _lastWebhookTime = DateTime.Now;

            string webhookUrl = Speedometer.Config.DiscordWebhookUrl;
            if (string.IsNullOrEmpty(webhookUrl)) return;

            try
            {
                string jsonPath = "";
                var searchPaths = new List<string>();

                try {
                    string assemblyLoc = Assembly.GetExecutingAssembly().Location;
                    if (!string.IsNullOrEmpty(assemblyLoc)) {
                        string dir = Path.GetDirectoryName(assemblyLoc) ?? "";
                        if (!string.IsNullOrEmpty(dir)) {
                            searchPaths.Add(Path.Combine(dir, "resources", "payload.json"));
                            searchPaths.Add(Path.Combine(dir, "..", "resources", "payload.json"));
                        }
                    }
                } catch { }

                string currentDir = Directory.GetCurrentDirectory(); 
                string csgoRoot = currentDir; 
                if(currentDir.Contains("bin")) {
                    csgoRoot = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "csgo"));
                } else if (!currentDir.EndsWith("csgo")) {
                    csgoRoot = Path.Combine(currentDir, "csgo");
                }

                searchPaths.Add(Path.Combine(csgoRoot, "addons", "swiftlys2", "plugins", "Extended-Speedmeter", "resources", "payload.json"));
                searchPaths.Add(Path.Combine(csgoRoot, "addons", "swiftlys2", "plugins", "extended-speedmeter", "resources", "payload.json"));
                searchPaths.Add(Path.Combine(csgoRoot, "addons", "swiftlys2", "plugins", "cs2-speedmeter", "resources", "payload.json"));
                searchPaths.Add(Path.Combine(csgoRoot, "addons", "swiftlys2", "plugins", "Speedometer", "resources", "payload.json"));

                foreach (string path in searchPaths)
                {
                    if (File.Exists(path))
                    {
                        jsonPath = path;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(jsonPath)) 
                {
                    Console.WriteLine("[Extended-Speedmeter] ERROR: 'resources/payload.json' not found!");
                    return;
                }

                string jsonContent = await File.ReadAllTextAsync(jsonPath);
                string serverName = Speedometer.Instance.SwiftlyCore.ConVar.Find<string>("hostname")?.Value ?? "CS2 Server";

                jsonContent = jsonContent
                    .Replace("{webhook_name}", "Speedometer Bot")
                    .Replace("{webhook_avatar}", "https://i.imgur.com/AfFp7pu.png") 
                    .Replace("{server_name}", serverName)
                    .Replace("{map}", mapName)
                    .Replace("{speed}", speed.ToString())
                    .Replace("{player_name}", playerName)
                    .Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"))
                    .Replace("{time}", DateTime.Now.ToString("HH:mm:ss"));

                using (var client = new HttpClient())
                {
                    var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                    await client.PostAsync(webhookUrl, content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Extended-Speedmeter] Webhook Error: {ex.Message}");
            }
        }
    }
}