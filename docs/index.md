# Ionide.Analyzers

Welcome to the Ionide Analyzers project.

## Quick start

In its simplest form, running analyzers requires running the [fsharp-analyzers tool](https://www.nuget.org/packages/fsharp-analyzers) and pass your `*.fsproj` and a folder that contains analyzers binaries.

```shell
# Create a new manifest if you don't have one
dotnet new tool-manifest

# Install the local tool
dotnet tool install fsharp-analyzers

# Execute the tool, run dotnet fsharp-analyzers --help for more options. 
dotnet fsharp-analyzers --project ./src/MyProject.fsproj --analyzers-path /var/some-folder
```

Of course, the `--analyzers-path` is a bit tricky. We need to download the binary and find them somehow.

You can add a NuGet reference to your project:

```xml
<PackageReference Include="Ionide.Analyzers" Version="0.1.1">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
```

This will download the package to our local NuGet cache.  
*But how do we find that local path?*

Starting `dotnet 8 RC 2`, we can [evaluate an MSBuild property](https://devblogs.microsoft.com/dotnet/announcing-dotnet-8-rc2/#msbuild-simple-cli-based-project-evaluation) which NuGet restore creates for us.

> dotnet build ./src/MyProject.fsproj --getProperty:PkgIonide_Analyzers

`PkgIonide_Analyzers` comes from the [{projectName}.projectFileExtension.nuget.g.props](https://learn.microsoft.com/en-us/nuget/reference/msbuild-targets#restore-outputs) file.

This can yield

> C:\Users\username\\.nuget\packages\ionide.analyzers\0.1.1

And we can use that path to pass to the tool:

> dotnet fsharp-analyzers --project ./src/MyProject.fsproj --analyzers-path C:\Users\username\\.nuget\packages\ionide.analyzers\0.1.1

## Contribute!

Learn how to [get started contributing]({{fsdocs-next-page-link}})!
