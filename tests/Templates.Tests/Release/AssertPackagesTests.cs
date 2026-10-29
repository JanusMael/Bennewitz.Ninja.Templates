using System.IO.Compression;
using System.Text.Json.Nodes;

namespace Templates.Tests.Release;

/// <summary>
/// <c>templates/bbpkg/scripts/assert-packages.cs</c>, the last gate before a push, over packages built
/// here: each a <c>.nupkg</c> holding only its <c>.nuspec</c>, which is all the guard reads.
/// </summary>
/// <remarks>
/// ⓘ The file names deliberately disagree with the ids inside, so a guard that read ids from file
/// names, which it must never do, would fail these tests.
/// </remarks>
public sealed class AssertPackagesTests
{
    [Fact]
    public void A_family_pack_that_matches_the_family_passes()
    {
        using Packed packed = new("Fixture.Gadget", "Fixture.Gizmo");

        (int exit, string output) = packed.Assert("--family", "gadgets");

        Assert.True(exit == 0, output);
        Assert.Contains("Packages check out: 2 packed, all declared in the gadgets family in .github/repository.json.", output, StringComparison.Ordinal);
    }

    /// <summary>Another family's package is not this release's, though packages.push names it.</summary>
    [Fact]
    public void A_family_pack_with_an_extra_id_fails()
    {
        using Packed packed = new("Fixture.Gadget", "Fixture.Gizmo", "Fixture.Widget");

        (int exit, string output) = packed.Assert("--family", "gadgets");

        Assert.Equal(1, exit);
        Assert.Contains("Packed but not in the gadgets family in .github/repository.json: Fixture.Widget.", output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_family_pack_with_a_missing_id_fails()
    {
        using Packed packed = new("Fixture.Gadget");

        (int exit, string output) = packed.Assert("--family", "gadgets");

        Assert.Equal(1, exit);
        Assert.Contains("Declared in the gadgets family in .github/repository.json but not packed: Fixture.Gizmo.", output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_family_the_repository_does_not_declare_fails()
    {
        using Packed packed = new("Fixture.Gadget");

        (int exit, string output) = packed.Assert("--family", "gizmos");

        Assert.Equal(1, exit);
        Assert.Contains("--family gizmos, which .github/repository.json does not declare; it declares widgets, gadgets.", output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_family_where_the_repository_declares_none_fails()
    {
        using Packed packed = new(families: false, "Fixture.Gadget");

        (int exit, string output) = packed.Assert("--family", "gadgets");

        Assert.Equal(1, exit);
        Assert.Contains("--family gadgets, but .github/repository.json declares no families.", output, StringComparison.Ordinal);
    }

    /// <summary>A family's packages carry its README, docs/&lt;stem&gt;.md, which nuget.org shows (plans/00011).</summary>
    [Fact]
    public void A_family_pack_says_its_packages_carry_the_family_s_README()
    {
        using Packed packed = new("Fixture.Gadget", "Fixture.Gizmo");

        (int exit, string output) = packed.Assert("--family", "gadgets");

        Assert.True(exit == 0, output);
        Assert.Contains("Each carries docs/gadgets.md as its README.md.", output, StringComparison.Ordinal);
    }

    /// <remarks>What a family package packs when its project names no FamilyReadme.</remarks>
    [Fact]
    public void A_family_pack_carrying_the_repository_s_README_fails()
    {
        using Packed packed = new(families: true, new Dictionary<string, string?> { ["Fixture.Gizmo"] = "README.md" }, "Fixture.Gadget", "Fixture.Gizmo");

        (int exit, string output) = packed.Assert("--family", "gadgets");

        Assert.Equal(1, exit);
        Assert.Contains("The gadgets family's packages carry docs/gadgets.md as their README.md, byte for byte: Fixture.Gizmo's is another file.", output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_family_pack_without_a_README_fails()
    {
        using Packed packed = new(families: true, new Dictionary<string, string?> { ["Fixture.Gadget"] = null }, "Fixture.Gadget", "Fixture.Gizmo");

        (int exit, string output) = packed.Assert("--family", "gadgets");

        Assert.Equal(1, exit);
        Assert.Contains("byte for byte: Fixture.Gadget packs none.", output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_family_without_its_README_document_fails()
    {
        using Packed packed = new("Fixture.Gadget", "Fixture.Gizmo");
        packed.Delete("docs/gadgets.md");

        (int exit, string output) = packed.Assert("--family", "gadgets");

        Assert.Equal(1, exit);
        Assert.Contains("docs/gadgets.md is missing: it is the gadgets family's README, which each of its packages carries.", output, StringComparison.Ordinal);
    }

    /// <summary>Without a family the guard is as it was: everything packed is in one of the two lists, and everything listed is packed.</summary>
    /// <remarks>Without a family, no README is compared (plans/00011 decision 5).</remarks>
    [Fact]
    public void Without_a_family_the_whole_pack_matches_the_two_lists()
    {
        using Packed packed = new("Fixture.Widget", "Fixture.Gadget", "Fixture.Gizmo", "Fixture.Private");

        (int exit, string output) = packed.Assert();

        Assert.True(exit == 0, output);
        Assert.Contains("Packages check out: 4 packed, all declared in packages.push or packages.local.", output, StringComparison.Ordinal);
        Assert.DoesNotContain("README", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_a_family_an_undeclared_id_fails()
    {
        using Packed packed = new("Fixture.Widget", "Fixture.Gadget", "Fixture.Gizmo", "Fixture.Private", "Fixture.Stray");

        (int exit, string output) = packed.Assert();

        Assert.Equal(1, exit);
        Assert.Contains("Packed but declared nowhere: Fixture.Stray.", output, StringComparison.Ordinal);
    }

    /// <summary>
    /// A repository with two families, each with its README, a root README, packages.push and
    /// packages.local, and a folder of packages, each carrying its family's README unless told otherwise.
    /// </summary>
    private sealed class Packed : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "assert-packages-" + Guid.NewGuid().ToString("N"));

        public Packed(params string[] ids)
            : this(families: true, ids)
        {
        }

        public Packed(bool families, params string[] ids)
            : this(families, new Dictionary<string, string?>(), ids)
        {
        }

        /// <param name="readmes">The file a package carries as its README, by id; null for none.</param>
        public Packed(bool families, IReadOnlyDictionary<string, string?> readmes, params string[] ids)
        {
            JsonObject config = new() { ["description"] = "Fixture." };
            if (families)
            {
                config["families"] = new JsonObject
                {
                    ["widgets"] = new JsonArray("Fixture.Widget"),
                    ["gadgets"] = new JsonArray("Fixture.Gadget", "Fixture.Gizmo"),
                };
            }
            Write(".github/repository.json", config.ToJsonString());
            Write("packages.push", "Fixture.Widget\nFixture.Gadget\nFixture.Gizmo\n");
            Write("packages.local", "# Packed, never published.\nFixture.Private\n");
            Write("README.md", "# The repository\n\nHow to build it, and how to release it.\n");
            Write("docs/widgets.md", "# Widgets\n\nWhat a widget is.\n");
            Write("docs/gadgets.md", "# Gadgets\n\nWhat a gadget is, and a gizmo.\n");

            string packages = Path.Combine(_directory, "packages", "Release");
            Directory.CreateDirectory(packages);
            for (int i = 0; i < ids.Length; i++)
            {
                string? readme = readmes.TryGetValue(ids[i], out string? chosen) ? chosen : DefaultReadme(families, ids[i]);
                using ZipArchive archive = ZipFile.Open(Path.Combine(packages, $"package{i}.2026.4.1022.nupkg"), ZipArchiveMode.Create);
                ZipArchiveEntry nuspec = archive.CreateEntry($"package{i}.nuspec");
                using (StreamWriter writer = new(nuspec.Open()))
                {
                    string element = readme is null ? "" : "<readme>README.md</readme>";
                    writer.Write($"<?xml version=\"1.0\"?>\n<package xmlns=\"http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd\"><metadata><id>{ids[i]}</id><version>2026.4.1022</version>{element}</metadata></package>\n");
                }
                if (readme is not null)
                {
                    using Stream entry = archive.CreateEntry("README.md").Open();
                    entry.Write(File.ReadAllBytes(Path.Combine(_directory, readme.Replace('/', Path.DirectorySeparatorChar))));
                }
            }
        }

        public (int ExitCode, string Output) Assert(params string[] arguments) =>
            ShippedScript.AssertPackages.Run(_directory, [Path.Combine("packages", "Release"), .. arguments]);

        public void Delete(string relative) => File.Delete(Path.Combine(_directory, relative.Replace('/', Path.DirectorySeparatorChar)));

        // What the root props pack: a family's README for a family's package, the root's otherwise.
        private static string DefaultReadme(bool families, string id) => (families, id) switch
        {
            (true, "Fixture.Widget") => "docs/widgets.md",
            (true, "Fixture.Gadget" or "Fixture.Gizmo") => "docs/gadgets.md",
            _ => "README.md",
        };

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

        private void Write(string relative, string text)
        {
            string path = Path.Combine(_directory, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
        }
    }
}
