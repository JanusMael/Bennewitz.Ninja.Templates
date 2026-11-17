using System.Text.Json.Nodes;
using Templates.Tests.RepoConventions;

namespace Templates.Tests.Reference;

/// <summary>
/// <c>reference/fetch-reference.cs</c>, the family's kit for reading upstream sources (plans/00013
/// decision 5), run as a person runs it over a repository built in a temp directory, fetching from a
/// bare repository beside it through a <c>file://</c> url: the states <c>missing</c>, <c>fetched</c>,
/// <c>present</c> and <c>off-pin</c>, the exit codes 0, 1 and 2, a checkout linked in as a symlink, and
/// the two rules on where checkouts go: under <c>reference/src/</c>, which git must ignore.
/// </summary>
/// <remarks>
/// <para>
/// ⓘ <b>The long-path gate is not exercised here.</b> It reads Windows' registry, which a test cannot
/// change, and every machine that runs this has long paths on, or is not Windows.
/// </para>
/// </remarks>
/// <remarks>
/// <para>
/// ⭐ <b>No network and no package.</b> A source pinned by a <c>ref</c> reads no nuspec and no feed, so
/// the run is the same on every runner. CI runs it on Ubuntu, a person on Windows; a run on macOS is
/// recorded in <c>PROGRESS.md</c> before the kit is released.
/// </para>
/// <para>
/// ⛔ <b>The script is copied into the repository it fetches for</b>, with its three shields, as every
/// repository keeps it. It looks for its root from its own folder before the current directory, so run
/// from this repository's <c>reference/</c> it would fetch into this repository.
/// </para>
/// <para>
/// ⓘ <b>One fixture runs every step in order</b>, since each state follows from the one before, and the
/// tests assert on what it recorded. Building the script is the slow part, and one class runs it.
/// </para>
/// </remarks>
public sealed class FetchReferenceTests : IClassFixture<FetchReferenceTests.Run>
{
    private readonly Run _run;

    public FetchReferenceTests(Run run) => _run = run;

    [Fact]
    public void Status_reports_a_source_never_fetched_as_missing_fetches_nothing_and_exits_1()
    {
        Step step = _run.StatusBeforeFetch;

        Assert.True(step.Exit == 1, step.Output);
        Assert.Equal("missing", step.StateOf(Run.Name));
        Assert.Null(step.Checkout);
    }

    [Fact]
    public void A_plain_run_fetches_a_missing_source_at_its_ref_and_exits_0()
    {
        Step step = _run.FirstFetch;

        Assert.True(step.Exit == 0, step.Output);
        Assert.Equal("fetched", step.StateOf(Run.Name));
        Assert.Equal("one\n", step.Checkout);
    }

    [Fact]
    public void Git_ignores_the_checkout_and_still_sees_the_kit_beside_it()
    {
        Assert.Equal("one\n", _run.FirstFetch.Checkout);
        string[] untracked = [.. ConventionsScript.Lines(_run.UntrackedAfterFetch).Where(line => line.Length > 0)];

        Assert.Contains("?? reference/sources.json", untracked);
        Assert.DoesNotContain(untracked, line => line.Contains("reference/src", StringComparison.Ordinal));
    }

    [Fact]
    public void A_checkout_linked_in_as_a_symlink_is_present_and_ignored()
    {
        Assert.SkipWhen(_run.Symlinked is null, $"This machine cannot make a directory symlink: {_run.SymlinkProblem}");
        Step step = _run.Symlinked!;

        Assert.True(step.Exit == 0, step.Output);
        Assert.Equal("present", step.StateOf(Run.Name));
        Assert.Equal("two\n", step.Checkout);
        Assert.DoesNotContain(ConventionsScript.Lines(_run.UntrackedWhenSymlinked), line => line.Contains("reference/src", StringComparison.Ordinal));
    }

