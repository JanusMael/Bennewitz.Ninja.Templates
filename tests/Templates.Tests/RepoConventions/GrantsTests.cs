using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Templates.Tests.RepoConventions;

/// <summary>
/// The friend-grant convention of <c>scripts/repo-conventions.cs</c> (plans/00006): the <c>grants</c>
/// verb that writes the list, and the checks that hold a repository to it, run over small REAL
/// repositories that MSBuild restores and evaluates.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>The shipped link, not a copy of it.</b> Every fixture's <c>Directory.Build.targets</c> is the
/// bbpkg template's own, so these tests prove the link the templates ship, the file-based-app
/// exclusion included.
/// </para>
/// <para>
/// ⓘ In the same collection as <see cref="RepoConventionsTests"/> and <see cref="PropsTests"/>: all
/// three run the same file-based app, and parallel builds of one file contend for its output.
/// </para>
/// </remarks>
[Collection("repo-conventions")]
public sealed class GrantsTests : IClassFixture<GrantsTests.Runs>
{
    private readonly Runs _runs;

    public GrantsTests(Runs runs) => _runs = runs;

    /// <remarks>The vacuity guard: the check must have evaluated the projects at all.</remarks>
    [Fact]
    public void A_conforming_repository_has_no_grant_finding()
    {
        Assert.Contains("NOTE props:", _runs.Conforming, StringComparison.Ordinal);
        Assert.DoesNotContain(Lines(_runs.Conforming), l => l.StartsWith("FAIL grants", StringComparison.Ordinal));
    }

    /// <remarks>
    /// Every evaluated assembly, by its ASSEMBLY name: <c>Renamed</c> builds <c>Shipped.Name</c>. The
    /// opted-out project is still a grantee.
    /// </remarks>
    [Fact]
    public void Grants_lists_every_assembly_by_its_assembly_name() =>
        Assert.Equal(["Alpha", "Alpha.Tests", "OptedOut", "Shipped.Name"], _runs.Granted);

    [Fact]
    public void Grants_writes_an_empty_External_file_when_there_is_none() =>
        Assert.Contains("wrote AssemblyInfo.InternalsVisibleTo.External.cs", _runs.GrantsOutput, StringComparison.Ordinal);

    [Fact]
    public void A_list_differing_only_in_line_endings_is_current() =>
        Assert.DoesNotContain(Lines(_runs.CrLf), l => l.StartsWith("FAIL grants", StringComparison.Ordinal));

    [Fact]
    public void An_opted_out_project_may_declare_its_own_grant() =>
        Assert.DoesNotContain(Lines(_runs.Conforming), l => l.StartsWith("FAIL grants: OptedOut", StringComparison.Ordinal));

    /// <remarks>
    /// The generator's own source contains the attribute's text, and so does the fixture's script;
    /// neither is compiled by a project, so neither is a grant.
    /// </remarks>
    [Fact]
    public void Text_no_project_compiles_is_not_a_grant() =>
        Assert.DoesNotContain(Lines(_runs.Conforming), l => l.Contains("scripts/tool.cs", StringComparison.Ordinal));

    [Fact]
    public void A_grant_s_text_inside_a_compiled_string_literal_is_not_a_grant() =>
        Assert.DoesNotContain(Lines(_runs.Conforming), l => l.Contains("Sample.cs", StringComparison.Ordinal));

    [Fact]
    public void A_missing_External_file_fails() =>
        Assert.Contains("FAIL grants: AssemblyInfo.InternalsVisibleTo.External.cs is missing", _runs.NoExternal, StringComparison.Ordinal);

    /// <remarks>
    /// ⛔ The reason the link skips file-based apps: linked, a missing grant file would stop the
    /// repository's own scripts compiling, repo-conventions.cs included, so `grants` could not repair it.
    /// </remarks>
    [Fact]
    public void A_file_based_app_still_runs_without_the_External_file() =>
        Assert.True(_runs.ScriptExit == 0, _runs.ScriptOutput);

    [Fact]
    public void A_project_added_without_regenerating_fails() =>
        Assert.Contains("FAIL grants: AssemblyInfo.InternalsVisibleTo.cs is out of date", _runs.Defective, StringComparison.Ordinal);

    [Fact]
    public void A_root_namespace_in_the_External_file_fails() =>
        Assert.Contains("grants \"Bennewitz.Ninja.Alpha\", a root namespace", _runs.Defective, StringComparison.Ordinal);

    [Fact]
    public void A_nearer_targets_file_that_drops_the_link_fails() =>
        Assert.Contains("FAIL grants: Shadowed does not compile AssemblyInfo.InternalsVisibleTo.cs", _runs.Defective, StringComparison.Ordinal);

    [Theory]
    [InlineData("FAIL grants: Stray declares its own grant to \"Somewhere\" in an <InternalsVisibleTo> item")]
    [InlineData("FAIL grants: Attribute declares its own grant to \"Elsewhere\" in an <AssemblyAttribute>")]
    [InlineData("FAIL grants: Source: src/Source/Grant.cs declares [assembly: InternalsVisibleTo]")]
    public void A_grant_a_project_declares_itself_fails_in_every_spelling(string finding) =>
        Assert.Contains(finding, _runs.Defective, StringComparison.Ordinal);

    [Fact]
    public void A_conforming_project_beside_broken_ones_is_not_blamed() =>
        Assert.DoesNotContain(Lines(_runs.Defective), l => l.StartsWith("FAIL grants: Alpha", StringComparison.Ordinal));

    [Fact]
    public void Without_a_solution_every_project_outside_shipped_content_is_listed() =>
        Assert.Equal(["One", "Two"], _runs.Solutionless);

    [Fact]
    public void Grants_refuses_to_write_through_the_API()
    {
        // Straight to the script: Runs.Script would add --root, which is a checkout to write into.
        (int exit, string output) = PropsTests.Runs.Script(PropsTests.Runs.RepoRoot(), "grants", "--repo", "o/r");

        Assert.Equal(2, exit);
        Assert.Contains("grants writes into a checkout", output, StringComparison.Ordinal);
    }

    private static IEnumerable<string> Lines(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Select(l => l.Trim());

    /// <summary>The runs, each over its own repository, shared by every test in the class.</summary>
    public sealed class Runs
    {
        private const string List = "AssemblyInfo.InternalsVisibleTo.cs";
        private const string External = "AssemblyInfo.InternalsVisibleTo.External.cs";

        public Runs()
        {
            string conforming = Fixture(["Alpha", "Alpha.Tests", "Renamed", "OptedOut"], solution: true, content: []);
            try
            {
                (_, GrantsOutput) = Script(conforming, "grants");
                Granted = Names(File.ReadAllText(Path.Combine(conforming, List)));
                Conforming = Check(conforming);

                string list = Path.Combine(conforming, List);
                File.WriteAllText(list, File.ReadAllText(list).Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "\r\n", StringComparison.Ordinal));
                CrLf = Check(conforming);

                File.Delete(Path.Combine(conforming, External));
                (ScriptExit, ScriptOutput) = Execute("dotnet", conforming, "run", "--file", Path.Combine("scripts", "tool.cs"));
                NoExternal = Check(conforming);
            }
            finally
            {
                Directory.Delete(conforming, recursive: true);
            }

            string defective = Fixture(["Alpha", "Stray", "Attribute", "Source", "Shadowed"], solution: true, content: []);
            try
            {
                Script(defective, "grants");
                Write(defective, External, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"Bennewitz.Ninja.Alpha\")]\n");
                AddProject(defective, "Late");
                Defective = Check(defective);
            }
            finally
            {
                Directory.Delete(defective, recursive: true);
            }

            string solutionless = Fixture(["One", "Two"], solution: false, content: ["content"]);
            try
            {
                Write(solutionless, "content/Shipped/Shipped.csproj", Csproj("Shipped"));
                Script(solutionless, "grants");
                Solutionless = Names(File.ReadAllText(Path.Combine(solutionless, List)));
            }
            finally
            {
                Directory.Delete(solutionless, recursive: true);
            }
        }

        public string GrantsOutput { get; }

        public string[] Granted { get; }

        public string Conforming { get; }

        public string CrLf { get; }

        public string NoExternal { get; }

        public int ScriptExit { get; }

        public string ScriptOutput { get; }

        public string Defective { get; }

        public string[] Solutionless { get; }

        private static string Check(string directory) =>
            Script(directory, "check", "--offline", "--root", directory, "--repo", "o/r").Output;

        private static string[] Names(string text) =>
        [
            .. text.Split('\n')
                .Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal))
                .Select(l => l.Trim())
                .Where(l => l.Contains("InternalsVisibleTo(\"", StringComparison.Ordinal))
                .Select(l => l[(l.IndexOf("(\"", StringComparison.Ordinal) + 2)..l.IndexOf("\")", StringComparison.Ordinal)]),
        ];

        private static string Fixture(string[] projects, bool solution, string[] content)
        {
            string directory = Path.Combine(Path.GetTempPath(), "repo-conventions-grants-" + Guid.NewGuid().ToString("N"));
            Write(directory, "NuGet.config",
                """
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                  <packageSources>
                    <clear />
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                  </packageSources>
                </configuration>
                """);
            Write(directory, "Directory.Build.props",
                """
                <Project>
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
                  </PropertyGroup>
                </Project>
                """);
            // ⭐ The template's own targets file, so the fixture links exactly what the templates ship.
            Write(directory, "Directory.Build.targets",
                File.ReadAllText(Path.Combine(PropsTests.Runs.RepoRoot(), "templates", "bbpkg", "Directory.Build.targets")));
            // A file-based app, compiled by no project, whose code contains a grant's text.
            Write(directory, "scripts/tool.cs",
                "System.Console.WriteLine(\"[assembly: InternalsVisibleTo(\\\"NotAGrant\\\")]\".Length);\n");

            if (solution)
            {
                Write(directory, "Fixture.slnx",
                    "<Solution>\n" + string.Concat(projects.Select(p => $"  <Project Path=\"src/{p}/{p}.csproj\" />\n")) + "</Solution>\n");
            }
            foreach (string project in projects)
            {
                Write(directory, $"src/{project}/{project}.csproj", Csproj(project));
                Write(directory, $"src/{project}/Code.cs", $"namespace Fixture;\n\ninternal static class {project.Replace('.', '_')}Code\n{{\n}}\n");
            }
            if (projects.Contains("Alpha"))
            {
                // Compiled, with a grant's text inside a string literal: not a grant. The unanchored
                // scan took it for one, and failed this repository's own conventions job.
                Write(directory, "src/Alpha/Sample.cs",
                    "namespace Fixture;\n\ninternal static class Sample\n{\n    internal const string Text = \"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\\\"X\\\")]\";\n}\n");
            }
            if (projects.Contains("Source"))
            {
                Write(directory, "src/Source/Grant.cs", "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"Elsewhere\")]\n");
            }
            if (projects.Contains("Shadowed"))
            {
                Write(directory, "src/Shadowed/Directory.Build.targets", "<Project>\n</Project>\n");
            }

            JsonObject config = new()
            {
                ["description"] = "Fixture.",
                ["topics"] = new JsonArray("csharp", "dotnet"),
                ["requiredChecks"] = new JsonArray(),
                ["content"] = new JsonArray([.. content.Select(c => (JsonNode)JsonValue.Create(c)!)]),
            };
            Write(directory, ".github/repository.json", config.ToJsonString());

            (int initExit, string initOutput) = Execute("git", directory, "init", "-q");
            Assert.True(initExit == 0, initOutput);
            return directory;
        }

        private static void AddProject(string directory, string project)
        {
            Write(directory, $"src/{project}/{project}.csproj", Csproj(project));
            string slnx = Path.Combine(directory, "Fixture.slnx");
            File.WriteAllText(slnx, File.ReadAllText(slnx).Replace("</Solution>", $"  <Project Path=\"src/{project}/{project}.csproj\" />\n</Solution>", StringComparison.Ordinal));
        }

        private static string Csproj(string project) => project switch
        {
            "Alpha.Tests" => Sdk("<IsPackable>false</IsPackable><IsTestProject>true</IsTestProject><OutputType>Exe</OutputType>"),
            "Renamed" => Sdk("<AssemblyName>Shipped.Name</AssemblyName>"),
            // Opted out, with a grant of its own: allowed, and it is still a grantee.
            "OptedOut" => Sdk("<SolutionFriendGrants>false</SolutionFriendGrants>", "<ItemGroup><InternalsVisibleTo Include=\"Special\" /></ItemGroup>"),
            "Stray" => Sdk("", "<ItemGroup><InternalsVisibleTo Include=\"Somewhere\" /></ItemGroup>"),
            "Attribute" => Sdk("",
                "<ItemGroup><AssemblyAttribute Include=\"System.Runtime.CompilerServices.InternalsVisibleTo\"><_Parameter1>Elsewhere</_Parameter1></AssemblyAttribute></ItemGroup>"),
            _ => Sdk(""),
        };

        private static string Sdk(string properties, string items = "") =>
            $"<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>{properties}</PropertyGroup>\n  {items}\n</Project>\n";

        private static void Write(string root, string relative, string text)
        {
            string path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
        }

        public static (int ExitCode, string Output) Script(string workingDirectory, params string[] arguments) =>
            PropsTests.Runs.Script(PropsTests.Runs.RepoRoot(), [.. arguments, .. arguments.Contains("--root") ? [] : (string[])["--root", workingDirectory]]);

        private static (int ExitCode, string Output) Execute(string file, string workingDirectory, params string[] arguments)
        {
            ProcessStartInfo start = new(file) { WorkingDirectory = workingDirectory, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (string argument in arguments)
            {
                start.ArgumentList.Add(argument);
            }
            using Process process = Process.Start(start)!;
            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            return (process.ExitCode, stdout.Result + stderr.Result);
        }
    }
}
