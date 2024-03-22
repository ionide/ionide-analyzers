#r "nuget: Fun.Build, 1.0.3"
#r "nuget: Fake.IO.FileSystem, 6.0.0"
#r "nuget: NuGet.Protocol, 6.7.0"
#r "nuget: Ionide.KeepAChangelog, 0.1.8"
#r "nuget: Humanizer.Core, 2.14.1"

open System
open System.IO
open System.Xml.Linq
open System.Threading
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fun.Build
open NuGet.Common
open NuGet.Protocol
open NuGet.Protocol.Core.Types
open Ionide.KeepAChangelog
open Ionide.KeepAChangelog.Domain
open SemVersion
open Humanizer

let cleanDirs globExpr = (!!globExpr) |> Shell.cleanDirs

let packStage =
    stage "pack" { run "dotnet pack ./src/Ionide.Analyzers/Ionide.Analyzers.fsproj -c Release -o bin" }

pipeline "Build" {
    workingDir __SOURCE_DIRECTORY__
    stage "clean" {
        run (fun _ ->
            async {
                cleanDirs "src/**/obj"
                cleanDirs "src/**/bin"
                cleanDirs "tests/**/obj"
                cleanDirs "tests/**/bin"
                Shell.cleanDir "bin"
                return 0
            }
        )
    }
    stage "lint" {
        run "dotnet tool restore"
        run "dotnet fantomas . --check"
    }
    stage "restore" { run "dotnet restore" }
    stage "build" {
        run "dotnet restore ionide-analyzers.sln"
        run "dotnet build --no-restore -c Release ionide-analyzers.sln"
    }
    stage "test" { run "dotnet test --no-restore --no-build -c Release" }
    packStage
    stage "docs" {
        envVars
            [|
                "DOTNET_ROLL_FORWARD_TO_PRERELEASE", "1"
                "DOTNET_ROLL_FORWARD", "LatestMajor"
            |]
        run "dotnet fsdocs build --properties Configuration=Release"
    }
    runIfOnlySpecified false
}

pipeline "Docs" {
    workingDir __SOURCE_DIRECTORY__
    stage "main" {
        envVars
            [|
                "DOTNET_ROLL_FORWARD_TO_PRERELEASE", "1"
                "DOTNET_ROLL_FORWARD", "LatestMajor"
            |]
        run "dotnet tool restore"
        run "dotnet fsdocs watch --port 7890"
    }
    runIfOnlySpecified true
}

let getLatestPublishedNugetVersion packageName =
    task {
        let logger = NullLogger.Instance
        let cancellationToken = CancellationToken.None

        let cache = new SourceCacheContext()
        let repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json")
        let! resource = repository.GetResourceAsync<FindPackageByIdResource>()
        let! versions = resource.GetAllVersionsAsync(packageName, cache, logger, cancellationToken)
        if Seq.isEmpty versions then
            return None
        else
            return versions |> Seq.max |> Some
    }

let getLatestChangeLogVersion () : SemanticVersion * DateTime * ChangelogData option =
    let changelog = FileInfo(__SOURCE_DIRECTORY__ </> "CHANGELOG.md")
    let changeLogResult =
        match Parser.parseChangeLog changelog with
        | Error error -> failwithf "%A" error
        | Ok result -> result

    changeLogResult.Releases
    |> List.sortByDescending (fun (_, d, _) -> d)
    |> List.head

type CommandRunner =
    abstract member LogWhenDryRun: string -> unit
    abstract member RunCommand: string -> Async<Result<unit, string>>
    abstract member RunCommandCaptureOutput: string -> Async<Result<string, string>>

/// Push *.nupkg
let releaseNuGetPackage (ctx: CommandRunner) (version: SemanticVersion, _, _) =
    async {
        let key = Environment.GetEnvironmentVariable "NUGET_KEY"

        let! result =
            ctx.RunCommand
                $"dotnet nuget push bin/Ionide.Analyzers.%s{string version}.nupkg --api-key {key} --source \"https://api.nuget.org/v3/index.json\""

        match result with
        | Error _ -> return 1
        | Ok _ -> return 0
    }

type GithubRelease =
    {
        /// Is not suffixed with `v`
        Version: string
        Title: string
        Date: DateTime
        Draft: string
    }

