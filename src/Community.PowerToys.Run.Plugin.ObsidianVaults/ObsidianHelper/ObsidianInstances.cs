using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;

namespace Community.PowerToys.Run.Plugin.ObsidianVaults.ObsidianHelper;

public static class ObsidianInstances
{
    private static readonly string LocalAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static readonly string UserProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public static List<ObsidianInstance> Instances { get; } = new();

    private static BitmapImage BitmapImageFromFile(string absolutePath)
    {
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.UriSource = new Uri(absolutePath, UriKind.Absolute);
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }

    private static Bitmap BitmapOverlayToCenter(Bitmap bitmap1, Bitmap overlayBitmap)
    {
        int bitmap1Width = bitmap1.Width;
        int bitmap1Height = bitmap1.Height;
        bitmap1.SetResolution(144, 144);
        using Bitmap overlayBitmapResized = new(overlayBitmap, new System.Drawing.Size(bitmap1Width / 2, bitmap1Height / 2));

        float marginLeft = (float)((bitmap1Width * 0.7) - (overlayBitmapResized.Width * 0.5));
        float marginTop = (float)((bitmap1Height * 0.7) - (overlayBitmapResized.Height * 0.5));

        Bitmap finalBitmap = new(bitmap1Width, bitmap1Height);
        using Graphics g = Graphics.FromImage(finalBitmap);
        g.DrawImage(bitmap1, System.Drawing.Point.Empty);
        g.DrawImage(overlayBitmapResized, marginLeft, marginTop);

        return finalBitmap;
    }

    /// <summary>定位 Obsidian 安装目录（常见安装与 Scoop）。</summary>
    public static void LoadObsidianInstances()
    {
        Instances.Clear();

        var exeCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var standard in new[]
                 {
                     Path.Combine(LocalAppDataPath, "Obsidian", "Obsidian.exe"),
                     Path.Combine(LocalAppDataPath, "Programs", "Obsidian", "Obsidian.exe"),
                     Path.Combine(LocalAppDataPath, "Programs", "obsidian", "Obsidian.exe"),
                 })
        {
            if (File.Exists(standard))
            {
                exeCandidates.Add(Path.GetFullPath(standard));
            }
        }

