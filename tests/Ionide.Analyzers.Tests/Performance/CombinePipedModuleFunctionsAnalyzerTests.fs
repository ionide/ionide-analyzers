module Ionide.Analyzers.Tests.Performance.CombinePipedModuleFunctionsAnalyzerTests

open NUnit.Framework
open FSharp.Compiler.CodeAnalysis
open FSharp.Analyzers.SDK.Testing
open Ionide.Analyzers.Performance.CombinePipedModuleFunctionsAnalyzer

let mutable projectOptions: FSharpProjectOptions = FSharpProjectOptions.zero

[<SetUp>]
let Setup () =
    task {
        let! opts = mkOptionsFromProject "net8.0" []
        projectOptions <- opts
    }

let collectionTypes = [ "List"; "Seq"; "Array"; "Set" ]

[<TestCaseSource(nameof collectionTypes)>]
let ``pipe map to map`` moduleName =
    async {
        let source =
            $"""module Lib

let a b =
    b
    |> %s{moduleName}.map (fun x -> x)
    |> %s{moduleName}.map (fun y -> y)
    """

        let ctx = getContext projectOptions source
        let! msgs = combinePipedModuleFunctionsCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains $"%s{moduleName}.map is being piped into %s{moduleName}.map" msg, Is.True)
    }

[<TestCaseSource(nameof collectionTypes)>]
let ``pipe map to map with ignore comment`` moduleName =
    async {
        let source =
            $"""module Lib

let a b =
    b
    // IGNORE: IONIDE-010
    |> %s{moduleName}.map (fun x -> x)
    |> %s{moduleName}.map (fun y -> y)
    """

        let ctx = getContext projectOptions source
        let! msgs = combinePipedModuleFunctionsCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<TestCaseSource(nameof collectionTypes)>]
let ``pipe filter to map`` moduleName =
    async {
        let source =
            $"""module Lib

let a b =
    b
    |> %s{moduleName}.filter (fun x -> x)
    |> %s{moduleName}.map (fun y -> y)
    """

        let ctx = getContext projectOptions source
        let! msgs = combinePipedModuleFunctionsCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]

        Assert.That(
            Assert.messageContains
                $"%s{moduleName}.filter |> %s{moduleName}.map can be combined into %s{moduleName}.choose"
                msg,
            Is.True
        )
    }

[<TestCaseSource(nameof collectionTypes)>]
let ``pipe filter to map with ignore comment`` moduleName =
    async {
        let source =
            $"""module Lib

let a b =
    b
    // IGNORE: IONIDE-010
    |> %s{moduleName}.filter (fun x -> x)
    |> %s{moduleName}.map (fun y -> y)
    """

        let ctx = getContext projectOptions source
        let! msgs = combinePipedModuleFunctionsCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<TestCaseSource(nameof collectionTypes)>]
let ``pipe map to filter`` moduleName =
    async {
        let source =
            $"""module Lib

let a b =
    b
    |> %s{moduleName}.map (fun x -> x)
    |> %s{moduleName}.filter (fun y -> y)
    """

        let ctx = getContext projectOptions source
        let! msgs = combinePipedModuleFunctionsCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]

        Assert.That(
            Assert.messageContains
                $"%s{moduleName}.map |> %s{moduleName}.filter can be combined into %s{moduleName}.choose"
                msg,
            Is.True
        )
    }

[<TestCaseSource(nameof collectionTypes)>]
let ``pipe map to filter with ignore comment`` moduleName =
    async {
        let source =
            $"""module Lib

let a b =
    b
    // IGNORE: IONIDE-010
    |> %s{moduleName}.map (fun x -> x)
    |> %s{moduleName}.filter (fun y -> y)
    """

        let ctx = getContext projectOptions source
        let! msgs = combinePipedModuleFunctionsCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }
