# Release
## Win
```shell
$VERSION = "v1.0.0"
gh release create $VERSION `
  --title "$VERSION" `
  --notes-file .\scripts\RELEASE_NOTES.md `
  .\dist\*
```