# Releasing AppStem

A release is one self-contained, trimmed, single-file binary per platform, attached to a GitHub
Release. Nothing goes to nuget.org, and no credential needs setting up: the workflow's own token
creates the release.

## The version

The version is the tag: `vYYYY.Q.MMDD`, for example `v2026.3.920` for a release on 20 September
2026, the third quarter. The binaries carry the version the tag gives them.

## Releasing

1. `main` is green, including CI's `trim` job.
2. `PROGRESS.md` lists what the release carries.
3. Tag and push:

   ```bash
   git tag v2026.3.920
   git push origin v2026.3.920
   ```

   Or run **Release** → *Run workflow* with the version; the workflow creates the tag on the release.

The workflow runs, in order: `Refuse to release a repository the conventions reject`, `Build` and
`Test`; then, for each of the six platforms, `Publish`, `Compare warnings with the baseline` and
`Archive`; then `Create GitHub Release`. A failure at any step stops it before the release exists.

## When a publish reports a new trim warning

`Compare warnings with the baseline` fails on any change to the set in
`src/AppStem/trim-warnings.txt`, in either direction.

- **A new warning** is a trim hazard: code that the trimmed build may break. Fix the cause when it
  is this app's; when it is a dependency's, decide whether to ship it, and if so add the line to the
  baseline with the reason, in a commit of its own.
- **A missing warning** means the baseline is stale or ILLink stopped analysing something. Find out
  which before removing the line.

## Checking what shipped

On the release page, download one binary, run it with `--smoke`, and check it exits 0. That proves
the trimmed build starts, loads its theme and binds. Then record the release in `PROGRESS.md`.

A release can be deleted and re-cut, unlike a package version. The tag is what users see, so
delete a bad release and its tag together, and release under a new version rather than reusing one.
