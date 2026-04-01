using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Community.PowerToys.Run.Plugin.ObsidianVaults.ObsidianHelper;
using Community.PowerToys.Run.Plugin.ObsidianVaults.VaultsHelper;
using Wox.Infrastructure;
using Wox.Plugin;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.ObsidianVaults;

public class Main : IPlugin, IPluginI18n, IContextMenu
{
    private PluginInitContext? _context;

    public string Name => PluginStrings.PluginTitle;

    public string Description => PluginStrings.PluginDescription;

    public static string PluginID => "B8C0D9E1F2A345678901234567890CD";

    private readonly ObsidianVaultsApi _vaultsApi = new();

    private static readonly object _loadLock = new();
    private static bool _instancesLoaded;

    private static string FallbackIcoPath()
    {
        string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        return Path.Combine(pluginDir, "Images", "obsidian.dark.png");
    }

    /// <summary>
    /// PowerToys 可能在非 STA 线程上构造插件；构造函数内创建 WPF <see cref="System.Windows.Media.Imaging.BitmapImage"/> 会抛错并导致「插件初始化错误」。
    /// 改为在首次 <see cref="Query"/>（通常在 STA）时再加载 Obsidian 实例与图标。
    /// </summary>
    private void EnsureObsidianInstancesLoaded()
    {
        lock (_loadLock)
        {
            if (_instancesLoaded)
            {
                return;
            }

            try
            {
                ObsidianInstances.LoadObsidianInstances();
            }
            catch (Exception ex)
            {
                Log.Exception("Obsidian 库：加载 Obsidian 实例失败。", ex, typeof(Main));
            }
            finally
            {
                _instancesLoaded = true;
            }
        }
    }

    public List<Result> Query(Query query)
    {
        EnsureObsidianInstancesLoaded();

        var results = new List<Result>();

        if (query is null)
        {
            return results;
        }

        var search = query.Search?.Trim() ?? string.Empty;

        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in _vaultsApi.Vaults)
        {
            if (!seenPaths.Add(a.RootPath))
            {
                continue;
            }

            string title = a.DisplayName;
            string realPath = SystemPath.RealPath(a.RootPath);
            string subtitle = $"{PluginStrings.Vault} ({PluginStrings.TypeVaultLocal}): {realPath}";

            var tooltip = new ToolTipData(title, subtitle);

            string icoPath = a.ObsidianInstance?.WorkspaceIcoPath ?? FallbackIcoPath();

            results.Add(new Result
            {
                Title = title,
                SubTitle = subtitle,
                IcoPath = icoPath,
                ToolTipData = tooltip,
                Action = _ =>
                {
                    try
                    {
                        var uri = ObsidianUri.BuildOpenVaultUri(realPath);
                        var process = new ProcessStartInfo
                        {
                            FileName = uri,
                            UseShellExecute = true,
                            WindowStyle = ProcessWindowStyle.Hidden,
                        };
                        Process.Start(process);
                        return true;
                    }
                    catch (Win32Exception ex)
                    {
                        HandleError("无法通过 Obsidian URI 打开该库（请确认已安装 Obsidian 并已注册 URI）。", ex, showMsg: true);
                        return false;
                    }
                },
                ContextData = a,
            });
        }

        results = results.Where(r => r.Title.Contains(search, StringComparison.InvariantCultureIgnoreCase)).ToList();

        foreach (var x in results)
        {
            if (x.Score == 0)
            {
                x.Score = 100;
            }

            var intersection = Convert.ToInt32(x.Title.ToLowerInvariant().Intersect(search.ToLowerInvariant()).Count() * search.Length);
            var differenceWithQuery = Convert.ToInt32((x.Title.Length - intersection) * search.Length * 0.7);
            x.Score = x.Score - differenceWithQuery + intersection;
        }

        results = results.OrderByDescending(x => x.Score).ToList();
        if (string.IsNullOrEmpty(search))
        {
            results = results.OrderBy(x => x.Title).ToList();
        }

        return results;
    }

    public void Init(PluginInitContext context)
    {
        _context = context;
    }

    public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
    {
        if (selectedResult?.ContextData is not ObsidianVault vault)
        {
            return new List<ContextMenuResult>();
        }

        string realPath = SystemPath.RealPath(vault.RootPath);

        return new List<ContextMenuResult>
        {
            new()
            {
                PluginName = Name,
                Title = $"{PluginStrings.CopyPath} (Ctrl+C)",
                Glyph = "\xE8C8",
                FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                AcceleratorKey = Key.C,
                AcceleratorModifiers = ModifierKeys.Control,
                Action = _ => CopyToClipboard(realPath),
            },
            new()
            {
                PluginName = Name,
                Title = $"{PluginStrings.OpenInExplorer} (Ctrl+Shift+F)",
                Glyph = "\xEC50",
                FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                AcceleratorKey = Key.F,
                AcceleratorModifiers = ModifierKeys.Control | ModifierKeys.Shift,
                Action = _ => OpenInExplorer(realPath),
            },
            new()
            {
                PluginName = Name,
                Title = $"{PluginStrings.OpenInConsole} (Ctrl+Shift+C)",
                Glyph = "\xE756",
                FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                AcceleratorKey = Key.C,
                AcceleratorModifiers = ModifierKeys.Control | ModifierKeys.Shift,
                Action = _ => OpenInConsole(realPath),
            },
        };
    }

    private bool CopyToClipboard(string path)
    {
        try
        {
            Clipboard.SetText(path);
            return true;
        }
        catch (Exception ex)
        {
            HandleError("无法复制到剪贴板。", ex, showMsg: true);
            return false;
        }
    }

    private bool OpenInConsole(string path)
    {
        try
        {
            Helper.OpenInConsole(path);
            return true;
        }
        catch (Exception ex)
        {
            HandleError($"无法在终端中打开路径: {path}", ex, showMsg: true);
            return false;
        }
    }

    private bool OpenInExplorer(string path)
    {
        if (!Helper.OpenInShell("explorer.exe", $"\"{path}\""))
        {
            HandleError($"无法在资源管理器中打开: {path}", showMsg: true);
            return false;
        }

        return true;
    }

    private void HandleError(string msg, Exception? exception = null, bool showMsg = false)
    {
        if (exception != null)
        {
            Log.Exception(msg, exception, exception.GetType());
        }
        else
        {
            Log.Error(msg, typeof(Main));
        }

        if (showMsg && _context is not null)
        {
            _context.API.ShowMsg($"插件: {_context.CurrentPluginMetadata.Name}", msg);
        }
    }

    public string GetTranslatedPluginTitle() => PluginStrings.PluginTitle;

    public string GetTranslatedPluginDescription() => PluginStrings.PluginDescription;
}
