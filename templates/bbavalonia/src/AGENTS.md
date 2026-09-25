# AGENTS.md — `src/`

The app. `src/AppStem` is an Avalonia desktop app on the Semi theme, with CommunityToolkit.Mvvm
view models and logging through Bennewitz.Ninja.AppServices.

<!-- bbpkg: list the projects under src/ and what each one is, once there is more than the app -->

| Path | What it is |
|---|---|
| `AppStem/Program.cs` | Entry point: logging and the fatal-error hook first, then the Avalonia app; `--smoke` |
| `AppStem/App.axaml` | The theme, Semi.Avalonia, and application resources |
| `AppStem/App.axaml.cs` | Wiring by hand: the main window and its view model, no container |
| `AppStem/Views/` | Windows and views, each with `x:DataType` |
| `AppStem/ViewModels/` | View models, with no reference to a control |
| `AppStem/trim-warnings.txt` | The ILLink warnings a trimmed publish is expected to produce |

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| Every view declares `x:DataType`; bindings are compiled by default | A reflection binding is exactly what the trimmed release breaks without a word | `AvaloniaUseCompiledBindingsByDefault`; the build |
| No `{ReflectionBinding}` and no `x:CompileBindings="False"` | Same reason. If one is truly needed, the type it reaches must be kept from trimming, and a trimmed publish has to prove it works | review; the `trim` job |
| Every interactive control has `AutomationProperties.AutomationId` and `AutomationProperties.Name` | The id is how a test or an agent finds it, the name is what is read out. See `docs/ai-drivable-ui.md` in Bennewitz.Ninja.XamlQuality | `AutomationNameTests` |
| View models reference no control | They stay testable without a UI thread | review |
| The app project stays `IsPackable` `false` | An app is run, not referenced | `PackagingTests` |
| `ILLinkTreatWarningsAsErrors` stays `false`, `TreatWarningsAsErrors` stays `true` | ILLink's warnings are held to the baseline in both directions instead; the compiler's stay errors | `AppStem.csproj`; the `trim` job |

⚠ **The trim analyser in the build cannot see compiled XAML or the dependencies.** Only a trimmed
publish can, which is why CI's `trim` job publishes on every push.
