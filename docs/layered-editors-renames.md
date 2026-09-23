# LayeredEditors renames — what became what, and why

The `LayeredEditors` libraries left ClaudeForge as seven packages in two repositories, and have been
renamed twice on the way. This is every name that changed, when, and the decision behind it.

## The two rounds

| Round | Ships in | What changed | Why | Decided by |
|---|---|---|---|---|
| **1 · The split** | `2026.3.923` | Five `LayeredEditors.*` projects became seven ids in two repositories, `Bennewitz.Ninja.AppServices` and `Bennewitz.Ninja.ScopedEditors`. `LayeredEditors` became `ScopedEditors`, and every id took the `Bennewitz.Ninja.` prefix | A package's identity is its dependency closure: application services and editors share no edge, so they are separate families. "Scoped" names the override model precisely where "Layered" was ambiguous; "Settings" was ruled out because it reads as flat key-value pairs | [`plans/00002`](../plans/00002-layerededitors-becomes-two-package-repositories.md), decisions 1 and 2, from the measured analysis in [`layered-editors-package-split.md`](layered-editors-package-split.md) |
| **2 · `.Avalonia` → `.AvaloniaUI`** | `2026.3.924` | The two Avalonia packages: package id, assembly, namespace and project folder | AQ1004 found the `Avalonia` namespace segment shadowing Avalonia's own root namespace: inside `…ScopedEditors.Avalonia`, the name `Avalonia.Media.Color` resolves to `…ScopedEditors.Avalonia.Media.Color` and fails with CS0234. Renamed all the way through, not just the namespace, because this family names every namespace `Bennewitz.Ninja.<AssemblyName>` | The developer, 2026-09-23, after AQ1004 reported 8 findings on the published `2026.3.923`. Re-confirmed the same day when AQ1004's message suggested the lighter, namespace-only fix; that message now names both |

The two `.Avalonia` ids stop at `2026.3.923`. Once `2026.3.924` is published they are deprecated on
nuget.org, pointing at the `.AvaloniaUI` ids.

## Where each project went

```mermaid
flowchart LR
    subgraph before["ClaudeForge, before the split"]
        LA["LayeredEditors.Abstractions"]
        LV["LayeredEditors.ViewModels"]
        LU["LayeredEditors.Avalonia"]
        LS["LayeredEditors.Avalonia.Services"]
        LD["LayeredEditors.Avalonia.Diagnostics"]
    end
    subgraph scoped["Bennewitz.Ninja.ScopedEditors"]
        SA[".Abstractions"]
        SV[".ViewModels"]
        SU[".AvaloniaUI"]
    end
    subgraph appsvc["Bennewitz.Ninja.AppServices"]
        AA[".Abstractions"]
        AB["AppServices (bare id)"]
        AL[".Logging"]
        AU[".AvaloniaUI"]
    end
    CF["stays in ClaudeForge"]
    LA --> SA
    LA -- dialog vocabulary --> AA
    LV --> SV
    LU --> SU
    LS -- contracts --> AA
    LS -- default implementations --> AB
    LS -- Avalonia dialog service --> AU
    LD -- NativeErrorDialog --> AB
    LD -- rolling file sink --> AL
    LD -- dialogs, Serilog bridge, diagnostics --> AU
    LD -. live log and tail windows .-> CF
```

## Every package

| Id at `2026.3.924` | Id at `2026.3.923` | Came from, in ClaudeForge | Assembly | Namespace root |
|---|---|---|---|---|
| `Bennewitz.Ninja.ScopedEditors.Abstractions` | unchanged | `LayeredEditors.Abstractions`: schemas, scopes, values, workspaces and the severity model, less the dialog vocabulary | `ScopedEditors.Abstractions` | `Bennewitz.Ninja.ScopedEditors.Abstractions` |
| `Bennewitz.Ninja.ScopedEditors.ViewModels` | unchanged | `LayeredEditors.ViewModels` | `ScopedEditors.ViewModels` | `Bennewitz.Ninja.ScopedEditors.ViewModels` |
| `Bennewitz.Ninja.ScopedEditors.AvaloniaUI` | `Bennewitz.Ninja.ScopedEditors.Avalonia` | `LayeredEditors.Avalonia` | `ScopedEditors.AvaloniaUI` | `Bennewitz.Ninja.ScopedEditors.AvaloniaUI` |
| `Bennewitz.Ninja.AppServices.Abstractions` | unchanged | The contracts from `LayeredEditors.Avalonia.Services` (`IShellLauncher`, `IShareService`, `IDialogService`, `IEnvironmentProvider`, `IPermissionPathPicker`, `ISaveChangesPrompt`, `FilePickerFilter`, `ShareOutcome`, `UnsavedChangesChoice`), the dialog vocabulary from `LayeredEditors.Abstractions` (`DialogMessage`, `DialogMessageBuilder`, `DialogSegment`, `DialogSegmentKind`, `DialogCategory`), and four types new in the split: `LaunchResult`, `LaunchStatus`, `DiagnosticSink`, `DiagnosticLevel` | `AppServices.Abstractions` | `Bennewitz.Ninja.AppServices.Abstractions` |
| `Bennewitz.Ninja.AppServices` | unchanged | `ShellLauncher`, `DefaultShareService` and `DefaultEnvironmentProvider` from `LayeredEditors.Avalonia.Services`; `NativeErrorDialog` from `LayeredEditors.Avalonia.Diagnostics` | `AppServices` | `Bennewitz.Ninja.AppServices` |
| `Bennewitz.Ninja.AppServices.Logging` | unchanged | `BucketedRollingFileSink` from `LayeredEditors.Avalonia.Diagnostics` | `AppServices.Logging` | `Bennewitz.Ninja.AppServices.Logging` |
| `Bennewitz.Ninja.AppServices.AvaloniaUI` | `Bennewitz.Ninja.AppServices.Avalonia` | `AvaloniaDialogService` from `LayeredEditors.Avalonia.Services`; `FatalErrorDialog`, `NonFatalNoticeDialog`, `SerilogAvaloniaSink`, `BindingValidationErrorLogger` and `AvaloniaDiagnostics` from `LayeredEditors.Avalonia.Diagnostics` | `AppServices.AvaloniaUI` | `Bennewitz.Ninja.AppServices.AvaloniaUI` |

**Not moved:** `LiveLogWindow`, `LiveTailWindow`, `HeaderLink` and `LiveLogWindowSink` stay in
ClaudeForge. The plan expected `LiveLogWindowSink` to ship under a new name, believing it never
referenced its window. It calls `LiveLogWindow.EnqueueLog` directly, so it stayed with the windows,
and a host now attaches it through `AvaloniaDiagnosticsOptions.ConfigureLogger`.

## Names that follow from a package

| Name | Before the split | `2026.3.923` | `2026.3.924` | Rule |
|---|---|---|---|---|
| Package id | `Bennewitz.Ninja.LayeredEditors.*` on ClaudeForge's GitHub Packages feed, which nothing ever resolved. DiffView consumes the unprefixed `LayeredEditors.Avalonia.Diagnostics` `1.0.1`, hand-packed to a folder feed | `Bennewitz.Ninja.ScopedEditors.Avalonia` on nuget.org | `Bennewitz.Ninja.ScopedEditors.AvaloniaUI` | Every published id carries the `Bennewitz.Ninja.` prefix, which is reserved on nuget.org |
| Assembly | `LayeredEditors.Avalonia` | `ScopedEditors.Avalonia` | `ScopedEditors.AvaloniaUI` | Assemblies stay unprefixed and are named for their project |
| Namespace root | `Bennewitz.Ninja.LayeredEditors.Avalonia` | `Bennewitz.Ninja.ScopedEditors.Avalonia` | `Bennewitz.Ninja.ScopedEditors.AvaloniaUI` | `Bennewitz.Ninja.<AssemblyName>` |
| `avares://` URI | `avares://LayeredEditors.Avalonia/…` | `avares://ScopedEditors.Avalonia/…` | `avares://ScopedEditors.AvaloniaUI/…` | Names the ASSEMBLY, so it moves whenever the assembly does |
| Test project | `LayeredEditors.Avalonia.Tests`, `LayeredEditors.Avalonia.Diagnostics.Tests`, and parts of `ClaudeForge.Tests` | `ScopedEditors.Tests` and `AppServices.Tests`, holding the new layering guards | the same two, with the original suites ported in | One test project per repository, carrying the tests of the code it ships |

The rows show the ScopedEditors Avalonia package; the AppServices one moved the same way, from
`LayeredEditors.Avalonia.Services` and `.Diagnostics` to `AppServices.Avalonia` and then
`AppServices.AvaloniaUI`. It has no `avares://` URIs.

⚠ **A stale `avares://` URI is not silent, and it fails differently by where it sits.** A
`StyleInclude` in AXAML fails the build with `AVLN2000`; a `FontFamily` naming one family throws
`InvalidOperationException` the first time text in it is laid out; a `FontFamily` with a fallback
list draws the next face and reports nothing.

## Members renamed by the API reshape

The service contracts changed shape before their first publish, which is when that was free, and
the four launcher methods took the `Async` suffix with it.

| Before the split | `2026.3.923` | `2026.3.924` |
|---|---|---|
| `bool LaunchTerminalWithCommand(command)` | `ValueTask<LaunchResult> LaunchTerminalWithCommandAsync(command, cancellationToken = default)` | the token is required |
| `void RevealInFileManager(filePath)` | `ValueTask<LaunchResult> RevealInFileManagerAsync(filePath, cancellationToken = default)` | the token is required |
| `void OpenInDefaultEditor(filePath)` | `ValueTask<LaunchResult> OpenInDefaultEditorAsync(filePath, cancellationToken = default)` | the token is required |
| `void LaunchUrl(url)` | `ValueTask<LaunchResult> LaunchUrlAsync(url, cancellationToken = default)` | the token is required |
| `Task<ShareOutcome> ShareTextAsync(title, text, uri = null)` | `ValueTask<ShareOutcome> ShareTextAsync(title, text, uri = null, cancellationToken = default)` | `uri` and the token are both required |
| `Task<ShareOutcome> ShareFileAsync(title, filePath)` | `ValueTask<ShareOutcome> ShareFileAsync(title, filePath, cancellationToken = default)` | the token is required |

`2026.3.923` made them asynchronous and gave them a result that says which failure happened: the
service API shape in `plans/00002`. `2026.3.924` removed every defaulted `CancellationToken` for
AQ1001; `uri` lost its default with it, because a required parameter cannot follow an optional one
(CS1737).
