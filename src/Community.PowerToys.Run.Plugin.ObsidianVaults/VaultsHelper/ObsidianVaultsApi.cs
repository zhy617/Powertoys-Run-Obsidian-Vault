using System.IO;
using System.Text.Json;
using Community.PowerToys.Run.Plugin.ObsidianVaults.ObsidianHelper;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.ObsidianVaults.VaultsHelper;

public sealed class ObsidianVaultsApi
{
    private static IEnumerable<string> ObsidianJsonCandidates()
    {
        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        yield return Path.Combine(roaming, "obsidian", "obsidian.json");
        yield return Path.Combine(roaming, "Obsidian", "obsidian.json");
    }

    public List<ObsidianVault> Vaults
    {
        get
        {
            var results = new List<ObsidianVault>();
            ObsidianInstance? iconSource = ObsidianInstances.Instances.FirstOrDefault();
            foreach (var jsonPath in ObsidianJsonCandidates())
            {
                if (!File.Exists(jsonPath))
                {
                    continue;
                }

                results.AddRange(ParseObsidianJson(jsonPath, iconSource));
                break;
            }

            return results;
        }
    }

    private static List<ObsidianVault> ParseObsidianJson(string filePath, ObsidianInstance? iconSource)
    {
        var list = new List<ObsidianVault>();
        string text;
        try
        {
            text = File.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            Log.Exception($"无法读取 Obsidian 配置: {filePath}", ex, typeof(ObsidianVaultsApi));
            return list;
        }

        try
        {
            using var doc = JsonDocument.Parse(text);
            if (!doc.RootElement.TryGetProperty("vaults", out var vaultsEl))
            {
                return list;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (vaultsEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in vaultsEl.EnumerateObject())
                {
                    AddVaultFromElement(prop.Value, seen, list, iconSource);
                }
            }
            else if (vaultsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in vaultsEl.EnumerateArray())
                {
                    AddVaultFromElement(el, seen, list, iconSource);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Exception($"解析 Obsidian 配置失败: {filePath}", ex, typeof(ObsidianVaultsApi));
        }

        return list;
    }

    private static void AddVaultFromElement(JsonElement el, HashSet<string> seen, List<ObsidianVault> list, ObsidianInstance? iconSource)
    {
        if (!el.TryGetProperty("path", out var pathEl))
        {
            return;
        }

        string? path = pathEl.GetString();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }
        catch
        {
            return;
        }

        if (!Directory.Exists(fullPath))
        {
            return;
        }

        if (!seen.Add(fullPath))
        {
            return;
        }

        string? customName = el.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
            ? nameEl.GetString()
            : null;

        string folderName = Path.GetFileName(fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(folderName))
        {
            var di = new DirectoryInfo(fullPath);
            folderName = di.Name.TrimEnd(':');
        }

        string display = !string.IsNullOrWhiteSpace(customName) ? customName! : folderName;

        list.Add(new ObsidianVault
        {
            RootPath = fullPath,
            DisplayName = display,
            ObsidianInstance = iconSource,
        });
    }
}
