# CliStem

<!-- bbpkg: one paragraph saying what this tool does and who runs it -->

## Running it

From a [release](https://github.com/REPO_OWNER/REPO_NAME/releases/latest): download the file for your
platform, unpack it, and run `CliStem` (`CliStem.exe` on Windows). It is one self-contained
executable: no .NET runtime needed.

```bash
CliStem notes.txt todo.txt
```

From source:

```bash
dotnet run --project src/CliStem -- notes.txt
```

| Exit code | Meaning |
|---|---|
| 0 | Success |
| 1 | It failed; the reason is on stderr |
| 2 | The command line was wrong; the usage is on stderr |
| 130 | Cancelled with Ctrl+C |

`CliStem --version` prints the release version.

See [AGENTS.md](AGENTS.md) for the tests, the release and the rest.

## Licence

MIT. See [LICENSE](LICENSE).
