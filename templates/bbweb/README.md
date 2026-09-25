# SiteStem

<!-- bbpkg: one paragraph saying what this site is and who it is for -->

## Running it

From a [release](https://github.com/REPO_OWNER/REPO_NAME/releases/latest): download the file for your
platform, unpack it, and run `SiteStem` (`SiteStem.exe` on Windows). It listens on
`http://localhost:5000`; set `ASPNETCORE_URLS` to change that. Each archive is self-contained: no
.NET runtime needed.

As a container:

```bash
docker build -t sitestem .
docker run -p 8080:8080 sitestem
```

From source:

```bash
dotnet run --project src/SiteStem
```

`/healthz` answers `ok` while the site is serving; `/version` names the build.

See [AGENTS.md](AGENTS.md) for the tests, the release and the rest.

## Licence

MIT. See [LICENSE](LICENSE).
