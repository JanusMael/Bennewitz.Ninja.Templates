using System.IO.Compression;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Templates.Tests.RepoConventions;

namespace Templates.Tests.Release;

/// <summary>
/// Two families packed for real by <c>dotnet pack</c>, through <c>bbpkg</c>'s root
/// <c>Directory.Build.props</c> (plans/00011 decision 1): each family's package carries its
/// <c>docs/&lt;stem&gt;.md</c> as <c>README.md</c>, and a package that names no <c>FamilyReadme</c> the
/// root <c>README.md</c>.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ <b>Nothing else packs through the item.</b> The guard's other tests read packages made by hand,
/// <c>verify-release</c>'s <c>bbpkg</c> has no families, and Inspections keeps its own props.
/// </para>
/// <para>
/// ⓘ One pack, shared by every test: a restore and three packs are the slow part. The template's build
/// files are copied in unchanged; its placeholders are only text there.
/// </para>
/// </remarks>
public sealed class FamilyPackTests : IClassFixture<FamilyPackTests.Pack>
{
    private readonly Pack _pack;

    public FamilyPackTests(Pack pack) => _pack = pack;

    [Theory]
    [InlineData("Fixture.Alpha", "docs/alpha.md")]
    [InlineData("Fixture.Beta", "docs/beta.md")]
    [InlineData("Fixture.Plain", "README.md")]
    public void Each_package_carries_its_README(string id, string readme) =>
        Assert.Equal(_pack.Source(readme), _pack.Readme(id));

    /// <summary>The guard reads what <c>dotnet pack</c> made as it reads the packages made by hand.</summary>
    [Theory]
    [InlineData("alpha", "Fixture.Alpha")]
    [InlineData("beta", "Fixture.Beta")]
    public void The_guard_passes_each_family_s_real_pack(string family, string id)
    {
        (int exit, string output) = _pack.Guard(family, id);

        Assert.True(exit == 0, output);
        Assert.Contains($"Each carries docs/{family}.md as its README.md.", output, StringComparison.Ordinal);
    }

    /// <summary>
    /// A package that names no <c>FamilyReadme</c> packs the root README, a nuget.org page like any
    /// other (plans/00012): its relative link fails, and the families' absolute READMEs do not.
    /// </summary>
    [Fact]
    public void The_guard_fails_the_root_README_s_relative_link_in_a_real_pack()
    {
        (int exit, string output) = _pack.GuardAll();

        Assert.Equal(1, exit);
        string error = Assert.Single(output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'), line => line.Contains(", relatively.", StringComparison.Ordinal));
        Assert.StartsWith("::error::Fixture.Plain's README, README.md:3, links to \"docs/publishing.md\", relatively.", error, StringComparison.Ordinal);
    }

    /// <summary>The repository, packed once.</summary>
    public sealed class Pack : IDisposable
    {
        private static readonly string[] TemplateFiles =
        [
            "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "NuGet.config", "global.json",
            "AssemblyInfo.InternalsVisibleTo.cs", "AssemblyInfo.InternalsVisibleTo.External.cs", "src/Directory.Build.props",
        ];

        private readonly string _directory = Path.Combine(Path.GetTempPath(), "family-pack-" + Guid.NewGuid().ToString("N"));

        public Pack()
        {
            string template = Path.Combine(ConventionsScript.RepoRoot(), "templates", "bbpkg");
            foreach (string file in TemplateFiles)
            {
                string target = Local(file);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(Path.Combine(template, file.Replace('/', Path.DirectorySeparatorChar)), target);
            }

            Write("README.md", "# The repository\n\nHow to build it, and [how to release it](docs/publishing.md).\n");
            Write("packages.push", "Fixture.Alpha\nFixture.Beta\nFixture.Plain\n");
            Write("docs/alpha.md", "# Alpha\n\nWhat the alpha family does.\n");
            Write("docs/beta.md", "# Beta\n\nWhat the beta family does, at more length.\n");
            Write(".github/repository.json", new JsonObject
            {
                ["description"] = "Fixture.",
                ["families"] = new JsonObject
                {
                    ["alpha"] = new JsonArray("Fixture.Alpha"),
                    ["beta"] = new JsonArray("Fixture.Beta"),
                },
            }.ToJsonString());
            Write("src/Alpha/Alpha.csproj", Csproj("Fixture.Alpha", "<FamilyReadme>docs/alpha.md</FamilyReadme><AssemblyProduct>Alpha</AssemblyProduct>"));
            Write("src/Beta/Beta.csproj", Csproj("Fixture.Beta", "<FamilyReadme>docs/beta.md</FamilyReadme><AssemblyProduct>Beta</AssemblyProduct>"));
            Write("src/Plain/Plain.csproj", Csproj("Fixture.Plain", ""));
            Write("Fixture.slnx", "<Solution>\n  <Project Path=\"src/Alpha/Alpha.csproj\" />\n  <Project Path=\"src/Beta/Beta.csproj\" />\n  <Project Path=\"src/Plain/Plain.csproj\" />\n</Solution>\n");

            (int exit, string output) = ConventionsScript.Execute("dotnet", _directory, "pack", "Fixture.slnx", "-c", "Release", "-o", "packed");
            Assert.True(exit == 0, "dotnet pack failed:\n" + output);
        }

        public byte[] Source(string relative) => File.ReadAllBytes(Local(relative));

        /// <summary>The README the package's nuspec names, as packed.</summary>
        public byte[] Readme(string id)
        {
            using ZipArchive archive = ZipFile.OpenRead(Package(id));
            ZipArchiveEntry nuspec = Assert.Single(archive.Entries, e => e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase) && !e.FullName.Contains('/'));
            string path;
            using (Stream stream = nuspec.Open())
            {
                path = XDocument.Load(stream).Descendants().Single(e => e.Name.LocalName == "readme").Value.Trim().Replace('\\', '/');
            }
            ZipArchiveEntry readme = Assert.Single(archive.Entries, e => e.FullName == path);
            using MemoryStream bytes = new();
            using (Stream stream = readme.Open())
            {
                stream.CopyTo(bytes);
            }
            return bytes.ToArray();
        }

        /// <summary>The guard over a folder holding only this family's package, as a family's release packs.</summary>
        public (int ExitCode, string Output) Guard(string family, string id)
        {
            string folder = Path.Combine(_directory, "release-" + family);
            Directory.CreateDirectory(folder);
            File.Copy(Package(id), Path.Combine(folder, Path.GetFileName(Package(id))), overwrite: true);
            return ShippedScript.AssertPackages.Run(_directory, folder, "--family", family);
        }

        /// <summary>The guard over the whole pack, as CI's <c>pack</c> job runs it.</summary>
        public (int ExitCode, string Output) GuardAll() =>
            ShippedScript.AssertPackages.Run(_directory, Path.Combine(_directory, "packed"));

        public void Dispose()
        {
            try
            {
                Directory.Delete(_directory, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private string Package(string id) =>
            Assert.Single(Directory.GetFiles(Path.Combine(_directory, "packed"), id + ".*.nupkg"),
                p => !p.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase));

        private static string Csproj(string id, string properties) =>
            $"<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><IsPackable>true</IsPackable><PackageId>{id}</PackageId>{properties}</PropertyGroup>\n</Project>\n";

        private string Local(string relative) => Path.Combine(_directory, relative.Replace('/', Path.DirectorySeparatorChar));

        private void Write(string relative, string text)
        {
            string path = Local(relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
        }
    }
}
