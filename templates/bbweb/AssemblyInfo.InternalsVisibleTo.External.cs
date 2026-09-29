// AssemblyInfo.InternalsVisibleTo.External.cs: grants to assemblies OUTSIDE this repository.
//
// Written by hand. `repo-conventions grants` writes this file only when it is missing, and never
// touches it after. Directory.Build.targets links it into every project that links
// AssemblyInfo.InternalsVisibleTo.cs, so a name here sees the internals of every such project.
//
// A name is an ASSEMBLY name, unprefixed ("AppServices"), never a root namespace
// ("Bennewitz.Ninja.AppServices"): that form compiles, ships and grants nothing, and
// `repo-conventions check` fails on it. An internal another repository uses is a promise: change
// it only together with a release of that repository. See docs/repository-conventions.md in
// Bennewitz.Ninja.Templates.
//
// [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("AppServices")]
