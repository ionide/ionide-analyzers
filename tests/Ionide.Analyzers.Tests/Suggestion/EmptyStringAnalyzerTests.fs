module Ionide.Analyzers.Tests.Suggestion.EmptyStringAnalyzerTests

open NUnit.Framework
open FSharp.Compiler.CodeAnalysis
open FSharp.Analyzers.SDK.Testing
open Ionide.Analyzers.Suggestion.EmptyStringAnalyzer

let mutable projectOptions: FSharpProjectOptions = FSharpProjectOptions.zero

[<SetUp>]
let Setup () =
    task {
        let! opts = mkOptionsFromProject "net8.0" []

        projectOptions <- opts
    }

[<Test>]
let ``Operator based test for zero-length`` () =
    async {
        let source =
            """
module M

let s = "foo"
let x = s = ""
    """

        let ctx = getContext projectOptions source
        let! msgs = emptyStringCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)

        Assert.That(
            Assert.messageContains
                "Test for empty strings should use the String.Length property or the String.IsNullOrEmpty method."
                msgs[0],
            Is.True
        )
    }

[<Test>]
let ``Operator based test for zero-length with ignore comment`` () =
    async {
        let source =
            """
module M

let s = "foo"
// IGNORE: IONIDE-005
let x = s = ""
    """

        let ctx = getContext projectOptions source
        let! msgs = emptyStringCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``Operator based test for zero-length reversed`` () =
    async {
        let source =
            """
module M

let s = "foo"
let x = "" = s
    """

        let ctx = getContext projectOptions source
        let! msgs = emptyStringCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)

        Assert.That(
            Assert.messageContains
                "Test for empty strings should use the String.Length property or the String.IsNullOrEmpty method."
                msgs[0],
            Is.True
        )
    }

[<Test>]
let ``Operator based test for zero-length reversed with ignore comment`` () =
    async {
        let source =
            """
module M

let s = "foo"
// IGNORE: IONIDE-005
let x = "" = s
    """

        let ctx = getContext projectOptions source
        let! msgs = emptyStringCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``Operator based equality test`` () =
    async {
        let source =
            """
module M

let s = "foo"
let x = s = "bar"
    """

        let ctx = getContext projectOptions source
        let! msgs = emptyStringCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``Operator based equality test reversed`` () =
    async {
        let source =
            """
module M

let s = "foo"
let x = "bar" = s
    """

        let ctx = getContext projectOptions source
        let! msgs = emptyStringCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``Operator based null test`` () =
    async {
        let source =
            """
module M

let s = "foo"
let x = s = null
    """

        let ctx = getContext projectOptions source
        let! msgs = emptyStringCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``Operator based null test reversed`` () =
    async {
        let source =
            """
module M

let s = "foo"
let x = null = s
    """

        let ctx = getContext projectOptions source
        let! msgs = emptyStringCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``Property based length test`` () =
    async {
        let source =
            """
module M

let s = "foo"
let x = s.Length = 0
    """

        let ctx = getContext projectOptions source
        let! msgs = emptyStringCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``Handle types without a FullName gracefully`` () =
    async {
        let source =
            """
module M

let f () =
    let mutable bom = Array.zeroCreate 3
    let mutable bom2 = Array.zeroCreate 3
    bom = bom2
    """

        let ctx = getContext projectOptions source
        let! msgs = emptyStringCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }
