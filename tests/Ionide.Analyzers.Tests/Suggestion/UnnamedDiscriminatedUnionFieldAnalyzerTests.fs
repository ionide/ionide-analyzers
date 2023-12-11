module Ionide.Analyzers.Tests.Suggestion.UnnamedDiscriminatedUnionFieldAnalyzerTests

open NUnit.Framework
open FSharp.Compiler.CodeAnalysis
open FSharp.Analyzers.SDK.Testing
open Ionide.Analyzers.Suggestion.UnnamedDiscriminatedUnionFieldAnalyzer

let mutable projectOptions: FSharpProjectOptions = FSharpProjectOptions.zero

[<SetUp>]
let Setup () =
    task {
        let! opts = mkOptionsFromProject "net7.0" []
        projectOptions <- opts
    }

[<Test>]
let ``one named field and one unnamed field`` () =
    async {
        let source =
            """
module M

type DU =
    | Foo of bar: string * int
    """

        let ctx = getContext projectOptions source
        let! msgs = unnamedDiscriminatedUnionFieldAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains "Field inside union case is not named!" msgs[0], Is.True)
    }

[<Test>]
let ``single unnamed field shouldn't trigger`` () =
    async {
        let source =
            """
module M

type DU =
    | Foo of int
    """

        let ctx = getContext projectOptions source
        let! msgs = unnamedDiscriminatedUnionFieldAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``fields in exception are detected`` () =
    async {
        let source =
            """
module M

exception Exception3 of int * noOkCase: string // kind of not ok
    """

        let ctx = getContext projectOptions source
        let! msgs = unnamedDiscriminatedUnionFieldAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains "Field inside union case is not named!" msgs[0], Is.True)
    }

[<Test>]
let ``union case without fields shouldn't trigger`` () =
    async {
        let source =
            """
module M

type TriviaContent =
    | Newline
    """

        let ctx = getContext projectOptions source
        let! msgs = unnamedDiscriminatedUnionFieldAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }
