#!/usr/bin/env dotnet
// Pre-publication gate for the template package itself: pack it, install it FROM THE PACKED
// .nupkg, generate a repository from EVERY template it ships, assert each generated tree, and
// publish each app for this machine, running the web ones — all before anything is published.
//
// ⛔ Every check here asserts CONTENT, never an exit code. A template package whose content was
// flattened still installs, still lists, and still "generates" — emitting the extracted nupkg
// instead of a project. Each `dotnet` call below would return 0 on that package. The three
// structural checks exist because all three failure modes were reproduced, not imagined:
//
//     .template.config/ dropped by flattening      → content check 1
//     dotfiles excluded by NuGet's default rules   → content check 2 (NU5119)
//     LICENSE packed as a FOLDER, not a file       → content check 3
//
// ⭐ Two modes, and they differ in ONE thing: where the template comes from. Everything after the
// install — generate, assert the tree, build, test, pack, run the packaging guard — is the same
// code running the same assertions. A published check that re-implemented those assertions could
// drift from the local one and start proving something subtly different, and a release is exactly
// when you cannot afford a check that has quietly become a different check.
//
//   default       pack this working tree, assert the packed content, install from that .nupkg.
//                 Run it before tagging.
//   --published   install the id from nuget.org at the given version. Run it AFTER releasing,
//                 because it is the only mode that tests the artifact people will actually get.
//                 Packing locally and calling it "verified" proves the working tree instead.
//
// ⛔ Everything is installed into a template hive of this run's own (`--debug:custom-hive`), inside
// the scratch directory, and nothing is ever uninstalled. An earlier version uninstalled
// Bennewitz.Ninja.Templates before and after each run, which emptied the maintainer's own global
// registration: `dotnet new list bb` found nothing afterwards. A private hive also cannot collide
// with a folder install of the same identity ("Sequence contains more than one matching element").
//
// Usage:  dotnet run --file scripts/verify-release.cs
//         dotnet run --file scripts/verify-release.cs -- --published 2026.3.921
//         dotnet run --file scripts/verify-release.cs -- --keep   (keep the scratch dirs)
//
// ⚠ `--file` is required HERE and not in a generated repository. `dotnet run <file.cs>` binds to
// the project in the working directory when there is one, and this repository root holds
// Bennewitz.Ninja.Templates.csproj — so the bare form tries to run the packaging project and dies
// with "The current OutputType is 'Library'". A generated repository keeps its projects under
// src/ and tests/, so `dotnet run scripts/assert-packages.cs` resolves to the file there, which is
// why its CI says it that way and why this script invokes it that way below.

using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

const string PackageId = "Bennewitz.Ninja.Templates";

// What every generated repository carries, whichever template made it. `{0}` is the stem.
//
// The family conventions (docs/repository-conventions.md) are here: what a repository needs beyond
// code, which the two repositories generated before this list existed went live without.
// repo-conventions.cs checks them in the generated repository's own CI; this makes sure every
// template still ships every one.
string[] common =
[
    ".gitignore",
    ".gitattributes",
    "global.json",
    "NuGet.config",
    "LICENSE",
    "README.md",
    "Directory.Build.props",
    "Directory.Packages.props",
    // Carries the PublicVersion default, which the props file evaluates too early to take.
    "Directory.Build.targets",
    "packages.push",
    "packages.local",
    "{0}.slnx",
    Path.Combine(".github", "workflows", "ci.yml"),
    Path.Combine(".github", "workflows", "release.yml"),
    Path.Combine("src", "{0}", "{0}.csproj"),
    Path.Combine("tests", "{0}.Tests", "{0}.Tests.csproj"),
    "PROGRESS.md",
    "AGENTS.md",
    "CLAUDE.md",
    Path.Combine(".github", "repository.json"),
    Path.Combine(".github", "copilot-instructions.md"),
    Path.Combine("scripts", "repo-conventions.cs"),
    .. ((string[])["src", "tests", "scripts", "docs", ".github"]).SelectMany(directory =>
        (string[])[Path.Combine(directory, "AGENTS.md"), Path.Combine(directory, "CLAUDE.md")]),
];

// What an app carries that a package repository does not: its release runbook, and the test
// settings every app template shares.
string[] app =
[
    Path.Combine("docs", "releasing.md"),
    Path.Combine("tests", "Directory.Build.props"),
];

string[] container = ["Dockerfile", ".dockerignore"];

string[] blazorOnly =
[
    Path.Combine("src", "{0}", "Components", "Counter.razor"),
    Path.Combine("tests", "{0}.Tests", "ComponentTests.cs"),
];

// ⭐ One case per generated repository: every template, and bbweb in both variants. The packed-
// content check below requires the package's templates to be exactly the ones named here, so a
// template added without a case fails this script rather than shipping unverified.
Case[] cases =
[
    new("bbpkg", "bbpkg", "Widget", [], Packs: true,
        Required:
        [
            .. common,
            Path.Combine("scripts", "assert-packages.cs"),
            Path.Combine("docs", "publishing.md"),
            // ⛔ Carries IsTrimmable. Its absence builds, tests and packs perfectly well, and ships
            // every assembly unmarked, so a tree check is the earliest place it can fail loudly.
            Path.Combine("src", "Directory.Build.props"),
        ],
        Forbidden: [], Publish: PublishKind.None),
    new("bbavalonia", "bbavalonia", "Notebook", [], Packs: false,
        Required:
        [
            .. common,
            .. app,
            // The trim-warning baseline and the script that holds the publish to it, in both directions.
            Path.Combine("scripts", "check-trim-warnings.cs"),
            Path.Combine("src", "{0}", "trim-warnings.txt"),
        ],
        Forbidden: [], Publish: PublishKind.Trimmed),
    new("bbweb", "bbweb", "Gallery", [], Packs: false,
        Required: [.. common, .. app, .. container],
        Forbidden: blazorOnly, Publish: PublishKind.SingleFile),
    new("bbweb --blazor", "bbweb", "Gallery", ["--blazor"], Packs: false,
        Required: [.. common, .. app, .. container, .. blazorOnly],
        Forbidden: [], Publish: PublishKind.SingleFile),
    new("bbapi", "bbapi", "Catalog", [], Packs: false,
        Required: [.. common, .. app, .. container],
        Forbidden: [], Publish: PublishKind.Native),
];

// ⭐ Each app is published for the machine running this, the way its own release publishes that
// runtime identifier, and a web app is then started and asked for /healthz and /version. A
// template whose generated repository builds and tests clean can still fail to publish: a trim
// warning the baseline does not hold, a native compile, a single-file site missing its wwwroot.
string hostRid = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;

// Passed to every publish and read back from /version, so a site that reports anything but the
// version it was published with (the "Built with ♥" defect) fails here.
const string PublishedVersion = "2026.3.999";

// No placeholder may survive, in a path or in a file: every template's stem, matched regardless of
// case because the engine also substitutes the lowercase form (docker tags), and the symbols' tokens.
string[] stemTokens = ["PkgStem", "AppStem", "SiteStem", "ApiStem"];
string[] symbolTokens = ["PKG_ID", "REPO_OWNER", "REPO_NAME", "#if (", "#endif"];

bool keep = args.Contains("--keep", StringComparer.OrdinalIgnoreCase);

// ⭐ --published <version> swaps ONLY where the template comes from: nuget.org instead of a local
// pack. Everything after the install is the same code on the same assertions, which is the point.
int publishedFlag = Array.FindIndex(args, argument => string.Equals(argument, "--published", StringComparison.OrdinalIgnoreCase));
string? publishedVersion = publishedFlag >= 0 && publishedFlag + 1 < args.Length ? args[publishedFlag + 1] : null;

if (publishedFlag >= 0 && publishedVersion is null)
{
    return Fail("--published needs a version, e.g. --published 2026.3.921.");
}

string repoRoot = Directory.GetCurrentDirectory();
string packagingProject = Path.Combine(repoRoot, "Bennewitz.Ninja.Templates.csproj");

if (!File.Exists(packagingProject))
{
    return Fail($"No packaging project at '{packagingProject}'. Run this from the repository root.");
}

// The expected owner and repository come from git, never from a constant here: a check that
// hardcodes the answer it is looking for cannot notice when the repository is renamed.
(string? originOwner, string? originRepo) = OriginSlug(repoRoot);

if (originOwner is null || originRepo is null)
{
    return Fail("Could not read an origin remote of the form <owner>/<repo>. This check compares the packed metadata against it.");
}

// The templates this tree ships, from disk: each folder under templates/ with its own
// .template.config/. The cases above must cover every one of them.
string[] templateFolders =
[
    .. Directory.EnumerateDirectories(Path.Combine(repoRoot, "templates"))
        .Where(directory => File.Exists(Path.Combine(directory, ".template.config", "template.json")))
        .Select(Path.GetFileName)
        .Order(StringComparer.Ordinal)!,
];

string[] casedFolders = [.. cases.Select(entry => entry.Template).Distinct().Order(StringComparer.Ordinal)];

if (!templateFolders.SequenceEqual(casedFolders))
{
    return Fail($"templates/ holds [{string.Join(", ", templateFolders)}] but this script generates [{string.Join(", ", casedFolders)}]. " +
        "Every template needs a case here, so that none ships unverified.");
}

string scratch = Path.Combine(Path.GetTempPath(), "bbpkg-verify-" + Guid.NewGuid().ToString("n")[..8]);
string packOutput = Path.Combine(scratch, "packages");
string generateRoot = Path.Combine(scratch, "generated");
string hive = Path.Combine(scratch, "hive");

Directory.CreateDirectory(packOutput);
Directory.CreateDirectory(generateRoot);
Directory.CreateDirectory(hive);

Console.WriteLine($"Scratch: {scratch}");
Console.WriteLine();

string[] hiveArguments = ["--debug:custom-hive", hive];

try
{
    // What `dotnet new install` is pointed at: a freshly packed file, or the published id.
    string installSource;

    if (publishedVersion is not null)
    {
        // ── 1p · The published package, straight off the feed ───────────────────────────────
        // ⚠ Nothing is packed here on purpose. Packing locally and then "verifying the release"
        // proves the working tree, not the artifact somebody will actually install — and those
        // differ exactly when it matters, as the Linux flattening showed.
        Step($"Use the published {PackageId} {publishedVersion}");
        installSource = $"{PackageId}::{publishedVersion}";
        Console.WriteLine($"  {installSource}");
    }
    else
    {

    // ── 1 · Pack the template package ───────────────────────────────────────────────────────
    Step("Pack the template package");

    if (!Dotnet(["pack", packagingProject, "-c", "Release", "--nologo", "--output", packOutput], repoRoot, out string packLog))
    {
        return Fail("dotnet pack failed:" + Environment.NewLine + packLog);
    }

    string[] packed = [.. Directory.EnumerateFiles(packOutput, "*.nupkg", SearchOption.TopDirectoryOnly)
        .Where(path => !path.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase))];

    if (packed.Length != 1)
    {
        return Fail($"Expected exactly one .nupkg, got {packed.Length}: {string.Join(", ", packed.Select(Path.GetFileName))}.");
    }

    string nupkg = packed[0];
    Console.WriteLine($"  packed {Path.GetFileName(nupkg)}");

    // ── 2 · Assert the packed CONTENT, not that pack exited 0 ───────────────────────────────
    Step("Assert the packed content");

    using (ZipArchive archive = ZipFile.OpenRead(nupkg))
    {
        HashSet<string> entries = [.. archive.Entries.Select(entry => entry.FullName)];

        foreach (string folder in templateFolders)
        {
            string contentRoot = $"content/{folder}/";

            // Check 1 — the flattening failure. Without this the package still installs and still
            // "generates", emitting the extracted nupkg rather than a project.
            string templateJson = contentRoot + ".template.config/template.json";

            if (!entries.Contains(templateJson))
            {
                return Fail(
                    $"'{templateJson}' is not in the package. The template content was flattened, which drops " +
                    ".template.config/ — and the resulting package still installs and still 'generates'.");
            }

            // Check 2 — NU5119. NuGet excludes dotfiles by default, so a generated repository would
            // arrive with no .gitignore and no .gitattributes: the files nobody opens.
            foreach (string dotfile in new[] { ".gitignore", ".gitattributes" })
            {
                if (!entries.Contains(contentRoot + dotfile))
                {
                    return Fail($"'{contentRoot + dotfile}' is not in the package. NoDefaultExcludes is what keeps dotfiles in.");
                }
            }

            // Check 3 — the PackagePath failure. A final segment with no extension reads as a FOLDER,
            // so LICENSE packed to content/bbpkg/LICENSE/bbpkg/LICENSE and the real file vanished.
            if (!entries.Contains(contentRoot + "LICENSE"))
            {
                return Fail($"'{contentRoot}LICENSE' is not in the package as a FILE. An extensionless PackagePath segment reads as a folder.");
            }

            string[] licenseAsFolder = [.. entries.Where(entry => entry.StartsWith(contentRoot + "LICENSE/", StringComparison.Ordinal))];

            if (licenseAsFolder.Length > 0)
            {
                return Fail("LICENSE was packed as a FOLDER: " + string.Join(", ", licenseAsFolder));
            }
        }

        // Check 4 — nothing rides along, and no template is missing or extra. Every content entry
        // must sit inside a template: a folder under content/ that carries its own
        // .template.config/template.json. The checks above prove what must be present; only this
        // one notices a file that should not be. It exists because templates/AGENTS.md, this
        // repository's own note about that directory, packed to content/AGENTS.md and passed every
        // other check.
        HashSet<string> templates =
        [
            .. entries
                .Where(entry => entry.StartsWith("content/", StringComparison.Ordinal) &&
                    entry.EndsWith("/.template.config/template.json", StringComparison.Ordinal))
                .Select(entry => entry[..(entry.IndexOf("/.template.config/", StringComparison.Ordinal) + 1)]),
        ];

        string[] packedFolders = [.. templates.Select(root => root["content/".Length..^1]).Order(StringComparer.Ordinal)];

        if (!packedFolders.SequenceEqual(templateFolders))
        {
            return Fail($"The package holds the templates [{string.Join(", ", packedFolders)}], but templates/ holds [{string.Join(", ", templateFolders)}].");
        }

        string[] strays =
        [
            .. entries.Where(entry =>
                entry.StartsWith("content/", StringComparison.Ordinal) &&
                !templates.Any(root => entry.StartsWith(root, StringComparison.Ordinal))),
        ];

        if (strays.Length > 0)
        {
            return Fail("Packed content that belongs to no template: " + string.Join(", ", strays) +
                ". Only a folder with its own .template.config/ should reach content/.");
        }

        Console.WriteLine($"  {entries.Count} entries; {templateFolders.Length} templates ({string.Join(", ", templateFolders)}), each with .template.config, dotfiles and LICENSE; nothing outside a template");

        // The nuspec has to say Template, or `dotnet new install` treats it as an ordinary package.
        XDocument nuspec = ReadNuspec(archive) ?? throw new InvalidOperationException("no .nuspec");

        string? packageType = nuspec.Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "packageType")
            ?.Attribute("name")?.Value;

        if (!string.Equals(packageType, "Template", StringComparison.Ordinal))
        {
            return Fail($"Package type is '{packageType ?? "(none)"}', not 'Template'. `dotnet new install` would not register it.");
        }

        // ⛔ Metadata is permanent too. A package whose repository URL points at a DIFFERENT
        // repository is not a packaging warning — it is a wrong nuget.org listing that cannot be
        // corrected afterwards, only superseded. Compared against git, so a rename cannot rot it.
        string?[] urls =
        [
            nuspec.Descendants().FirstOrDefault(element => element.Name.LocalName == "projectUrl")?.Value,
            nuspec.Descendants().FirstOrDefault(element => element.Name.LocalName == "repository")?.Attribute("url")?.Value,
        ];

        string expected = $"{originOwner}/{originRepo}";
        string[] wrong = [.. urls.Where(url => !string.IsNullOrWhiteSpace(url) && !url!.Contains(expected, StringComparison.OrdinalIgnoreCase))!];

        if (wrong.Length > 0)
        {
            return Fail(
                $"Packed metadata points somewhere other than '{expected}': {string.Join(", ", wrong)}. " +
                "Fix Directory.Build.props before releasing — a published nuspec cannot be corrected.");
        }

        Console.WriteLine($"  package type Template; metadata points at {expected}");
    }

    installSource = nupkg;

    } // end of the local-pack path

    // ── 3 · Install FROM THE PACKAGE, never from the folder, into this run's own hive ─────────
    // Installing from '.' reads the source tree and so cannot see a packaging mistake at all.
    Step("Install the template package into a private hive");

    // ⚠ Retried, and only in the published case. The CLI's own index lags the flat container by
    // minutes after a push, so an immediate miss means "not yet indexed", never "not published".
    // A local .nupkg has no such excuse, so it gets one attempt and a real failure.
    int attempts = publishedVersion is null ? 1 : 20;
    string installLog = string.Empty;
    bool ok = false;

    for (int attempt = 1; attempt <= attempts && !ok; attempt++)
    {
        ok = Dotnet(["new", "install", installSource, .. hiveArguments], repoRoot, out installLog);

        if (!ok && attempt < attempts)
        {
            Thread.Sleep(TimeSpan.FromSeconds(30));
        }
        else if (ok && attempt > 1)
        {
            Console.WriteLine($"  installed on attempt {attempt}");
        }
    }

    if (!ok)
    {
        return Fail($"dotnet new install failed for '{installSource}':" + Environment.NewLine + installLog);
    }

    Console.WriteLine($"  {hive}");

    // ── 4 · Every template, generated and exercised ────────────────────────────────────────
    // ⭐ A failing case does not stop the others: the report names every generated repository that
    // fails, so one run shows the whole damage rather than the first of it.
    List<string> failed = [];

    foreach (Case entry in cases)
    {
        Console.WriteLine();
        Console.WriteLine($"══ {entry.Label} ══");

        string? failure = Verify(entry);

        if (failure is not null)
        {
            Console.Error.WriteLine("::error::" + entry.Label + ": " + failure);
            failed.Add(entry.Label);
        }
    }

    Console.WriteLine();

    if (failed.Count > 0)
    {
        return Fail($"verify-release: FAIL — {failed.Count} of {cases.Length} generated repositories: {string.Join(", ", failed)}.");
    }

    Console.WriteLine(publishedVersion is null
        ? $"verify-release: PASS — packed, installed from the .nupkg, and all {cases.Length} generated repositories ({string.Join(", ", cases.Select(entry => entry.Label))}) build and pass their tests; every app publishes for {hostRid}."
        : $"verify-release: PASS — {PackageId} {publishedVersion} installed FROM NUGET.ORG, and all {cases.Length} generated repositories ({string.Join(", ", cases.Select(entry => entry.Label))}) build and pass their tests; every app publishes for {hostRid}.");

    return 0;
}
finally
{
    // Nothing to uninstall: the hive is inside the scratch directory and goes with it.
    if (keep)
    {
        Console.WriteLine($"Left in place: {scratch}");
    }
    else
    {
        TryDelete(scratch);
    }
}

