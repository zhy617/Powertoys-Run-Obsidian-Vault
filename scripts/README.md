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


# Release
## Win
```shell
$VERSION = "v1.0.2"
gh release create $VERSION `
  --title "$VERSION" `
  --notes-file .\scripts\RELEASE_NOTES.md `
  .\dist\*
```