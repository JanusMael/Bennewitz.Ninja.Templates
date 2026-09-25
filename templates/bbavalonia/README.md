# AppStem

<!-- bbpkg: one paragraph saying what this app does and who it is for -->

## Install

Download the file for your platform from the
[latest release](https://github.com/REPO_OWNER/REPO_NAME/releases/latest) and run it. Each is one
self-contained file: nothing else to install, no .NET runtime needed.

| Platform | File |
|---|---|
| Windows | `AppStem-<version>-win-x64.zip`, or `-win-arm64` |
| Linux | `AppStem-<version>-linux-x64.tar.gz`, or `-linux-arm64` |
| macOS | `AppStem-<version>-osx-arm64.tar.gz` (Apple silicon), or `-osx-x64` |

The binaries are not signed. Windows SmartScreen and macOS Gatekeeper warn on first run; on macOS,
`xattr -d com.apple.quarantine AppStem` lets it start.

## Building

```bash
dotnet run --project src/AppStem
```

See [AGENTS.md](AGENTS.md) for the tests, the trimmed publish, and the rest.

## Licence

MIT. See [LICENSE](LICENSE).