// Generates one repository and runs everything its own CI would, short of publishing. Returns why it
// failed, or null.
string? Verify(Case entry)
{
    string stem = entry.Stem;
    string generated = Path.Combine(generateRoot, entry.Label.Replace(' ', '_').Replace("--", string.Empty), stem);

    // ── Generate ────────────────────────────────────────────────────────────────────────────
    Step("Generate a repository");

    if (!Dotnet(["new", entry.Template, "-n", stem, "-o", generated, "--RepoOwner", originOwner, .. entry.Arguments, .. hiveArguments], generateRoot, out string generateLog))
    {
        return "dotnet new failed:" + Environment.NewLine + generateLog;
    }

    if (!Directory.Exists(generated))
    {
        return $"Nothing generated at '{generated}'.";
    }

    // ── Assert the generated TREE ───────────────────────────────────────────────────────────
    Step("Assert the generated tree");

    string[] absent = [.. entry.Required.Select(relative => string.Format(relative, stem)).Where(relative => !File.Exists(Path.Combine(generated, relative)))];

    if (absent.Length > 0)
    {
        return "Generated tree is missing: " + string.Join(", ", absent);
    }

    string[] present = [.. entry.Forbidden.Select(relative => string.Format(relative, stem)).Where(relative => File.Exists(Path.Combine(generated, relative)))];

    if (present.Length > 0)
    {
        return "Generated tree carries what this variant must not: " + string.Join(", ", present);
    }

    // The extracted-nupkg tell. These appear when the "template" was really the package itself.
    //
    // ⚠ Matched as whole PATH SEGMENTS. A prefix test on "package" also matches packages.push and
    // packages.local, which are legitimate files every template ships — the check would have
    // failed on a correct tree.
    string[] debrisSegments = ["_rels", "package"];

    string[] packageDebris =
    [
        .. Directory.EnumerateFileSystemEntries(generated, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(generated, path))
            .Where(relative =>
            {
                string[] segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                return segments.Any(segment => debrisSegments.Contains(segment, StringComparer.OrdinalIgnoreCase)) ||
                    segments.Any(segment => segment.Contains("[Content_Types]", StringComparison.OrdinalIgnoreCase)) ||
                    relative.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase);
            }),
    ];

    if (packageDebris.Length > 0)
    {
        return "The generated tree contains package debris, so the nupkg was extracted rather than a template applied: " + string.Join(", ", packageDebris);
    }

    // .template.config is the template's own metadata and must not travel into the generated repo.
    if (Directory.Exists(Path.Combine(generated, ".template.config")))
    {
        return ".template.config/ survived into the generated tree.";
    }

    // This is what proves the substitution actually ran, and every conditional was resolved, rather
    // than the engine having copied the skeleton verbatim.
    List<string> leaks = [];

    foreach (string path in Directory.EnumerateFiles(generated, "*", SearchOption.AllDirectories))
    {
        string relative = Path.GetRelativePath(generated, path);

        if (stemTokens.Any(token => relative.Contains(token, StringComparison.OrdinalIgnoreCase)))
        {
            leaks.Add($"{relative} (path)");
            continue;
        }

        string text = File.ReadAllText(path);

        foreach (string token in stemTokens.Where(token => text.Contains(token, StringComparison.OrdinalIgnoreCase)))
        {
            leaks.Add($"{relative} (contains '{token}')");
        }

        foreach (string token in symbolTokens.Where(token => text.Contains(token, StringComparison.Ordinal)))
        {
            leaks.Add($"{relative} (contains '{token}')");
        }
    }

    if (leaks.Count > 0)
    {
        return "Unsubstituted template placeholders survived: " + string.Join(", ", leaks);
    }

    string csproj = File.ReadAllText(Path.Combine(generated, "src", stem, $"{stem}.csproj"));
    string push = File.ReadAllText(Path.Combine(generated, "packages.push"));
    string expectedId = $"Bennewitz.Ninja.{stem}";

    // The derived names a package template exists to get right, asserted rather than assumed. An
    // app template's push list stays empty: an app is released, not referenced.
    if (entry.Packs)
    {
        if (!csproj.Contains($"<PackageId>{expectedId}</PackageId>", StringComparison.Ordinal))
        {
            return $"Generated csproj does not declare <PackageId>{expectedId}</PackageId>.";
        }

        if (!csproj.Contains($"<AssemblyName>{stem}</AssemblyName>", StringComparison.Ordinal))
        {
            return $"Generated csproj does not declare <AssemblyName>{stem}</AssemblyName>.";
        }

        if (!push.Contains(expectedId, StringComparison.Ordinal))
        {
            return $"packages.push does not list '{expectedId}', so the generated release would push nothing.";
        }

        Console.WriteLine($"  tree complete, no debris, no placeholders, id {expectedId}");
    }
    else
    {
        string[] pushed = [.. push.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith('#'))];

        if (pushed.Length > 0)
        {
            return "packages.push names ids in an app repository, which publishes none: " + string.Join(", ", pushed);
        }

        Console.WriteLine("  tree complete, no debris, no placeholders, nothing to push");
    }

    // ── The generated repository has to actually work ───────────────────────────────────────
    Step(entry.Packs ? "Build, test and pack the generated repository" : "Build and test the generated repository");

    if (!Dotnet(["build", $"{stem}.slnx", "-c", "Release", "--nologo", "-warnaserror"], generated, out string buildLog))
    {
        return "The generated repository does not build:" + Environment.NewLine + buildLog;
    }

    if (!Dotnet(["test", "--solution", $"{stem}.slnx", "--no-build", "-c", "Release"], generated, out string testLog))
    {
        return "The generated repository's tests fail:" + Environment.NewLine + testLog;
    }

    // The counts, so a run that quietly discovered no tests reads as one.
    string[] counts =
    [
        .. testLog.Split('\n').Select(line => line.Trim())
            .Where(line => line.StartsWith("total:", StringComparison.Ordinal) ||
                line.StartsWith("succeeded:", StringComparison.Ordinal) ||
                line.StartsWith("failed:", StringComparison.Ordinal)),
    ];

    Console.WriteLine("  " + (counts.Length > 0 ? string.Join(", ", counts) : LastNonEmptyLines(testLog, 1)));

    if (entry.Packs)
    {
        string generatedPackages = Path.Combine("packages", "Release");

        if (!Dotnet(["pack", $"{stem}.slnx", "-c", "Release", "--no-build", "--nologo", "--output", generatedPackages], generated, out string generatedPackLog))
        {
            return "The generated repository does not pack:" + Environment.NewLine + generatedPackLog;
        }

        // ── Its own guard, run the way its CI runs it ───────────────────────────────────────
        Step("Run the generated repository's packaging guard");

        if (!Dotnet(["run", Path.Combine("scripts", "assert-packages.cs"), "--", generatedPackages], generated, out string assertLog))
        {
            return "assert-packages failed in the generated repository:" + Environment.NewLine + assertLog;
        }

        Console.WriteLine("  " + LastNonEmptyLines(assertLog, 1));
    }

    // ── The family conventions, as its own CI will check them ───────────────────────────────
    // ⭐ The generated repository's OWN copy of repo-conventions.cs, so this also proves the shipped
    // script compiles under a generated repository's warnings-as-errors. --offline, because the
    // repository does not exist on GitHub.
    //
    // A freshly generated repository is SUPPOSED to fail: its description is empty and its documents
    // carry markers until a person replaces them. Anything else that fails, a build property above
    // all, is the template's defect, not the generated repository's.
    Step("Check the generated repository's conventions, offline");

    if (!Git(["init", "-q"], generated, out string gitLog))
    {
        return "git init failed in the generated repository: " + gitLog;
    }

    // A package repository carries the prefix, like its package id; an app's is named after the app.
    string repository = entry.Packs ? expectedId : stem;

    Dotnet(["run", "--file", Path.Combine("scripts", "repo-conventions.cs"), "--",
        "check", "--offline", "--root", generated, "--repo", $"{originOwner}/{repository}"], generated, out string conventionsLog);

    string[] conventionLines =
    [
        .. conventionsLog.Split('\n')
            .Select(line => line.Trim())
            .Select(line => line.StartsWith("::error::", StringComparison.Ordinal) ? line["::error::".Length..] : line),
    ];

    // ⛔ Evidence that the check ran at all: a script that failed to compile prints no FAIL line
    // either, and would otherwise pass this step.
    if (!conventionLines.Any(line => line.StartsWith("NOTE props:", StringComparison.Ordinal) && line.Contains("projects evaluated", StringComparison.Ordinal)))
    {
        return "The conventions check never evaluated the generated repository's projects:" + Environment.NewLine + conventionsLog;
    }

    string[] unexpected =
    [
        .. conventionLines.Where(line => line.StartsWith("FAIL ", StringComparison.Ordinal)
            && !line.StartsWith("FAIL repository.json: \"description\" is empty", StringComparison.Ordinal)
            && !(line.StartsWith("FAIL docs:", StringComparison.Ordinal) && line.Contains("still carries a template marker", StringComparison.Ordinal))),
    ];

    if (unexpected.Length > 0)
    {
        return "The generated repository fails the conventions for reasons that are the template's, not its own:"
            + Environment.NewLine + string.Join(Environment.NewLine, unexpected);
    }

    int markers = conventionLines.Count(line => line.Contains("still carries a template marker", StringComparison.Ordinal));
    Console.WriteLine($"  only the intended gaps: the empty description and {markers} markers; every project's properties conform");

    return entry.Publish == PublishKind.None ? null : Publish(entry, generated);
}

// Publishes one generated app for this machine's runtime identifier, with the arguments its own
// release passes, and proves what came out. Returns why it failed, or null.
string? Publish(Case entry, string generated)
{
    string stem = entry.Stem;
    string project = Path.Combine("src", stem, $"{stem}.csproj");
    string output = Path.Combine(generated, "publish", hostRid);

    Step(entry.Publish switch
    {
        PublishKind.Trimmed => $"Publish trimmed for {hostRid}, against the warning baseline",
        PublishKind.SingleFile => $"Publish single-file for {hostRid}, then run it",
        _ => $"Publish natively for {hostRid}, then run it",
    });

    string[] kind = entry.Publish switch
    {
        PublishKind.Trimmed => ["--self-contained"],
        PublishKind.SingleFile => ["--self-contained", "-p:PublishSingleFile=true", "-p:IncludeNativeLibrariesForSelfExtract=true"],
        // PublishAot is the project's own.
        _ => [],
    };

    // ⚠ On Windows the AOT compiler finds MSVC through vswhere.exe, by its full path, taking the
    // newest Visual Studio or Build Tools with the C++ tools, and then runs that install's
    // VsDevCmd.bat, which changes into the Installer folder and calls `vswhere.exe` BY NAME. Where
    // NoDefaultCurrentDirectoryInExePath is set, as agent shells set it, cmd does not search the
    // current folder and the publish fails with "'vswhere.exe' is not recognized". So the folder goes
    // on PATH for this one child process.
    Dictionary<string, string> environment = [];

    if (entry.Publish == PublishKind.Native && OperatingSystem.IsWindows())
    {
        string installer = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer");

        environment["PATH"] = installer + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
    }

    if (!Dotnet(["publish", project, "-c", "Release", "-r", hostRid, "--nologo", .. kind,
        $"-p:Version={PublishedVersion}", "-p:CommitSha=0000000", "-o", output], generated, out string publishLog, environment))
    {
        return $"The {hostRid} publish fails:" + Environment.NewLine + publishLog;
    }

    if (entry.Publish == PublishKind.Trimmed)
    {
        // The script CI runs, on the log CI would give it: the warnings must equal the baseline,
        // in both directions.
        string logPath = Path.Combine(generated, "trim-publish.log");
        File.WriteAllText(logPath, publishLog);

        if (!Dotnet(["run", "--file", Path.Combine("scripts", "check-trim-warnings.cs"), "--", logPath,
            Path.Combine("src", stem, "trim-warnings.txt")], generated, out string trimLog))
        {
            return "The trimmed publish's warnings differ from the baseline:" + Environment.NewLine + trimLog;
        }

        Console.WriteLine("  " + LastNonEmptyLines(trimLog, 1));
        return null;
    }

    string executable = Path.Combine(output, OperatingSystem.IsWindows() ? stem + ".exe" : stem);

    if (!File.Exists(executable))
    {
        return $"The publish reported success but left no executable at '{executable}'.";
    }

    return Serve(executable, output);
}

