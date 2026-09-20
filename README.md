# Bennewitz.Ninja.PackageTemplate

A starting point for a .NET NuGet package that publishes through **NuGet.org Trusted Publishing
(OIDC)** — no long-lived API key anywhere. It is two things at once:

```bash
# GitHub route
gh repo create Bennewitz.Ninja.Widget --template JanusMael/Bennewitz.Ninja.PackageTemplate

# SDK route
dotnet new install Bennewitz.Ninja.PackageTemplate
dotnet new bbpkg -n Widget --RepoOwner JanusMael
```

⚠ **`-n` takes the unprefixed stem.** `-n Widget` produces assembly `Widget` and package id
`Bennewitz.Ninja.Widget`, which is the convention across these repos. Passing the full id instead
doubles the prefix in the namespace.

## What comes with it

| | |
|---|---|
| `ci.yml` | build, test and the packaging guard on every push |
| `release.yml` | tag-triggered release, and a **credential preflight** that logs in to NuGet.org and stops |
| `packages.push` / `packages.local` | what publishes, and what packs but must never reach nuget.org |
| `docs/publishing.md` | the runbook: policy fields, the version rule, and what to check after |

## The two things that cost a real release

Both were learned the expensive way on `Bennewitz.Ninja.XamlQuality` `2026.3.920`, and both are
encoded here rather than left to memory.

**One trusted-publishing policy, never one per package id.** NuGet.org mints one API key per token
exchange, scoped to one matching policy. Two policies matching the same repository and workflow
means one is chosen and the other's package is rejected `403` — after the first has already
published permanently. A single policy carries as many glob patterns as the pushable set needs.

**Packed is not pushable.** `dotnet pack` over a solution can produce packages that must stay
private. The push names every id explicitly and a test asserts the packed set equals
`packages.push ∪ packages.local`, so a new packable project fails the build until somebody
classifies it — loudly and early, rather than permanently and late.

## Licence

MIT. See [LICENSE](LICENSE).