let mapToGithubRelease (v: SemanticVersion, d: DateTime, cd: ChangelogData option) =
    match cd with
    | None -> failwith "Each Ionide.Analyzers release is expected to have at least one section."
    | Some cd ->

    let version = $"{v.Major}.{v.Minor}.{v.Patch}"
    let title =
        let month = d.ToString("MMMM")
        let day = d.Day.Ordinalize()
        $"{month} {day} Release"

    let sections =
        [
            "Added", cd.Added
            "Changed", cd.Changed
            "Fixed", cd.Fixed
            "Deprecated", cd.Deprecated
            "Removed", cd.Removed
            "Security", cd.Security
            yield! (Map.toList cd.Custom)
        ]
        |> List.choose (fun (header, lines) ->
            if lines.IsEmpty then
                None
            else
                lines
                |> List.map (fun line -> line.TrimStart())
                |> String.concat "\n"
                |> sprintf "### %s\n%s" header
                |> Some
        )
        |> String.concat "\n\n"

    let draft =
        $"""# {version}

{sections}"""

    {
        Version = version
        Title = title
        Date = d
        Draft = draft
    }

let getReleaseNotes (ctx: CommandRunner) (currentRelease: GithubRelease) (previousReleaseDate: string option) =
    async {
        let closedFilter =
            match previousReleaseDate with
            | None -> ""
            | Some date -> $"closed:>%s{date}"

        let! authorsStdOut =
            ctx.RunCommandCaptureOutput
                $"gh pr list -S \"state:closed base:main %s{closedFilter} -author:app/robot -author:app/dependabot\" --json author --jq \".[].author.login\""

        let authorMsg =
            match authorsStdOut with
            | Error e -> failwithf $"Could not get authors: %s{e}"
            | Ok stdOut ->

            let authors =
                stdOut.Split([| '\n' |], StringSplitOptions.RemoveEmptyEntries)
                |> Array.distinct
                |> Array.sort

            if authors.Length = 1 then
                $"Special thanks to @%s{authors.[0]}!"
            else
                let lastAuthor = Array.last authors
                let otherAuthors =
                    if authors.Length = 2 then
                        $"@{authors.[0]}"
                    else
                        authors
                        |> Array.take (authors.Length - 1)
                        |> Array.map (sprintf "@%s")
                        |> String.concat ", "
                $"Special thanks to %s{otherAuthors} and @%s{lastAuthor}!"

        return
            $"""{currentRelease.Draft}

{authorMsg}

[https://www.nuget.org/packages/Ionide.Analyzers/{currentRelease.Version}](https://www.nuget.org/packages/Ionide.Analyzers/{currentRelease.Version})
    """
    }

/// <summary>
/// Create a GitHub release via the CLI.
/// </summary>
/// <param name="ctx"></param>
/// <param name="currentVersion">From the ChangeLog file.</param>
/// <param name="previousReleaseDate">Filter used to find the users involved in the release. This will be passed a parameter to the GitHub CLI.</param>
let mkGitHubRelease
    (ctx: CommandRunner)
    (currentVersion: SemanticVersion * DateTime * ChangelogData option)
    (previousReleaseDate: string option)
    =
    async {
        let ghReleaseInfo = mapToGithubRelease currentVersion
        let! notes = getReleaseNotes ctx ghReleaseInfo previousReleaseDate
        ctx.LogWhenDryRun $"NOTES:\n%s{notes}"
        let noteFile = Path.GetTempFileName()
        File.WriteAllText(noteFile, notes)
        let file = $"./bin/Ionide.Analyzers.%s{ghReleaseInfo.Version}.nupkg"

        let! releaseResult =
            ctx.RunCommand
                $"gh release create v%s{ghReleaseInfo.Version} {file} --title \"{ghReleaseInfo.Title}\" --notes-file \"{noteFile}\""

        if File.Exists noteFile then
            File.Delete(noteFile)

        match releaseResult with
        | Error _ -> return 1
        | Ok _ -> return 0
    }

pipeline "Release" {
    workingDir __SOURCE_DIRECTORY__
    stage "Release " {
        packStage
        run (fun ctx ->
            async {
                let commandRunner =
                    match ctx.TryGetCmdArg "--dry-run" with
                    | ValueNone ->
                        { new CommandRunner with
                            member x.LogWhenDryRun _ = ()
                            member x.RunCommand command = ctx.RunCommand command
                            member x.RunCommandCaptureOutput command = ctx.RunCommandCaptureOutput command
                        }
                    | ValueSome _ ->
                        { new CommandRunner with
                            member x.LogWhenDryRun msg = printfn "%s" msg
                            member x.RunCommand command =
                                async {
                                    printfn $"[dry-run]:{command}"
                                    return Ok()
                                }
                            member x.RunCommandCaptureOutput command =
                                async {
                                    printfn $"[dry-run]:{command}"
                                    return Ok "nojaf\ndawedawe\nbaronfel"
                                }
                        }

                let currentVersion = getLatestChangeLogVersion ()
                let currentVersionText, _, _ = currentVersion
                let! latestNugetVersion = getLatestPublishedNugetVersion "Ionide.Analyzers" |> Async.AwaitTask
                match latestNugetVersion with
                | None ->
                    let! nugetResult = releaseNuGetPackage commandRunner currentVersion
                    let! githubResult = mkGitHubRelease commandRunner currentVersion None
                    return nugetResult + githubResult

                | Some nugetVersion when (nugetVersion.OriginalVersion <> string currentVersionText) ->
                    let! nugetResult = releaseNuGetPackage commandRunner currentVersion
                    let! previousReleaseDate =
                        ctx.RunCommandCaptureOutput
                            $"gh release view v%s{nugetVersion.OriginalVersion} --json createdAt -t \"{{{{.createdAt}}}}\""

                    let previousReleaseDate =
                        match previousReleaseDate with
                        | Error e ->
                            printfn "Unable to format previous release data, %s" e
                            None
                        | Ok d ->
                            let output = d.Trim()
                            let lastIdx = output.LastIndexOf("Z", StringComparison.Ordinal)
                            Some(output.Substring(0, lastIdx))

                    let! githubResult = mkGitHubRelease commandRunner currentVersion previousReleaseDate
                    return nugetResult + githubResult

                | Some nugetVersion ->
                    printfn "%s is already published" nugetVersion.OriginalVersion
                    return 0
            }
        )
    }
    runIfOnlySpecified true
}

let getLastCompileItem (fsproj: string) =
    let xml = File.ReadAllText fsproj
    let doc = XDocument.Parse xml
    doc.Descendants(XName.Get "Compile")
    |> Seq.filter (fun xe -> xe.Attribute(XName.Get "Include").Value <> "Program.fs")
    |> Seq.last

