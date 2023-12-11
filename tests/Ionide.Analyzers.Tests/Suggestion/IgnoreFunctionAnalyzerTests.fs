module Ionide.Analyzers.Tests.Suggestion.IgnoreFunctionAnalyzerTests

open NUnit.Framework
open FSharp.Compiler.CodeAnalysis
open FSharp.Analyzers.SDK.Testing
open Ionide.Analyzers.Suggestion.IgnoreFunctionAnalyzer

let mutable projectOptions: FSharpProjectOptions = FSharpProjectOptions.zero

[<SetUp>]
let Setup () =
    task {
        let! opts = mkOptionsFromProject "net7.0" []

        projectOptions <- opts
    }

[<Test>]
let ``ignore incomplete partially applied function`` () =
    async {
        let source =
            """
module M

let f g h = g + h
let a = f 1
ignore a
    """

        let ctx = getContext projectOptions source
        let! msgs = ignoreFunctionAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)

        Assert.That(
            Assert.messageContains "A function is being ignored. Did you mean to execute this?" msgs[0],
            Is.True
        )
    }

[<Test>]
let ``method ignore`` () =
    async {
        let source =
            """
module M

open System.Threading.Channels

let channel = Channel.CreateUnbounded<int>();
channel.Writer.Complete |> ignore 
    """

        let ctx = getContext projectOptions source
        let! msgs = ignoreFunctionAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)

        Assert.That(
            Assert.messageContains "A function is being ignored. Did you mean to execute this?" msgs[0],
            Is.True
        )
    }

[<Test>]
let ``ignore with function type parameter`` () =
    async {
        let source =
            """
module M

ignore<int -> int -> int> (+)
    """

        let ctx = getContext projectOptions source
        let! msgs = ignoreFunctionAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)

        Assert.That(
            Assert.messageContains "A function is being ignored. Did you mean to execute this?" msgs[0],
            Is.True
        )
    }
