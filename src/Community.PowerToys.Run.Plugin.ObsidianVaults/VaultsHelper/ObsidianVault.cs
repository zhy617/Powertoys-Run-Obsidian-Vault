using Community.PowerToys.Run.Plugin.ObsidianVaults.ObsidianHelper;

namespace Community.PowerToys.Run.Plugin.ObsidianVaults.VaultsHelper;

public sealed class ObsidianVault
{
    /// <summary>库根目录的绝对路径（规范化后）。</summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>用于列表标题：obsidian.json 中的 name，缺省为文件夹名。</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>用于列表图标；若未检测到 Obsidian 安装则为 null，此时使用插件自带 PNG。</summary>
    public ObsidianInstance? ObsidianInstance { get; set; }
}
