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

    /// <summary>Without a family the guard is as it was: everything packed is in one of the two lists, and everything listed is packed.</summary>
    [Fact]
    public void Without_a_family_the_whole_pack_matches_the_two_lists()
    {
        using Packed packed = new("Fixture.Widget", "Fixture.Gadget", "Fixture.Gizmo", "Fixture.Private");

        (int exit, string output) = packed.Assert();

        Assert.True(exit == 0, output);
        Assert.Contains("Packages check out: 4 packed, all declared in packages.push or packages.local.", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_a_family_an_undeclared_id_fails()
    {
        using Packed packed = new("Fixture.Widget", "Fixture.Gadget", "Fixture.Gizmo", "Fixture.Private", "Fixture.Stray");

        (int exit, string output) = packed.Assert();

        Assert.Equal(1, exit);
        Assert.Contains("Packed but declared nowhere: Fixture.Stray.", output, StringComparison.Ordinal);
    }

    /// <summary>A repository with two families, packages.push and packages.local, and a folder of packages.</summary>
    private sealed class Packed : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "assert-packages-" + Guid.NewGuid().ToString("N"));

        public Packed(params string[] ids)
            : this(families: true, ids)
        {
        }

        public Packed(bool families, params string[] ids)
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

            string packages = Path.Combine(_directory, "packages", "Release");
            Directory.CreateDirectory(packages);
            for (int i = 0; i < ids.Length; i++)
            {
                using ZipArchive archive = ZipFile.Open(Path.Combine(packages, $"package{i}.2026.4.1022.nupkg"), ZipArchiveMode.Create);
                ZipArchiveEntry nuspec = archive.CreateEntry($"package{i}.nuspec");
                using StreamWriter writer = new(nuspec.Open());
                writer.Write($"<?xml version=\"1.0\"?>\n<package xmlns=\"http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd\"><metadata><id>{ids[i]}</id><version>2026.4.1022</version></metadata></package>\n");
            }
        }

        public (int ExitCode, string Output) Assert(params string[] arguments) =>
            ShippedScript.AssertPackages.Run(_directory, [Path.Combine("packages", "Release"), .. arguments]);

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