// Starts a published web app on a free loopback port, from its own folder as a deployment runs it,
// and checks /healthz answers "ok" and /version names the version it was published with.
static string? Serve(string executable, string folder)
{
    int port;

    using (System.Net.Sockets.TcpListener probe = new(System.Net.IPAddress.Loopback, 0))
    {
        probe.Start();
        port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
    }

    string root = $"http://127.0.0.1:{port}";

    ProcessStartInfo startInfo = new()
    {
        FileName = executable,
        WorkingDirectory = folder,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };

    startInfo.Environment["ASPNETCORE_URLS"] = root;

    StringBuilder captured = new();
    using Process process = new() { StartInfo = startInfo };
    process.OutputDataReceived += (_, e) => { if (e.Data is not null) { lock (captured) { captured.AppendLine(e.Data); } } };
    process.ErrorDataReceived += (_, e) => { if (e.Data is not null) { lock (captured) { captured.AppendLine(e.Data); } } };

    Stopwatch clock = Stopwatch.StartNew();
    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    try
    {
        using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(5) };
        string? health = null;

        while (health is null && clock.Elapsed < TimeSpan.FromSeconds(60) && !process.HasExited)
        {
            try
            {
                using HttpResponseMessage response = client.GetAsync(root + "/healthz").GetAwaiter().GetResult();
                health = $"{(int)response.StatusCode} {response.Content.ReadAsStringAsync().GetAwaiter().GetResult().Trim()}";
            }
            catch (HttpRequestException)
            {
                Thread.Sleep(250);
            }
        }

        long answeredAfter = clock.ElapsedMilliseconds;

        if (health != "200 ok")
        {
            return $"The published app did not answer /healthz with 200 ok ({health ?? (process.HasExited ? $"it exited with {process.ExitCode}" : "no answer in 60 s")}):"
                + Environment.NewLine + captured;
        }

        using HttpResponseMessage versionResponse = client.GetAsync(root + "/version").GetAwaiter().GetResult();
        string version = versionResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        if (!versionResponse.IsSuccessStatusCode || !version.Contains(PublishedVersion, StringComparison.Ordinal))
        {
            return $"/version answered {(int)versionResponse.StatusCode} '{version.Trim()}', not the published version {PublishedVersion}.";
        }

        Console.WriteLine($"  {Path.GetFileName(executable)} answered /healthz in {answeredAfter} ms; /version names {PublishedVersion}");
        return null;
    }
    finally
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }
    }
}

