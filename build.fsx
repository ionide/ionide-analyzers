#r "nuget: Fun.Build, 1.0.2"
#r "nuget: Fake.IO.FileSystem, 6.0.0"
#r "nuget: NuGet.Protocol, 6.7.0"
#r "nuget: Ionide.KeepAChangelog, 0.1.8"
#r "nuget: Humanizer.Core, 2.14.1"

open System
open System.Text.Json
open System.Threading
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fun.Build
open Fun.Build.Internal
open NuGet.Common
open NuGet.Protocol
open NuGet.Protocol.Core.Types
open Ionide.KeepAChangelog
open Ionide.KeepAChangelog.Domain
open SemVersion
open Humanizer

let cleanDirs globExpr = (!!globExpr) |> Shell.cleanDirs

/// Workaround for https://github.com/dotnet/sdk/issues/35989
let restoreTools (ctx: StageContext) =
    async {
        let json = File.readAsString ".config/dotnet-tools.json"
        let jsonDocument = JsonDocument.Parse(json)
        let root = jsonDocument.RootElement
        let tools = root.GetProperty("tools")

        let! installs =
            tools.EnumerateObject()
            |> Seq.map (fun tool ->
                let version = tool.Value.GetProperty("version").GetString()
                ctx.RunCommand $"dotnet tool install %s{tool.Name} --version %s{version}"
            )
            |> Async.Sequential

        let failedInstalls =
            installs
            |> Array.tryPick (
                function
                | Ok _ -> None
                | Error error -> Some error
            )

        match failedInstalls with
        | None -> return 0
        | Some error ->
            printfn $"%s{error}"
            return 1
    }

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
        run restoreTools
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
        run restoreTools
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
    let changelog = System.IO.FileInfo(__SOURCE_DIRECTORY__ </> "CHANGELOG.md")
    let changeLogResult =
        match Parser.parseChangeLog changelog with
        | Error error -> failwithf "%A" error
        | Ok result -> result

    changeLogResult.Releases
    |> List.sortByDescending (fun (_, d, _) -> d)
    |> List.head

/// Push *.nupkg
let releaseNuGetPackage (ctx: StageContext) (version: SemanticVersion, _, _) =
    async {
        let key = Environment.GetEnvironmentVariable "NUGET_KEY"
        printfn
            $"dotnet nuget push bin/Ionide.Analyzers.%s{string version}.nupkg --api-key {key} --source \"https://api.nuget.org/v3/index.json\""

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

let getReleaseNotes (ctx: StageContext) (currentRelease: GithubRelease) (previousReleaseDate: string option) =
    async {
        let closedFilter =
            match previousReleaseDate with
            | None -> ""
            | Some date -> $"closed:>%s{date}"

        let! authorsStdOut =
            ctx.RunCommandCaptureOutput
                $"gh pr list -S \"state:closed base:main %s{closedFilter} -author:app/robot -author:app/dependabot\" --json author"

        let authorMsg =
            match authorsStdOut with
            | Error e -> failwithf $"Could not get authors: %s{e}"
            | Ok stdOut ->

            let authors =
                let jsonDocument = JsonDocument.Parse(stdOut)
                jsonDocument.RootElement.EnumerateArray()
                |> Seq.map (fun item -> item.GetProperty("author").GetProperty("login").GetString())
                |> Seq.distinct
                |> Seq.sort
                |> Seq.toArray

            printfn "AUTHORS: %A" authors

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
    (ctx: StageContext)
    (currentVersion: SemanticVersion * DateTime * ChangelogData option)
    (previousReleaseDate: string option)
    =
    async {
        let ghReleaseInfo = mapToGithubRelease currentVersion
        let! notes = getReleaseNotes ctx ghReleaseInfo previousReleaseDate
        let noteFile = System.IO.Path.GetTempFileName()
        System.IO.File.WriteAllText(noteFile, notes)
        let file = $"./bin/Ionide.Analyzers.%s{ghReleaseInfo.Version}.nupkg"

        let! releaseResult =
            ctx.RunCommand
                $"gh release create v%s{ghReleaseInfo.Version} {file} --title \"{ghReleaseInfo.Title}\" --notes-file \"{noteFile}\""

        if System.IO.File.Exists noteFile then
            System.IO.File.Delete(noteFile)

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
                let currentVersion = getLatestChangeLogVersion ()
                let currentVersionText, _, _ = currentVersion
                let! latestNugetVersion = getLatestPublishedNugetVersion "Ionide.Analyzers" |> Async.AwaitTask
                match latestNugetVersion with
                | None ->
                    let! nugetResult = releaseNuGetPackage ctx currentVersion
                    let! githubResult = mkGitHubRelease ctx currentVersion None
                    return nugetResult + githubResult

                | Some nugetVersion when (nugetVersion.OriginalVersion <> string currentVersionText) ->
                    let! nugetResult = releaseNuGetPackage ctx currentVersion
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

                    let! githubResult = mkGitHubRelease ctx currentVersion previousReleaseDate
                    return nugetResult + githubResult

                | Some nugetVersion ->
                    printfn "%s is already published" nugetVersion.OriginalVersion
                    return 0
            }
        )
    }
    runIfOnlySpecified true
}

tryPrintPipelineCommandHelp ()
