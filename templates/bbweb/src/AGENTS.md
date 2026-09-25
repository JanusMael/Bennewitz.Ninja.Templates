# AGENTS.md — `src/`

The site. `src/SiteStem` is an ASP.NET Core app on Kestrel: MVC controllers and `.cshtml` views,
static assets through `MapStaticAssets`, and `/healthz` and `/version`.

<!-- bbpkg: list the projects under src/ and what each one is, once there is more than the site -->

| Path | What it is |
|---|---|
| `SiteStem/Program.cs` | The host: services, forwarded headers, error pages, routes, `/healthz`, `/version` |
| `SiteStem/Controllers/` | MVC controllers; `HomeController` also renders every error page |
| `SiteStem/Views/` | The layout, the views, and `Shared/Status.cshtml`, the error page |
| `SiteStem/wwwroot/` | Static assets, served with fingerprinting by `MapStaticAssets` |
| `SiteStem/Components/` | Interactive server components, when the site has them, embedded in views with the Component Tag Helper |
| `SiteStem/Properties/launchSettings.json` | Local development only: `http://localhost:5080` |

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| The error page shows a status and a title, never an exception | A stack trace tells an attacker what the site runs | `SiteTests` |
| `PublishTrimmed` stays `false` | MVC, Razor views and components create what they render by reflection; a trimmed site fails at runtime while its analysis reads clean | `SiteStem.csproj` |
| `IsTransformWebConfigDisabled` stays `true` | The host is Kestrel, directly or behind a reverse proxy, not IIS | `SiteStem.csproj` |
| Only loopback proxies are trusted for `X-Forwarded-*` | A header from anyone else would let a client claim any address or scheme | `Program.cs` |
| `appsettings.Development.json` is never published | Development settings stay on the developer's machine | `SiteStem.csproj` |
| The site project stays `IsPackable` `false` | A site is run, not referenced | `PackagingTests` |
