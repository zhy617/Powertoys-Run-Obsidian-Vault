using System.Windows.Media.Imaging;

namespace Community.PowerToys.Run.Plugin.ObsidianVaults.ObsidianHelper;

public sealed class ObsidianInstance
{
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>供 PowerToys Run 结果列表使用的绝对路径图标（见 Main.Query 中 IcoPath）。</summary>
    public string WorkspaceIcoPath { get; set; } = string.Empty;

    /// <summary>与 WorkspaceIcoPath 相同；保留字段以与 Cursor 插件图标结构一致。</summary>
    public string RemoteIcoPath { get; set; } = string.Empty;

    public BitmapImage WorkspaceIconBitMap { get; set; } = null!;

    public BitmapImage RemoteIconBitMap { get; set; } = null!;
}
