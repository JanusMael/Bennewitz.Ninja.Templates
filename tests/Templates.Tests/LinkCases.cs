namespace Templates.Tests;

/// <summary>
/// The link cases both scripts' link readers are held to (plans/00012 decision 3): repo-conventions
/// reads them in a family's README, assert-packages in a packed one.
/// </summary>
internal static class LinkCases
{
    /// <summary>Every way of linking, line by line.</summary>
    public const string Markdown =
        """
        # Alpha

        [Absolute](https://example.com/alpha), [an anchor](#install), <https://example.com/autolink> and [mail](mailto:someone@example.com).
        [Inline](../README.md) and ![an image](images/logo.png).

        [defined]: docs/other.md
        [absolute definition]: https://example.com/defined
        <a href="CONTRIBUTING.md">html</a> <img src='img/badge.png'> <a href="https://example.com/html">absolute html</a>

        Inline code is text: `[shown](relative.md)`, and so is ``[this](`odd`.md)``.

        ```markdown
        [in a fence](relative.md)
        <img src="fenced.png">
        ```

        ~~~
        [in a tilde fence](relative.md)
        ~~~

        ## Install

        """;

    /// <summary>
    /// The relative links in <see cref="Markdown"/>, in order: inline, an image, a reference
    /// definition and an HTML attribute each count; an absolute link, an anchor, and a link shown
    /// inside code do not.
    /// </summary>
    public static readonly (int Line, string Target)[] Relative =
    [
        (4, "../README.md"),
        (4, "images/logo.png"),
        (6, "docs/other.md"),
        (8, "CONTRIBUTING.md"),
        (8, "img/badge.png"),
    ];

    /// <summary>A README whose only relative links are shown inside code.</summary>
    public const string OnlyInCode =
        """
        # Shown, not linked

        Write `[the guide](docs/guide.md)` to link it.

        ```markdown
        [the guide](docs/guide.md)
        ```

        """;
}
