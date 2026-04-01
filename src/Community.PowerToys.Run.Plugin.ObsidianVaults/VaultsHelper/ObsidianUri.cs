using System.IO;

namespace Community.PowerToys.Run.Plugin.ObsidianVaults.VaultsHelper;

/// <summary>构建 Obsidian 官方 URI（参见 Obsidian 帮助文档中 Obsidian URI / open）。</summary>
internal static class ObsidianUri
{
    /// <summary>使用 <c>path</c> 参数打开位于该路径的库（或包含该路径的库）。</summary>
    public static string BuildOpenVaultUri(string vaultRootPath)
    {
        string full = Path.GetFullPath(vaultRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return "obsidian://open?path=" + Uri.EscapeDataString(full);
    }
}
