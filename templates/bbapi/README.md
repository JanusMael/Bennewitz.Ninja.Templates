# ApiStem

<!-- bbpkg: one paragraph saying what this API does and who calls it -->

## Running it

From a [release](https://github.com/REPO_OWNER/REPO_NAME/releases/latest): download the file for your
platform, unpack it, and run `ApiStem` (`ApiStem.exe` on Windows). It is one native executable: no
.NET runtime needed. It listens on `http://localhost:5000`; set `ASPNETCORE_URLS` to change that.

As a container:

```bash
docker build -t apistem .
docker run -p 8080:8080 apistem
```

From source:

```bash
dotnet run --project src/ApiStem
```

| Path | What it answers |
|---|---|
| `/openapi/v1.json` | The OpenAPI document: every endpoint, its parameters and its responses |
| `/api/greetings/{name}` | A greeting, as an example of the API's shape |
| `/healthz` | `ok` while the API is serving |
| `/version` | The release version, the build stamp and the commit |

See [AGENTS.md](AGENTS.md) for the tests, the release and the rest.

## Licence

MIT. See [LICENSE](LICENSE).
