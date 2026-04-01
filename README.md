# Obsidian 库 · PowerToys Run 插件

在 **PowerToys Run** 里快速搜索并打开本机 [Obsidian](https://obsidian.md/) 中已配置的库（Vault），数据来源与 Obsidian 一致：`%AppData%\obsidian\obsidian.json`（亦尝试 `%AppData%\Obsidian\obsidian.json`）。

[![PowerToys](https://img.shields.io/badge/PowerToys-Run-0078D4?style=flat&logo=microsoft)](https://github.com/microsoft/PowerToys)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)

---

## 功能概览

| 能力 | 说明 |
|------|------|
| **列表来源** | 读取 Obsidian 用户配置中的 `vaults`（支持对象或数组形式） |
| **打开方式** | 使用官方 Obsidian URI：`obsidian://open?path=` + 库根目录路径（经 URI 编码） |
| **图标** | 自动发现 `Obsidian.exe` 并生成与 Cursor 工作区插件类似的叠加图标；未安装 Obsidian 时仍列出库，使用插件自带图标 |
| **右键菜单** | 复制路径、在资源管理器中打开、在终端中打开（含快捷键提示） |

默认 **激活词** 为 `}`（可在 PowerToys → PowerToys Run → 插件 中修改）。  
若你同时安装 [Powertoys-Run-Cursor-Workspace](https://github.com/) 类插件且也使用 `}`，请在设置中为其中一个插件改用其它激活词，避免冲突。

在 Run 中输入 `}` 后键入关键字即可按**库显示名称**筛选；无关键字时按标题排序浏览。

---

## 环境要求

- **Windows 10 / 11**（x64 或 ARM64）
- 已安装 [**PowerToys**](https://github.com/microsoft/PowerToys)（需包含 **PowerToys Run**）
- 已安装 **Obsidian**，且至少在 Obsidian 中添加过一个库（否则 `obsidian.json` 中可能无条目）
- 系统已注册 **Obsidian URI**（正常安装 Obsidian 后会注册；否则无法通过 URI 打开）

---

## 安装

### 从源码构建

**依赖**：[.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0)（Windows）

在仓库根目录执行：

```powershell
dotnet build ObsidianVaults.sln -c Release -p:Platform=x64
# 或 ARM64
dotnet build ObsidianVaults.sln -c Release -p:Platform=ARM64
```

输出位于：

`src\Community.PowerToys.Run.Plugin.ObsidianVaults\bin\<平台>\Release\`

将 **该 Release 目录下全部文件** 复制到 PowerToys Run 插件目录下的**单独子文件夹**，例如：

`%LocalAppData%\Microsoft\PowerToys\PowerToys Run\Plugins\ObsidianVaults\`

解压或复制后应能直接看到 `plugin.json`、`Community.PowerToys.Run.Plugin.ObsidianVaults.dll` 与 `Images` 等。然后**重启 PowerToys**，在 **PowerToys 设置 → PowerToys Run → 插件** 中启用 **「Obsidian 库」**。

### 打包 zip

```powershell
.\scripts\pack-dist.ps1 -Version 1.0.0 -Build
```

会在 `dist\` 下生成 `ObsidianVaults-v*-win-x64.zip` 与 `win-arm64` 包。

生成 SHA256
```shell
Get-ChildItem .\dist\* | ForEach-Object {
  $h = Get-FileHash $_.FullName -Algorithm SHA256
  "$($h.Hash)  $($_.Name)"
} | Set-Content .\dist\SHA256SUMS.txt
```

---

## 实现说明（与 Cursor 工作区插件的对照）

- 结构上与 [Powertoys-Run-Cursor-Workspace](https://github.com/) 一致：`Main`（查询与上下文菜单）、`*Instances`（探测安装路径与图标缓存）、`*Api`（聚合列表）。
- 库列表不读 SQLite，仅解析 `obsidian.json`；打开动作使用 **`obsidian://` 协议**而非直接启动 exe 加参数。

---

## 图标资源

`Images\folder.png`、`monitor.png` 可由仓库旁 Cursor 插件中的 `tools\RenderPluginIcons` 生成；`obsidian.dark.png` / `obsidian.light.png` 为列表占位图，可按需替换。