    [Fact]
    public void A_dir_outside_reference_src_exits_2()
    {
        Step step = _run.OutsideSrc;

        Assert.True(step.Exit == 2, step.Output);
        Assert.Contains("is not a folder under reference/src/", step.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Status_reports_a_checkout_at_its_ref_as_present_and_exits_0()
    {
        Step step = _run.StatusAfterFetch;

        Assert.True(step.Exit == 0, step.Output);
        Assert.Equal("present", step.StateOf(Run.Name));
    }

    [Fact]
    public void Status_reports_a_checkout_whose_ref_changed_as_off_pin_and_exits_1()
    {
        Step step = _run.StatusOffPin;

        Assert.True(step.Exit == 1, step.Output);
        Assert.Equal("off-pin", step.StateOf(Run.Name));
        Assert.Contains("run with --update", step.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_plain_run_leaves_an_off_pin_checkout_as_it_is_and_exits_1()
    {
        Step step = _run.PlainRunOffPin;

        Assert.True(step.Exit == 1, step.Output);
        Assert.Equal("off-pin", step.StateOf(Run.Name));
        Assert.Equal("one\n", step.Checkout);
    }

    [Fact]
    public void Update_fetches_an_off_pin_checkout_again_at_its_ref_and_exits_0()
    {
        Step step = _run.Update;

        Assert.True(step.Exit == 0, step.Output);
        Assert.Equal("fetched", step.StateOf(Run.Name));
        Assert.Equal("two\n", step.Checkout);

        Assert.True(_run.StatusAfterUpdate.Exit == 0, _run.StatusAfterUpdate.Output);
        Assert.Equal("present", _run.StatusAfterUpdate.StateOf(Run.Name));
    }

    [Fact]
    public void Without_the_ignore_line_the_script_exits_2_and_fetches_nothing()
    {
        Step step = _run.UpdateWithoutIgnore;

        Assert.True(step.Exit == 2, step.Output);
        Assert.Contains("git does not ignore reference/src", step.Output, StringComparison.Ordinal);
        Assert.Equal("two\n", step.Checkout);
    }

    /// <summary>One run of the script: its exit code, its output, and the checkout's file after it, if any.</summary>
    public sealed record Step(int Exit, string Output, string? Checkout)
    {
        /// <summary>The state the report line for <paramref name="name"/> gives, or null when there is none.</summary>
        public string? StateOf(string name) =>
            ConventionsScript.Lines(Output)
                .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Where(words => words.Length >= 2 && words[0] == name)
                .Select(words => words[1])
                .FirstOrDefault();
    }

    /// <summary>
    /// An upstream with two tagged commits, as a bare repository, and a repository that keeps the kit and
    /// fetches it: every step run once, in order, and recorded.
    /// </summary>
    public sealed class Run : IDisposable
    {
        public const string Name = "upstream";

        private const string Identity = "user.name=Fixture";

        private const string Email = "user.email=fixture@example.com";

        private readonly string _scratch = Path.Combine(Path.GetTempPath(), "fetch-reference-" + Guid.NewGuid().ToString("N"));

        private readonly string _repository;

        private readonly string _url;

        public Run()
        {
            Directory.CreateDirectory(_scratch);
            _url = Upstream();

            _repository = Path.Combine(_scratch, "repository");
            Directory.CreateDirectory(Path.Combine(_repository, "reference"));
            Git(_repository, "init", "-q");
            string kit = Path.Combine(ConventionsScript.RepoRoot(), "reference");
            foreach (string file in (string[])["fetch-reference.cs", "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props"])
            {
                File.Copy(Path.Combine(kit, file), Path.Combine(_repository, "reference", file));
            }
            File.WriteAllText(Path.Combine(_repository, ".gitignore"), "bin/\nobj/\n/reference/src\n");
            Manifest("v1");

            StatusBeforeFetch = Fetch("--status");
            FirstFetch = Fetch();
            UntrackedAfterFetch = Git(_repository, "status", "--porcelain", "--untracked-files=all");
            StatusAfterFetch = Fetch("--status");

            Manifest("v2");
            StatusOffPin = Fetch("--status");
            PlainRunOffPin = Fetch();
            Update = Fetch("--update");
            StatusAfterUpdate = Fetch("--status");

            // The checkout moved elsewhere and linked back in. Git refuses to look through a symlink, so
            // a gate that asked about a path inside the checkout would fail here.
            string checkout = Path.Combine(_repository, "reference", "src", Name);
            string elsewhere = Path.Combine(_scratch, "elsewhere");
            Directory.Move(checkout, elsewhere);
            try
            {
                Directory.CreateSymbolicLink(checkout, elsewhere);
                Symlinked = Fetch("--status");
                UntrackedWhenSymlinked = Git(_repository, "status", "--porcelain", "--untracked-files=all");
                Directory.Delete(checkout);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                SymlinkProblem = error.Message;
            }
            Directory.Move(elsewhere, checkout);

            Manifest("v2", "reference/" + Name);
            OutsideSrc = Fetch("--status");

            // Off its pin again, so a run that ignored the missing line would fetch.
            Manifest("v1");
            File.WriteAllText(Path.Combine(_repository, ".gitignore"), "bin/\nobj/\n");
            UpdateWithoutIgnore = Fetch("--update");
        }

        public Step StatusBeforeFetch { get; }

        public Step FirstFetch { get; }

        public string UntrackedAfterFetch { get; }

        public Step StatusAfterFetch { get; }

        public Step StatusOffPin { get; }

        public Step PlainRunOffPin { get; }

        public Step Update { get; }

        public Step StatusAfterUpdate { get; }

        /// <summary><c>--status</c> with the checkout a symlink, or null when this machine cannot make one.</summary>
        public Step? Symlinked { get; }

        public string UntrackedWhenSymlinked { get; } = "";

        public string? SymlinkProblem { get; }

        public Step OutsideSrc { get; }

        public Step UpdateWithoutIgnore { get; }

        public void Dispose()
        {
            try
            {
                // git writes its objects read-only, and Windows will not delete a read-only file.
                foreach (string file in Directory.EnumerateFiles(_scratch, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                Directory.Delete(_scratch, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        /// <summary>Two commits, tagged v1 and v2, cloned bare; returns its url as the script forms a sibling's.</summary>
        private string Upstream()
        {
            string work = Path.Combine(_scratch, "upstream-work");
            Directory.CreateDirectory(work);
            Git(work, "init", "-q");
            foreach ((string text, string tag) in new[] { ("one\n", "v1"), ("two\n", "v2") })
            {
                File.WriteAllText(Path.Combine(work, "README.txt"), text);
                Git(work, "add", "-A");
                Git(work, "-c", Identity, "-c", Email, "-c", "commit.gpgsign=false", "commit", "-q", "-m", tag);
                Git(work, "-c", "tag.gpgSign=false", "tag", tag);
            }

            string bare = Path.Combine(_scratch, "upstream.git");
            Git(_scratch, "clone", "-q", "--bare", work, bare);
            return "file://" + (bare.StartsWith('/') ? "" : "/") + bare.Replace('\\', '/');
        }

        private void Manifest(string reference, string dir = "reference/src/" + Name)
        {
            JsonObject manifest = new()
            {
                ["sources"] = new JsonArray(new JsonObject
                {
                    ["name"] = Name,
                    ["dir"] = dir,
                    ["url"] = _url,
                    ["ref"] = reference,
                }),
            };
            File.WriteAllText(Path.Combine(_repository, "reference", "sources.json"), manifest.ToJsonString());
        }

        // `--file`, as a person runs it, from the repository root.
        private Step Fetch(params string[] arguments)
        {
            List<string> command = ["run", "--file", Path.Combine("reference", "fetch-reference.cs")];
            if (arguments.Length > 0)
            {
                command.Add("--");
                command.AddRange(arguments);
            }
            (int exit, string output) = ConventionsScript.Execute("dotnet", _repository, [.. command]);
            string file = Path.Combine(_repository, "reference", "src", Name, "README.txt");
            return new Step(exit, output, File.Exists(file) ? File.ReadAllText(file) : null);
        }

        private static string Git(string directory, params string[] arguments)
        {
            (int exit, string output) = ConventionsScript.Execute("git", directory, arguments);
            Assert.True(exit == 0, $"git {string.Join(' ', arguments)} failed in {directory}:\n{output}");
            return output;
        }
    }
}
