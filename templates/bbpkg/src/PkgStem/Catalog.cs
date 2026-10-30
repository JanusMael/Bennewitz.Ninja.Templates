namespace Bennewitz.Ninja.PkgStem;

/// <summary>
/// What every rule in this package shares: its ID's prefix, and where it says it comes from.
/// </summary>
/// <remarks>
/// ⭐ <b>An ID is four letters and a number, and the rest is metadata.</b> Every analyzer a project
/// loads shares one flat namespace of IDs, in <c>#pragma</c>, <c>NoWarn</c> and
/// <c>.editorconfig</c>, so the ID stays short and unique; where a rule comes from is shown by its
/// help link, its category and the package id, not by a longer ID. The prefix is <c>BN</c> plus the
/// product's initials, the scheme across the Bennewitz.Ninja quality packages, and once one ID has
/// shipped it never changes, because every rename breaks a consumer's suppressions.
/// </remarks>
internal static class Catalog
{
    /// <summary>The prefix of every rule's ID.</summary>
    public const string Prefix = "BNXX";

    /// <summary>The category every rule reports under: the package, so a reader can find it.</summary>
    public const string Category = "PKG_ID";

    /// <summary>
    /// The rule's section of the README, which is also the package's nuget.org page.
    /// </summary>
    public static string HelpLink(string id) =>
        "https://github.com/REPO_OWNER/PKG_ID/blob/main/README.md#" + id.ToLowerInvariant();
}
