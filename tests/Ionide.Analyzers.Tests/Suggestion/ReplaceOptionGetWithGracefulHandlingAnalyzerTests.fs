module Ionide.Analyzers.Tests.Suggestion.ReplaceOptionGetWithGracefulHandlingAnalyzerTests

open NUnit.Framework
open FSharp.Compiler.CodeAnalysis
open FSharp.Analyzers.SDK.Testing
open Ionide.Analyzers.Suggestion.ReplaceOptionGetWithGracefulHandlingAnalyzer

let mutable projectOptions: FSharpProjectOptions = FSharpProjectOptions.zero

[<SetUp>]
let Setup () =
    task {
        let! opts = mkOptionsFromProject "net7.0" []
        projectOptions <- opts
    }

[<Test>]
let ``Option.get is detected`` () =
    async {
        let source =
            """
module M

let option = Some 10
let value = Option.get option
    """

        let ctx = getContext projectOptions source
        let! msgs = optionGetAnalyzer ctx
        Assert.IsNotEmpty msgs
        Assert.IsTrue(Assert.messageContains "Replace Option.get with graceful handling of each case." msgs[0])
    }