static void Step(string title) => Console.WriteLine($"→ {title}");

static string LastNonEmptyLines(string log, int count) =>
    string.Join(
        Environment.NewLine + "  ",
        log.Split('\n').Select(line => line.TrimEnd('\r', ' ')).Where(line => line.Length > 0).TakeLast(count));

static XDocument? ReadNuspec(ZipArchive archive)
{
    ZipArchiveEntry? entry = archive.Entries.FirstOrDefault(candidate =>
        candidate.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase) &&
        !candidate.FullName.Contains('/'));

    if (entry is null)
    {
        return null;
    }

    using Stream stream = entry.Open();
    return XDocument.Load(stream);
}

// ArgumentList, never a joined string: a path with a space is the normal case on Windows, and a
// hand-quoted command line is where that becomes a bug nobody reproduces on their own machine.
static bool Dotnet(string[] arguments, string workingDirectory, out string log, IReadOnlyDictionary<string, string>? environment = null)
{
    ProcessStartInfo startInfo = new()
    {
        FileName = "dotnet",
        WorkingDirectory = workingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };

    foreach (string argument in arguments)
    {
        startInfo.ArgumentList.Add(argument);
    }

    foreach ((string name, string value) in environment ?? new Dictionary<string, string>())
    {
        startInfo.Environment[name] = value;
    }

    StringBuilder captured = new();

    using Process process = new() { StartInfo = startInfo };

    process.OutputDataReceived += (_, e) => { if (e.Data is not null) { lock (captured) { captured.AppendLine(e.Data); } } };
    process.ErrorDataReceived += (_, e) => { if (e.Data is not null) { lock (captured) { captured.AppendLine(e.Data); } } };

    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    process.WaitForExit();

    log = captured.ToString();

    return process.ExitCode == 0;
}

static (string? Owner, string? Repo) OriginSlug(string repoRoot)
{
    if (!Git(["remote", "get-url", "origin"], repoRoot, out string url))
    {
        return (null, null);
    }

    // Both forms reach the same place: git@github.com:Owner/Repo.git and https://…/Owner/Repo.git
    string trimmed = url.Trim();

    if (trimmed.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
    {
        trimmed = trimmed[..^4];
    }

    string[] segments = trimmed.Split(['/', ':'], StringSplitOptions.RemoveEmptyEntries);

    return segments.Length >= 2
        ? (segments[^2], segments[^1])
        : (null, null);
}

static bool Git(string[] arguments, string workingDirectory, out string log)
{
    ProcessStartInfo startInfo = new()
    {
        FileName = "git",
        WorkingDirectory = workingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };

    foreach (string argument in arguments)
    {
        startInfo.ArgumentList.Add(argument);
    }

    using Process process = new() { StartInfo = startInfo };
    process.Start();

    log = process.StandardOutput.ReadToEnd();
    process.StandardError.ReadToEnd();
    process.WaitForExit();

    return process.ExitCode == 0;
}

static void TryDelete(string directory)
{
    try
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
    catch (IOException)
    {
        Console.WriteLine($"Could not delete {directory}; it is only scratch.");
    }
}

static int Fail(string message)
{
    Console.Error.WriteLine("::error::" + message);
    return 1;
}

// One generated repository: which template, under which stem, with which arguments, and what its
// tree must and must not hold. `{0}` in a path is the stem.
record Case(string Label, string Template, string Stem, string[] Arguments, bool Packs, string[] Required, string[] Forbidden, PublishKind Publish);

// How an app's release publishes it: trimmed and held to its warning baseline (bbavalonia),
// self-contained single-file (bbweb), or compiled natively ahead of time (bbapi).
enum PublishKind { None, Trimmed, SingleFile, Native }
