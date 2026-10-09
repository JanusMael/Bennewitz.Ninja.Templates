# Releasing CliStem

A release is one self-contained, trimmed, single-file executable per platform, attached to a GitHub
Release. Nothing goes to nuget.org, and no credential needs setting up: the workflow's own token
creates the release.

## The version

The version is the tag: `vYYYY.Q.MMDD`, for example `v2026.3.920` for a release on 20 September
2026, the third quarter. The binaries carry the version the tag gives them, and `CliStem --version`
prints it.

## Releasing

1. `main` is green, including CI's `publish` job, which is the trimmed binary run as a release
   would ship it.
2. `PROGRESS.md` lists what the release carries.
3. Tag and push:

   ```bash
   git tag v2026.3.920
   git push origin v2026.3.920
   ```

   Or run **Release** → *Run workflow* with the version; the workflow creates the tag on the release.

The workflow runs, in order: `Refuse to release a repository the conventions reject`, `Build` and
`Test`; then, for each of the six platforms, on Linux, `Publish` and `Archive`; then
`Create GitHub Release`. A failure at any step stops it before the release exists.

## Checking what shipped

On the release page, download one archive, unpack it, and run `CliStem --version`: the version just
released. Then record the release in `PROGRESS.md`.

A release can be deleted and re-cut, unlike a package version. The tag is what users see, so
delete a bad release and its tag together, and release under a new version rather than reusing one.
