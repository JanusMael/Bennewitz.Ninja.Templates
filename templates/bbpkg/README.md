# PKG_ID

<!-- bbpkg: one paragraph saying what this package does, in the words someone searching nuget.org would use. This file is packed into the package, so it is also the description shown on nuget.org: write it for someone installing the package, and link absolutely, since a relative link goes nowhere there -->
## Install

```bash
dotnet add package PKG_ID
```
<!--#if (Analyzer) -->

The package is a development dependency: its analyzers run inside the compiler and add nothing to
your build's output, and it does not flow to whoever references your project. Any rule's severity
can be set in `.editorconfig`, as `dotnet_diagnostic.<id>.severity`.

## Rules

<!-- BEGIN GENERATED RULES -->
| Id | Default | Reports |
|---|---|---|
| [`BNXX1001`](#bnxx1001) | on | A library's public type is named for being a helper |
<!-- END GENERATED RULES -->

### BNXX1001

A public type of a library whose name ends in `Helper`. The name says what is left over, not what
the type does, and in a library's public surface every consumer comes to depend on it. Name the type
for what it does. The rule is silent in an application, whose public types are nobody's surface.
<!--#endif -->

## Licence

MIT. See [LICENSE](https://github.com/REPO_OWNER/PKG_ID/blob/main/LICENSE).
