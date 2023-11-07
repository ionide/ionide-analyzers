#r "nuget: Fun.Build, 1.0.2"
#r "nuget: Fake.IO.FileSystem, 6.0.0"

open System.Text.Json
open Fake.IO
open Fake.IO.Globbing.Operators
open Fun.Build

let cleanDirs globExpr = (!!globExpr) |> Shell.cleanDirs

/// Workaround for https://github.com/dotnet/sdk/issues/35989
let restoreTools (ctx: Internal.StageContext) =
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
    stage "pack" { run "dotnet pack ./src/Ionide.Analyzers/Ionide.Analyzers.fsproj -c Release -o bin" }
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

tryPrintPipelineCommandHelp ()
