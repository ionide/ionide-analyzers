#r "nuget: Fun.Build, 1.0.2"
#r "nuget: Fake.IO.FileSystem, 6.0.0"

open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fun.Build

let cleanDirs globExpr = (!!globExpr) |> Shell.cleanDirs

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
        run "dotnet fsdocs watch --port 7890"
    }
    runIfOnlySpecified true
}

tryPrintPipelineCommandHelp ()
