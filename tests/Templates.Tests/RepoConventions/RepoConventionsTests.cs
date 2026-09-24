using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Templates.Tests.RepoConventions;

/// <summary>
/// <c>scripts/repo-conventions.cs</c>, run exactly as CI runs it, over a repository built in a temp
/// directory and GitHub's answers read from fixture files.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>One conforming repository, broken one way per test.</b> <see cref="Conforming"/> passes at
/// admin depth; every other test changes exactly one thing and asserts the finding it causes, so a
/// check that stops firing fails its own test rather than hiding behind another's.
/// </para>
/// <para>
/// ⚠ <b>The ruleset fixtures are written as GitHub returns them</b>, with the fields a response
/// carries and a create request does not (<c>id</c>, <c>source</c>, <c>current_user_can_bypass</c>),
/// and NOT generated from the script's own baseline. Generated, they would agree with the script
/// by construction and could never catch it building the wrong ruleset.
/// </para>
/// <para>
/// ⓘ One class, so the runs are sequential: parallel <c>dotnet run --file</c> builds of the same
/// file contend for its build output.
/// </para>
/// </remarks>
public sealed class RepoConventionsTests
{
    [Fact]
    public void A_conforming_repository_passes_at_admin_depth()
    {
        Result result = Conforming().Run("check", "--admin");

        Assert.True(result.ExitCode == 0, result.Output);
        Assert.Contains("conforms (check, admin depth)", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Token_depth_notes_what_it_cannot_read_and_still_passes()
    {
        Result result = WithoutAdminFields().Run("check");

        Assert.True(result.ExitCode == 0, result.Output);
        Assert.Contains("NOTE settings:", result.Output, StringComparison.Ordinal);
        Assert.Contains("NOTE rulesets: Ruleset \"main\": who may bypass it needs admin rights", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Token_depth_checks_the_feature_toggles_a_workflow_token_can_read()
    {
        Fixture fixture = WithoutAdminFields();
        fixture.Api["repo"]!["has_wiki"] = true;

        Result result = fixture.Run("check");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL settings: \"has_wiki\" is true; the family baseline is false.", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_depth_fails_on_anything_it_could_not_read()
    {
        Result result = WithoutAdminFields().Run("check", "--admin");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("UNVERIFIED settings: \"allow_merge_commit\" was not returned", result.Output, StringComparison.Ordinal);
        Assert.Contains("UNVERIFIED security: Dependabot security updates were not returned", result.Output, StringComparison.Ordinal);
        Assert.Contains("UNVERIFIED rulesets: Ruleset \"release-tags\": who may bypass it could not be read", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void An_empty_description_fails_even_at_release_depth()
    {
        Fixture fixture = Conforming();
        fixture.Api["repo"]!["description"] = "";

        Result result = fixture.Run("check", "--release");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL description: The GitHub description is empty.", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Release_depth_does_not_stop_a_publish_for_drifted_settings_or_topics()
    {
        Fixture fixture = Conforming();
        fixture.Api["repo"]!["topics"] = new JsonArray("csharp");
        fixture.Api["repo"]!["allow_merge_commit"] = true;
        fixture.Api.Remove("rulesets");

        Result result = fixture.Run("check", "--release");

        Assert.True(result.ExitCode == 0, result.Output);
    }

    [Fact]
    public void Release_depth_still_stops_a_publish_for_a_missing_document()
    {
        Fixture fixture = Conforming();
        fixture.Files.Remove("PROGRESS.md");

        Result result = fixture.Run("check", "--release");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL docs: PROGRESS.md is missing", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_top_level_directory_needs_its_documents_including_one_added_later()
    {
        Fixture fixture = Conforming();
        fixture.Files["trimcheck/Program.cs"] = "// a directory nobody listed anywhere";

        Result result = fixture.Run("check", "--admin");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL docs: trimcheck/AGENTS.md is missing", result.Output, StringComparison.Ordinal);
        Assert.Contains("FAIL docs: trimcheck/CLAUDE.md is missing", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void An_exemption_with_a_reason_excuses_its_directory()
    {
        Fixture fixture = Conforming();
        fixture.Files["publish/app.txt"] = "build output";
        fixture.Config["undocumented"] = new JsonObject { ["publish"] = "Build output the release attaches; nothing is written by hand." };

        Result result = fixture.Run("check", "--admin");

        Assert.True(result.ExitCode == 0, result.Output);
    }

    [Fact]
    public void An_exemption_without_a_reason_fails()
    {
        Fixture fixture = Conforming();
        fixture.Files["publish/app.txt"] = "build output";
        fixture.Config["undocumented"] = new JsonObject { ["publish"] = " " };

        Result result = fixture.Run("check", "--admin");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("\"undocumented\" exempts \"publish\" without a reason", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_template_marker_fails_with_its_line_but_not_under_shipped_content()
    {
        Fixture fixture = Conforming();
        fixture.Files["src/AGENTS.md"] = "# src\n\n<!-- bbpkg: describe what src/ holds -->\n";

        Result result = fixture.Run("check", "--admin");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL docs: src/AGENTS.md:3 still carries a template marker", result.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("templates/bbpkg/AGENTS.md", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Prose_that_mentions_the_marker_syntax_is_not_a_marker()
    {
        Fixture fixture = Conforming();
        fixture.Files["src/AGENTS.md"] = "# src\n\nReplace every `<!-- bbpkg: … -->` marker before the first release.\n";

        Result result = fixture.Run("check", "--admin");

        Assert.True(result.ExitCode == 0, result.Output);
    }

    [Fact]
    public void A_pointer_file_that_says_anything_else_fails()
    {
        Fixture fixture = Conforming();
        fixture.Files["src/CLAUDE.md"] = "@AGENTS.md\n\nAlso: prefer records.\n";

        Result result = fixture.Run("check", "--admin");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL docs: src/CLAUDE.md must be exactly `@AGENTS.md`", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_required_check_that_no_job_reports_fails()
    {
        Fixture fixture = Conforming();
        fixture.Config["requiredChecks"]!.AsArray().Add("lint");

        Result result = fixture.Run("check");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL required checks: \"lint\" is required but no job reports it.", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_ruleset_loosened_on_github_fails()
    {
        Fixture fixture = Conforming();
        Rule(fixture.Api["rulesets-1"]!, "pull_request")["parameters"]!["required_review_thread_resolution"] = false;

        Result result = fixture.Run("check");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Ruleset \"main\" conversation resolution: wanted [true], GitHub has [false]", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_bypass_only_inside_pull_requests_fails_because_it_blocks_direct_pushes()
    {
        Fixture fixture = Conforming();
        fixture.Api["rulesets-1"]!["bypass_actors"]![0]!["bypass_mode"] = "pull_request";

        Result result = fixture.Run("check", "--admin");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Ruleset \"main\" bypass: wanted [RepositoryRole:5:always], GitHub has [RepositoryRole:5:pull_request]", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_missing_ruleset_fails()
    {
        Fixture fixture = Conforming();
        fixture.Api["rulesets"] = new JsonArray(new JsonObject { ["id"] = 1, ["name"] = "main" });

        Result result = fixture.Run("check");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL rulesets: Ruleset \"release-tags\" does not exist.", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_setting_off_the_family_baseline_fails_at_admin_depth()
    {
        Fixture fixture = Conforming();
        fixture.Api["repo"]!["allow_merge_commit"] = true;

        Result result = fixture.Run("check", "--admin");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL settings: \"allow_merge_commit\" is true; the family baseline is false.", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Classic_branch_protection_beside_the_ruleset_fails_at_admin_depth()
    {
        Fixture fixture = Conforming();
        fixture.Api["branches-main-protection"] = new JsonObject { ["url"] = "https://api.github.com/repos/o/r/branches/main/protection" };

        Result result = fixture.Run("check", "--admin");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("main still has classic branch protection", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Topics_missing_a_family_topic_fail_even_without_repository_json()
    {
        Fixture fixture = Conforming();
        fixture.WriteConfig = false;
        fixture.Api["repo"]!["topics"] = new JsonArray("csharp", "dotnet");

        Result result = fixture.Run("check");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL repository.json: .github/repository.json is missing.", result.Output, StringComparison.Ordinal);
        Assert.Contains("FAIL topics: GitHub's topics lack \"nuget\"", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_copy_of_the_script_that_differs_from_the_template_fails()
    {
        Fixture fixture = Conforming();
        fixture.Files["scripts/repo-conventions.cs"] = "// an older copy";

        Result result = fixture.Run("check");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("FAIL drift: scripts/repo-conventions.cs differs from the template's", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void Apply_writes_the_baseline_and_updates_an_existing_ruleset_in_place()
    {
        Result result = Conforming().Run("apply", "--dry-run");

        Assert.True(result.ExitCode == 0, result.Output);
        string[] lines = [.. Lines(result.Output)];

        string patch = Assert.Single(lines, l => l.StartsWith("PATCH repos/o/r ", StringComparison.Ordinal));
        Assert.Contains("\"description\":\"Fixture package.\"", patch, StringComparison.Ordinal);
        Assert.Contains("\"allow_merge_commit\":false", patch, StringComparison.Ordinal);
        Assert.Contains("\"has_wiki\":false", patch, StringComparison.Ordinal);

        Assert.Contains(lines, l => l.StartsWith("PUT repos/o/r/topics ", StringComparison.Ordinal) && l.Contains("\"fixture\"", StringComparison.Ordinal));
        Assert.Contains("PUT repos/o/r/vulnerability-alerts", lines);
        Assert.Contains("PUT repos/o/r/automated-security-fixes", lines);

        string main = Assert.Single(lines, l => l.StartsWith("PUT repos/o/r/rulesets/1 ", StringComparison.Ordinal));
        Assert.Contains("\"bypass_mode\":\"always\"", main, StringComparison.Ordinal);
        Assert.Contains("{\"context\":\"Build (ubuntu-latest)\"}", main, StringComparison.Ordinal);
        Assert.Contains("\"required_approving_review_count\":0", main, StringComparison.Ordinal);
        Assert.Single(lines, l => l.StartsWith("PUT repos/o/r/rulesets/2 ", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, l => l.StartsWith("POST ", StringComparison.Ordinal));
    }

    [Fact]
    public void Apply_creates_a_ruleset_that_does_not_exist_yet()
    {
        Fixture fixture = Conforming();
        fixture.Api["rulesets"] = new JsonArray();

        Result result = fixture.Run("apply", "--dry-run");

        Assert.True(result.ExitCode == 0, result.Output);
        Assert.Equal(2, Lines(result.Output).Count(l => l.StartsWith("POST repos/o/r/rulesets ", StringComparison.Ordinal)));
    }

    [Fact]
    public void Apply_refuses_to_require_a_check_that_no_job_reports()
    {
        Fixture fixture = Conforming();
        fixture.Config["requiredChecks"]!.AsArray().Add("lint");

        Result result = fixture.Run("apply", "--dry-run");

        Assert.Equal(1, result.ExitCode);
        Assert.DoesNotContain(Lines(result.Output), l => l.StartsWith("PATCH ", StringComparison.Ordinal));
    }

    private static IEnumerable<string> Lines(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim());

    private static JsonNode Rule(JsonNode ruleset, string type) =>
        ruleset["rules"]!.AsArray().Single(r => r!["type"]!.GetValue<string>() == type)!;

    /// <summary>
    /// The conforming repository as a workflow's read-only GITHUB_TOKEN sees it: exactly the fields
    /// a probe run found absent (plans/00003 step 2) are taken away, and the feature toggles stay.
    /// </summary>
    private static Fixture WithoutAdminFields()
    {
        Fixture fixture = Conforming();
        JsonObject repo = fixture.Api["repo"]!.AsObject();
        foreach (string field in (string[])["allow_merge_commit", "allow_squash_merge", "allow_rebase_merge", "allow_auto_merge",
                     "delete_branch_on_merge", "allow_update_branch", "security_and_analysis"])
        {
            repo.Remove(field);
        }
        fixture.Api["rulesets-1"]!.AsObject().Remove("bypass_actors");
        fixture.Api["rulesets-2"]!.AsObject().Remove("bypass_actors");
        return fixture;
    }

    private static Fixture Conforming()
    {
        string script = File.ReadAllText(Path.Combine(RepoRoot(), "scripts", "repo-conventions.cs"));

        Dictionary<string, string> files = new(StringComparer.Ordinal)
        {
            ["README.md"] = "# Fixture\n",
            ["PROGRESS.md"] = "# Progress\n",
            ["AGENTS.md"] = "# Fixture\n\nThe conventions are in docs/repository-conventions.md.\n",
            ["CLAUDE.md"] = "@AGENTS.md\n",
            [".github/AGENTS.md"] = "# .github\n",
            [".github/CLAUDE.md"] = "@AGENTS.md\n",
            [".github/copilot-instructions.md"] = "Read AGENTS.md at the repository root.\n",
            [".github/workflows/ci.yml"] =
                """
                name: CI
                on: [push, pull_request]
                jobs:
                  build:
                    name: Build (${{ matrix.os }})
                    runs-on: ${{ matrix.os }}
                    strategy:
                      matrix:
                        os: [ubuntu-latest, windows-latest]
                    steps:
                      - run: dotnet build
                  conventions:
                    runs-on: ubuntu-latest
                    steps:
                      - run: dotnet run --file scripts/repo-conventions.cs -- check
                """,
            ["src/AGENTS.md"] = "# src\n",
            ["src/CLAUDE.md"] = "@AGENTS.md\n",
            ["src/Widget.cs"] = "namespace Fixture;\n",
            ["scripts/AGENTS.md"] = "# scripts\n",
            ["scripts/CLAUDE.md"] = "@AGENTS.md\n",
            ["scripts/repo-conventions.cs"] = script,
            ["templates/AGENTS.md"] = "# templates\n\nEverything beneath is shipped content.\n",
            ["templates/CLAUDE.md"] = "@AGENTS.md\n",
            ["templates/bbpkg/AGENTS.md"] = "<!-- bbpkg: describe the generated repository -->\n",
            ["templates/bbpkg/scripts/repo-conventions.cs"] = script,
        };

        JsonObject config = new()
        {
            ["description"] = "Fixture package.",
            ["homepage"] = "",
            ["topics"] = new JsonArray("csharp", "dotnet", "nuget", "fixture"),
            ["requiredChecks"] = new JsonArray("Build (ubuntu-latest)", "conventions"),
            ["content"] = new JsonArray("templates/bbpkg"),
        };

        Dictionary<string, JsonNode?> api = new(StringComparer.Ordinal)
        {
            ["repo"] = JsonNode.Parse(
                """
                {
                  "id": 1, "name": "r", "full_name": "o/r", "default_branch": "main",
                  "description": "Fixture package.", "homepage": "",
                  "topics": ["csharp", "dotnet", "fixture", "nuget"],
                  "allow_merge_commit": false, "allow_squash_merge": true, "allow_rebase_merge": true,
                  "allow_auto_merge": false, "delete_branch_on_merge": true, "allow_update_branch": true,
                  "has_issues": true, "has_wiki": false, "has_projects": false, "has_discussions": false,
                  "security_and_analysis": {
                    "secret_scanning": { "status": "enabled" },
                    "dependabot_security_updates": { "status": "enabled" }
                  }
                }
                """),
            ["rulesets"] = JsonNode.Parse(
                """
                [
                  { "id": 1, "name": "main", "target": "branch", "source_type": "Repository", "source": "o/r", "enforcement": "active" },
                  { "id": 2, "name": "release-tags", "target": "tag", "source_type": "Repository", "source": "o/r", "enforcement": "active" }
                ]
                """),
            ["rulesets-1"] = JsonNode.Parse(
                """
                {
                  "id": 1, "name": "main", "target": "branch", "source_type": "Repository", "source": "o/r",
                  "enforcement": "active", "current_user_can_bypass": "always",
                  "bypass_actors": [ { "actor_id": 5, "actor_type": "RepositoryRole", "bypass_mode": "always" } ],
                  "conditions": { "ref_name": { "exclude": [], "include": ["~DEFAULT_BRANCH"] } },
                  "rules": [
                    { "type": "non_fast_forward" },
                    { "type": "deletion" },
                    { "type": "required_status_checks", "parameters": {
                        "strict_required_status_checks_policy": true, "do_not_enforce_on_create": false,
                        "required_status_checks": [ { "context": "conventions" }, { "context": "Build (ubuntu-latest)" } ] } },
                    { "type": "pull_request", "parameters": {
                        "required_approving_review_count": 0, "dismiss_stale_reviews_on_push": false,
                        "require_code_owner_review": false, "require_last_push_approval": false,
                        "required_review_thread_resolution": true, "automatic_copilot_code_review_enabled": false,
                        "allowed_merge_methods": ["squash", "rebase"] } }
                  ]
                }
                """),
            ["rulesets-2"] = JsonNode.Parse(
                """
                {
                  "id": 2, "name": "release-tags", "target": "tag", "source_type": "Repository", "source": "o/r",
                  "enforcement": "active", "current_user_can_bypass": "always",
                  "bypass_actors": [ { "actor_id": 5, "actor_type": "RepositoryRole", "bypass_mode": "always" } ],
                  "conditions": { "ref_name": { "exclude": [], "include": ["refs/tags/v*"] } },
                  "rules": [ { "type": "deletion" }, { "type": "creation" }, { "type": "update" } ]
                }
                """),
            // GitHub answers 204 with no body when vulnerability alerts are on.
            ["vulnerability-alerts"] = null,
        };

        return new Fixture(files, config, api);
    }

    private static string RepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Bennewitz.Ninja.Templates.csproj")))
        {
            directory = directory.Parent;
        }
        Assert.NotNull(directory);
        return directory.FullName;
    }

    private sealed record Result(int ExitCode, string Output);

    /// <summary>A repository's files, its repository.json, and GitHub's answers, written out per run.</summary>
    private sealed class Fixture(Dictionary<string, string> files, JsonObject config, Dictionary<string, JsonNode?> api)
    {
        public Dictionary<string, string> Files { get; } = files;

        public JsonObject Config { get; } = config;

        /// <summary>Keyed by fixture name: "repo", "rulesets", "rulesets-1", …; a null value answers 204.</summary>
        public Dictionary<string, JsonNode?> Api { get; } = api;

        /// <summary>False leaves .github/repository.json out of the tree.</summary>
        public bool WriteConfig { get; set; } = true;

        public Result Run(params string[] arguments)
        {
            string directory = Path.Combine(Path.GetTempPath(), "repo-conventions-" + Guid.NewGuid().ToString("N"));
            string tree = Path.Combine(directory, "tree");
            string answers = Path.Combine(directory, "api");
            Directory.CreateDirectory(tree);
            Directory.CreateDirectory(answers);

            try
            {
                Dictionary<string, string> written = new(Files, StringComparer.Ordinal);
                if (WriteConfig)
                {
                    written[".github/repository.json"] = Config.ToJsonString();
                }
                foreach ((string path, string text) in written)
                {
                    string target = Path.Combine(tree, path);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.WriteAllText(target, text);
                }
                foreach ((string name, JsonNode? answer) in Api)
                {
                    File.WriteAllText(Path.Combine(answers, name + ".json"), answer?.ToJsonString() ?? "");
                }

                (int initExit, string initOutput) = Execute("git", tree, "init", "-q");
                Assert.True(initExit == 0, initOutput);

                (int exit, string output) = Execute("dotnet", RepoRoot(),
                    ["run", "--file", Path.Combine("scripts", "repo-conventions.cs"), "--",
                     .. arguments, "--root", tree, "--repo", "o/r", "--api-fixtures", answers]);
                return new Result(exit, output);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

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
