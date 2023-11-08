---
title: Release Process
categoryindex: 1
index: 2
category: docs
---
# Release process

A new release happens automatically when the version in the [Changelog](https://github.com/ionide/ionide-analyzers/blob/main/CHANGELOG.md) was increased.  
We verify the next version doesn't exist yet on NuGet, and if that is the case, we publish the `*.nupkg` package and create a new GitHub release.

## Dry run

We use a custom pipeline in our `build.fsx` script to generate the release.  
You can safely dry-run this locally using:

> dotnet fsi build.fsx -- -p Release --dry-run
