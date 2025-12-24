using CounterStrikeSharp.API;
using System.Text.Json;

namespace InfiniteWeapons
{
    public partial class InfiniteWeapons
    {
        private readonly string _signatureName = "CCSPlayer_WeaponServices_Weapon_GetSlot";
        private readonly string _signatureWindows = "48 89 5C 24 08 48 89 6C 24 10 48 89 74 24 18 57 41 56 41 57 48 83 EC 20 33 FF 45 8B F0";
        private readonly string _signatureLinux = "55 48 89 E5 41 57 41 56 41 55 41 54 53 89 F3 48 83 EC 08 48 63 77 40";

        private void UpdateGamedata()
        {
            DebugPrint("Checking gamedata...");
            string gamedataPath = Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/gamedata", Config.SignatureFile);
            // check if file exists and create otherwise
            if (!File.Exists(gamedataPath))
            {
                DebugPrint("Gamedata file not found, creating new one...");
                Directory.CreateDirectory(Path.GetDirectoryName(gamedataPath)!);
                File.WriteAllText(gamedataPath, "{}");
            }
            else
            {
                DebugPrint("Gamedata file found");
            }
            // Load and parse JSON
            string json = File.ReadAllText(gamedataPath);
            if (JsonSerializer.Deserialize<Dictionary<string, object>>(json) is not Dictionary<string, object> gamedata)
            {
                DebugPrint("Gamedata is null");
                return;
            }
            // Try to get the signature entry
            if (!gamedata.TryGetValue(_signatureName, out var entryObj) ||
            entryObj is not JsonElement entryElement || entryElement.ValueKind != JsonValueKind.Object ||
            !entryElement.TryGetProperty("signatures", out var signaturesElement) || signaturesElement.ValueKind != JsonValueKind.Object ||
            (string.IsNullOrEmpty(Config.OverrideSignatureWindows)
                ? !string.Equals(signaturesElement.GetProperty("windows").GetString(), _signatureWindows, StringComparison.Ordinal)
                : !string.Equals(signaturesElement.GetProperty("windows").GetString(), Config.OverrideSignatureWindows, StringComparison.Ordinal)) ||
            (string.IsNullOrEmpty(Config.OverrideSignatureLinux)
                ? !string.Equals(signaturesElement.GetProperty("linux").GetString(), _signatureLinux, StringComparison.Ordinal)
                : !string.Equals(signaturesElement.GetProperty("linux").GetString(), Config.OverrideSignatureLinux, StringComparison.Ordinal)) ||
            !signaturesElement.TryGetProperty("library", out var libraryElement) || !string.Equals(libraryElement.GetString(), "server", StringComparison.Ordinal))
            {
                DebugPrint($"Updating or creating entry for {_signatureName}");
                // Create or update the entry
                gamedata[_signatureName] = new Dictionary<string, object>
                {
                    ["signatures"] = new Dictionary<string, object>
                    {
                        ["library"] = "server",
                        ["windows"] = !string.IsNullOrEmpty(Config.OverrideSignatureWindows) ? Config.OverrideSignatureWindows : _signatureWindows,
                        ["linux"] = !string.IsNullOrEmpty(Config.OverrideSignatureLinux) ? Config.OverrideSignatureLinux : _signatureLinux,
                    }
                };
                // Serialize and save the updated gamedata
                var serializerOptions = new JsonSerializerOptions { WriteIndented = true };
                string updatedJson = JsonSerializer.Serialize(gamedata, serializerOptions);
                File.WriteAllText(gamedataPath, updatedJson);
                DebugPrint("Gamedata updated successfully");
            }
            else
            {
                DebugPrint("Gamedata is already up-to-date");
            }
        }
    }
}