        foreach (var scoopExe in EnumerateScoopObsidianExeCandidates())
        {
            exeCandidates.Add(scoopExe);
        }

        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            foreach (var segment in pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var dir = segment.Trim();
                if (dir.Length == 0 || !Directory.Exists(dir))
                {
                    continue;
                }

                foreach (var name in new[] { "Obsidian.exe", "obsidian.exe" })
                {
                    var obsidianExe = Path.Combine(dir, name);
                    if (File.Exists(obsidianExe))
                    {
                        exeCandidates.Add(Path.GetFullPath(obsidianExe));
                    }
                }
            }
        }

        var resolvedExe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in exeCandidates)
        {
            var r = ResolveExecutablePath(file);
            if (File.Exists(r))
            {
                resolvedExe.Add(Path.GetFullPath(r));
            }
        }

        foreach (var file in resolvedExe)
        {
            var instance = TryCreateInstance(file);
            if (instance != null)
            {
                Instances.Add(instance);
            }
        }
    }

    private static IEnumerable<string> EnumerateScoopObsidianExeCandidates()
    {
        var roots = new List<string>();
        if (!string.IsNullOrEmpty(UserProfilePath))
        {
            roots.Add(Path.Combine(UserProfilePath, "scoop"));
        }

        string? scoopEnv = Environment.GetEnvironmentVariable("SCOOP");
        if (!string.IsNullOrEmpty(scoopEnv))
        {
            roots.Add(scoopEnv);
        }

        string globalScoop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "scoop");
        if (Directory.Exists(globalScoop))
        {
            roots.Add(globalScoop);
        }

        foreach (var root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string current = Path.Combine(root, "apps", "obsidian", "current", "Obsidian.exe");
            if (File.Exists(current))
            {
                yield return Path.GetFullPath(current);
            }
        }
    }

    /// <summary>PATH 里指向的是 shims 下的启动器时，改用 apps\obsidian\current 里的真实可执行文件。</summary>
    private static string ResolveExecutablePath(string file)
    {
        if (!file.Contains("shims", StringComparison.OrdinalIgnoreCase))
        {
            return file;
        }

        foreach (var scoopExe in EnumerateScoopObsidianExeCandidates())
        {
            if (File.Exists(scoopExe))
            {
                return scoopExe;
            }
        }

        return file;
    }

    private static string GetStableShortId(string exePath)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(exePath));
        return Convert.ToHexString(bytes.AsSpan(0, 8));
    }

    private static ObsidianInstance? TryCreateInstance(string file)
    {
        if (!File.Exists(file))
        {
            return null;
        }

        string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;

        using Icon? obsidianIcon = Icon.ExtractAssociatedIcon(file);
        if (obsidianIcon is null)
        {
            return null;
        }

        string cacheDir = Path.Combine(pluginDir, "IconCache");
        Directory.CreateDirectory(cacheDir);
        string id = GetStableShortId(file);
        string wsIcoPath = Path.Combine(cacheDir, $"{id}.ws.png");
        string rmIcoPath = Path.Combine(cacheDir, $"{id}.remote.png");

        BitmapImage workspaceBitmap;
        BitmapImage remoteBitmap;
        string workspaceIcoPathForResult;
        string remoteIcoPathForResult;

        try
        {
            string folderPng = Path.Join(pluginDir, "Images", "folder.png");
            string monitorPng = Path.Join(pluginDir, "Images", "monitor.png");

            using var obsidianIconBitmap = obsidianIcon.ToBitmap();
            if (File.Exists(folderPng) && File.Exists(monitorPng))
            {
                using var folderIcon = (Bitmap)Image.FromFile(folderPng);
                using var bitmapFolderIcon = BitmapOverlayToCenter(folderIcon, obsidianIconBitmap);
                using var monitorIcon = (Bitmap)Image.FromFile(monitorPng);
                using var bitmapMonitorIcon = BitmapOverlayToCenter(monitorIcon, obsidianIconBitmap);
                bitmapFolderIcon.Save(wsIcoPath, ImageFormat.Png);
                bitmapMonitorIcon.Save(rmIcoPath, ImageFormat.Png);
                workspaceBitmap = BitmapImageFromFile(wsIcoPath);
                remoteBitmap = BitmapImageFromFile(rmIcoPath);
            }
            else
            {
                using (var wsBmp = (Bitmap)obsidianIconBitmap.Clone())
                using (var rmBmp = (Bitmap)obsidianIconBitmap.Clone())
                {
                    wsBmp.Save(wsIcoPath, ImageFormat.Png);
                    rmBmp.Save(rmIcoPath, ImageFormat.Png);
                }

                workspaceBitmap = BitmapImageFromFile(wsIcoPath);
                remoteBitmap = BitmapImageFromFile(rmIcoPath);
            }

            workspaceIcoPathForResult = wsIcoPath;
            remoteIcoPathForResult = rmIcoPath;
        }
        catch
        {
            string dark = Path.Combine(pluginDir, "Images", "obsidian.dark.png");
            string light = Path.Combine(pluginDir, "Images", "obsidian.light.png");
            if (!File.Exists(dark))
            {
                return null;
            }

            workspaceBitmap = BitmapImageFromFile(dark);
            remoteBitmap = File.Exists(light) ? BitmapImageFromFile(light) : workspaceBitmap;
            workspaceIcoPathForResult = dark;
            remoteIcoPathForResult = File.Exists(light) ? light : dark;
        }

        return new ObsidianInstance
        {
            ExecutablePath = file,
            WorkspaceIcoPath = workspaceIcoPathForResult,
            RemoteIcoPath = remoteIcoPathForResult,
            WorkspaceIconBitMap = workspaceBitmap,
            RemoteIconBitMap = remoteBitmap,
        };
    }
}