pipeline "NewAnalyzer" {
    stage "Scaffold" {
        run (fun _ctx ->
            Console.Write "Enter analyzer name: "
            let analyzerName = Console.ReadLine().Trim()

            let analyzerName =
                if analyzerName.EndsWith("Analyzer", StringComparison.Ordinal) then
                    analyzerName
                else
                    $"%s{analyzerName}Analyzer"

            let name = analyzerName.Replace("Analyzer", "").Camelize()

            Console.Write("Enter the analyzer Category (existing are \"Suggestion\" or \"Style\"): ")
            let category = Console.ReadLine().Trim().Pascalize()
            let categoryLowered = category.ToLower()

            let number =
                Directory.EnumerateFiles(__SOURCE_DIRECTORY__ </> "docs", "*.md", SearchOption.AllDirectories)
                |> Seq.choose (fun fileName ->
                    let name = Path.GetFileNameWithoutExtension(fileName)
                    match Int32.TryParse(name) with
                    | true, result -> Some result
                    | _ -> None
                )
                |> Seq.max
                |> (+) 1

            let camelCasedAnalyzerName = analyzerName.Camelize()

            let analyzerFile =
                __SOURCE_DIRECTORY__
                </> $"src/Ionide.Analyzers/%s{category}/%s{analyzerName}.fs"
                |> FileInfo

            if not analyzerFile.Directory.Exists then
                analyzerFile.Directory.Create()

            let analyzerContent =
                $"""module Ionide.Analyzers.%s{category}.%s{analyzerName}

open System.Collections.Generic
open FSharp.Compiler.Symbols
open FSharp.Compiler.Text
open FSharp.Compiler.Syntax
open FSharp.Compiler.SyntaxTrivia
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting
open FSharp.Analyzers.SDK.TASTCollecting

[<Literal>]
let message = "Great message here"

let private analyze () : Message list =
    [
        {{
            Type = "%s{name}"
            Message = message
            Code = "IONIDE-%03i{number}"
            Severity = Severity.Hint
            Range = Range.Zero
            Fixes = []
        }}
    ]

[<Literal>]
let name = "%s{analyzerName}"

[<Literal>]
let shortDescription =
    "Short description about %s{analyzerName}"

[<Literal>]
let helpUri = "https://ionide.io/ionide-analyzers/%s{categoryLowered}/%03i{number}.html"

[<CliAnalyzer(name, shortDescription, helpUri)>]
let %s{name}CliAnalyzer: Analyzer<CliContext> =
    fun (context: CliContext) -> async {{ return analyze () }}

[<EditorAnalyzer(name, shortDescription, helpUri)>]
let %s{name}EditorAnalyzer: Analyzer<EditorContext> =
    fun (context: EditorContext) -> async {{ return analyze () }}
"""

            File.WriteAllText(analyzerFile.FullName, analyzerContent)
            printfn $"Created %s{analyzerFile.FullName}"

            let addCompileItem relativeFsProj filenameWithoutExtension =
                let fsproj = __SOURCE_DIRECTORY__ </> relativeFsProj
                let sibling = getLastCompileItem fsproj

                if
                    sibling.Attribute(XName.Get "Include").Value
                    <> $"%s{filenameWithoutExtension}.fs"
                then
                    sibling.AddAfterSelf(XElement.Parse $"<Compile Include=\"%s{filenameWithoutExtension}.fs\" />")
                    sibling.Document.Save fsproj

            addCompileItem "src/Ionide.Analyzers/Ionide.Analyzers.fsproj" (sprintf "%s\\%s" category analyzerName)

            let analyzerTestsFile =
                __SOURCE_DIRECTORY__
                </> $"tests/Ionide.Analyzers.Tests/%s{category}/%s{analyzerName}Tests.fs"
                |> FileInfo

            if not analyzerTestsFile.Directory.Exists then
                analyzerTestsFile.Directory.Create()

            let tripleQuote = "\"\"\""

            let analyzerTestsContent =
                $"""module Ionide.Analyzers.Tests.%s{category}.%s{analyzerName}Tests

open NUnit.Framework
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text.Range
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.Testing
open Ionide.Analyzers.%s{category}.%s{analyzerName}

let mutable projectOptions: FSharpProjectOptions = FSharpProjectOptions.zero

[<SetUp>]
let Setup () =
    task {{
        let! opts = mkOptionsFromProject "net7.0" []
        projectOptions <- opts
    }}

[<Test>]
let ``first test here`` () =
    async {{
        let source =
            %s{tripleQuote}module Lib
// Some source here
    %s{tripleQuote}

        let ctx = getContext projectOptions source
        let! msgs = %s{name}CliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains message msg, Is.True)
    }}
"""

            File.WriteAllText(analyzerTestsFile.FullName, analyzerTestsContent)

            addCompileItem
                "tests/Ionide.Analyzers.Tests/Ionide.Analyzers.Tests.fsproj"
                $"%s{category}\\%s{analyzerName}Tests"
            printfn "Created %s" analyzerTestsFile.FullName

            let documentationFile =
                __SOURCE_DIRECTORY__ </> $"docs/%s{categoryLowered}/%03i{number}.md" |> FileInfo

            if not documentationFile.Directory.Exists then
                documentationFile.Directory.Create()

            let documentationContent =
                $"""---
title: %s{analyzerName}
category: %s{categoryLowered}
categoryindex: 1
index: 1
---

# %s{analyzerName}

## Problem

```fsharp

```

## Fix

```fsharp

```
"""

            File.WriteAllText(documentationFile.FullName, documentationContent)
            printfn "Created %s, your frontmatter probably isn't correct though." documentationFile.FullName
        )
    }

    runIfOnlySpecified true
}

tryPrintPipelineCommandHelp ()
