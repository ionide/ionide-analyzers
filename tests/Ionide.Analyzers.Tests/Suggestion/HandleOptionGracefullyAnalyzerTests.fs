module Ionide.Analyzers.Tests.Suggestion.HandleOptionGracefullyAnalyzerTests

open NUnit.Framework
open FSharp.Compiler.CodeAnalysis
open FSharp.Analyzers.SDK.Testing
open Ionide.Analyzers.Suggestion.HandleOptionGracefullyAnalyzer

let mutable projectOptions: FSharpProjectOptions = FSharpProjectOptions.zero

let messageString =
    "Replace unsafe option unwrapping with graceful handling of each case."

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
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains messageString msgs[0], Is.True)
    }

[<Test>]
let ``ValueOption.get is detected`` () =
    async {
        let source =
            """
module M

let voption = ValueSome 10
let value = ValueOption.get voption
    """

        let ctx = getContext projectOptions source
        let! msgs = optionGetAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains messageString msgs[0], Is.True)
    }

[<Test>]
let ``Option.Value member is detected`` () =
    async {
        let source =
            """
module M

let option = Some 10
let value = option.Value
    """

        let ctx = getContext projectOptions source
        let! msgs = optionGetAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains messageString msgs[0], Is.True)
    }

[<Test>]
let ``ValueOption.Value member is detected`` () =
    async {
        let source =
            """
module M

let voption = ValueSome 10
let value = voption.Value
    """

        let ctx = getContext projectOptions source
        let! msgs = optionGetAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains messageString msgs[0], Is.True)
    }
