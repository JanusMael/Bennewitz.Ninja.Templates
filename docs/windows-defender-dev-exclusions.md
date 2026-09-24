# Windows Defender exclusions for development work

Real-time protection scans every file a process creates and every executable it launches. For
ordinary use that cost is invisible. For build and test tooling that **launches thousands of
short-lived processes or writes thousands of scratch files** — a test gate that runs `dot` and a
.NET renderer once per case, a large `dotnet build`, a NuGet restore — the scan is paid on every
one, and it can dominate the run.

This runbook excludes development locations from real-time scanning. It is **Windows only**, and
every command here needs an **elevated PowerShell 7** session (Run as Administrator).

> ⚠️ **An exclusion is a hole in protection.** Nothing under an excluded path is scanned — not when
> it is written or run, and not by scheduled or on-demand scans either, which honour the same
> exclusion list. That includes anything a build, a package restore or a download puts there.
> Exclude only locations whose contents you produce or trust, keep the list short, and remove
> entries you no longer need.

## What each kind of exclusion does

| kind | cmdlet parameter | effect |
|---|---|---|
| **path** | `-ExclusionPath` | files in the folder and **every subfolder** are not scanned |
| **process** | `-ExclusionProcess` | files **opened by** that executable are not scanned; the executable itself still is |
| extension | `-ExclusionExtension` | every file with that extension, anywhere — too broad for this purpose; not used here |

A path exclusion covers executables built under that path (a `bin\Debug\…\tool.exe` in the repo),
so launching them is not scanned either.

## Add the exclusions

The source tree — one root for all repositories:

```powershell
Add-MpPreference -ExclusionPath 'C:\c'
```

Scratch directories that tooling writes to **outside** the source tree. A test harness's temp
files usually land under `%TEMP%`; exclude the harness's own subfolder rather than all of `%TEMP%`,
which is where downloads and installers unpack too:

```powershell
Add-MpPreference -ExclusionPath "$env:TEMP\gv-fontsweep"
```

A third-party tool that the tooling launches many times from outside the tree, as a **process**
exclusion (the files it opens), not a path exclusion on its install directory:

```powershell
Add-MpPreference -ExclusionProcess 'C:\Program Files\Graphviz\bin\dot.exe'
```

> ⚠️ Do not add broad interpreters or hosts — `dotnet.exe`, `pwsh.exe`, `python.exe`,
> `node.exe` — as process exclusions. Every file any script opens through them would then go
> unscanned, which is far wider than a source tree.

### Idempotent form

`Add-MpPreference` appends, and adding an entry that already exists is harmless, but a script that
states the intended set and adds only what is missing is easier to review and re-run:

```powershell
#Requires -RunAsAdministrator
$paths = @(
    'C:\c'
    (Join-Path $env:TEMP 'gv-fontsweep')
)
$processes = @(
    'C:\Program Files\Graphviz\bin\dot.exe'
)

$pref = Get-MpPreference
foreach ($p in $paths) {
    if (@($pref.ExclusionPath) -notcontains $p) {
        Add-MpPreference -ExclusionPath $p
        Write-Host "added path    $p"
    }
}
foreach ($p in $processes) {
    if (@($pref.ExclusionProcess) -notcontains $p) {
        Add-MpPreference -ExclusionProcess $p
        Write-Host "added process $p"
    }
}
```

Save it as a `.ps1` and run it with `pwsh -NoProfile -File <script>.ps1` from an elevated
terminal. The `@(...)` around each property keeps the `-notcontains` test correct when the current
list has zero or one entry.

## Verify

```powershell
(Get-MpPreference).ExclusionPath
```

```powershell
(Get-MpPreference).ExclusionProcess
```

Both lists are readable only from an elevated session; a non-elevated one returns the text
`N/A: Must be an administrator to view exclusions` in place of the entries. That is also why the
idempotent script above carries `#Requires -RunAsAdministrator` — non-elevated, it would compare
against that text and re-add every entry.

⚠️ **If an entry you added is missing**, Defender is probably managed by policy (Group Policy,
Intune or another management tool). Local changes do not override a managed configuration; the
exclusion has to be set in the policy instead.

## Measure the effect

Time the same representative workload before and after, on an otherwise idle machine, and compare
wall clock — never assume the gain:

```powershell
Measure-Command { dotnet build --no-incremental } | Select-Object TotalSeconds
```

For a test harness, time one full run of it the same way. Run each side at least twice; the first
run after a change can be slower while caches warm.

## Remove

```powershell
Remove-MpPreference -ExclusionPath 'C:\c'
```

```powershell
Remove-MpPreference -ExclusionPath "$env:TEMP\gv-fontsweep"
```

```powershell
Remove-MpPreference -ExclusionProcess 'C:\Program Files\Graphviz\bin\dot.exe'
```

## The supported alternative: a Dev Drive

Windows 11 offers a **Dev Drive** — a ReFS volume intended for source trees and build output — on
which Defender can run in **performance mode**: files are scanned asynchronously instead of
blocking the process that opened them. That keeps protection on while removing most of the
per-file wait, and it is Microsoft's recommended route for this workload. It requires moving the
source tree onto the new volume, so it is a larger change than an exclusion list.
