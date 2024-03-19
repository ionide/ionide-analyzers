---
title: Contributing
categoryindex: 1
index: 1
category: docs
---

# Contributing

Hi there! Thank you for considering to contribute to this community project!  
We hope this project can serve as a vessel to proto-type your ideas and bring them easily into your workflow. 

The main goal of this project is to have a reference implementation for analyzers built with the [Ionide SDK](https://ionide.io/FSharp.Analyzers.SDK/).

## What kind of contributions do we accept?

We see this project as a collection of **general purpose analyzers for every kind of F# codebase**. The analyzers are ideally based on generally accepted guidance among the community.

If an analyzer is more tailored towards your own use-cases, or has a distinct (controversial) opinion, we might decline your pull request. This is to benefit the out-of-the-box experience.
We wish to avoid that people turns of certain analyzers from this `NuGet` package, because they are not universally accepted as best practise rules.

The best thing you can do is **pitch your idea before** you start any **implementation**. This is to avoid a mismatch of expectations. 

## How do I contribute a new analyzer?

When creating a new analyzer, the typical experience is that you will provide a one-time contribution. We review your PR, we go back and forward over some details, we merge it, and ship a new NuGet package with your work!
We truly hope to provide you with a great experience while contribution, and also keep a good balance on the maintenance of your change afterwards.

### Technical setup

This is a very typical `dotnet` repository. Run commands like `dotnet tool restore`, `dotnet restore` and `dotnet build` to get going.

Our build script can be invoke with `dotnet fsi build.fsx`.  
Or `dotnet fsi build.fsx -- --help` to view non-default pipelines.

### Your analyzer

Scaffold your analyzer by running:

```shell
dotnet fsi .\build.fsx -- -p NewAnalyzer
```

This will ask prompt your to enter a name and a category and create will all the necessary files.

We try to split the analyzers up into several categories:

- `hints`
- `style`
- `performance`
- `quality`

Add your analyzer the directory that makes the most sense. Ask us if you are unsure.  
Next start writing your [first analyzer](https://ionide.io/FSharp.Analyzers.SDK/content/Getting%20Started%20Writing.html#First-analyzer).

Please use the *next available code* for your messages, we currently do not have any elaborate system in place for the message codes.

### Your regression tests

Because we want to ensure you analyzer keeps working with every new release, we would like to ask you to provide a series of unit tests. These should cover the most critical use-case of your analyzer.
Try and create unit tests in a fashion where the tests themselves are stable. If the SDK API changes, we only want to update your analyzer code, and your tests should run fine. 

### Your documentation

Each analyzer should have a matching documentation page.
This is the url we will use to link in the `AnalyzerAttribute` meta data.  
Use the existing pages as a reference.

Run `dotnet fsi build.fsx -p Docs` to run the `fsdocs` tool locally.

### Your changelog entry

We use [KeepAChangelog](https://github.com/ionide/keepachangelog) to determine our next NuGet version.  
Please add [a new entry](https://keepachangelog.com/en/1.1.0/) for your changes.

## When will the next version ship?

Unless, there are technical reason blocking us, we will try and ship your contribution as soon as possible.  
Our CI process should pick up new version from the changelog and push new packages to NuGet.org once the code is on the `main` branch.

## Using a local analyzers SDK

You might run into a situation when the SDK packages don't provide the features you need for your analyzers development.  
In such a case, you can edit the `Directory.Build.props` file and set `UseLocalAnalyzersSDK` to `true` and let `LocalAnalyzersSDKRepo` point to your local SDK repository.
