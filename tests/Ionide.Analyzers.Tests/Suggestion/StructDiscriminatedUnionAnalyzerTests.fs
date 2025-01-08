module Ionide.Analyzers.Tests.Suggestion.StructDiscriminatedUnionAnalyzerTests

open NUnit.Framework
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text.Range
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.Testing
open Ionide.Analyzers.Suggestion.StructDiscriminatedUnionAnalyzer

let mutable projectOptions: FSharpProjectOptions = FSharpProjectOptions.zero

[<SetUp>]
let Setup () =
    task {
        let! opts = mkOptionsFromProject "net8.0" []
        projectOptions <- opts
    }

[<Test>]
let ``du without any field values`` () =
    async {
        let source =
            """module Lib

type Foo =
    | Bar
    | Barry
    """

        let ctx = getContext projectOptions source
        let! msgs = structDiscriminatedUnionCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains message msg, Is.True)
    }

[<Test>]
let ``negative: du is already struct`` () =
    async {
        let source =
            """module Lib

[<Struct>]
type Foo =
    | Bar
    | Barry
    """

        let ctx = getContext projectOptions source
        let! msgs = structDiscriminatedUnionCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``negative: du is already struct (full StructAttribute name)`` () =
    async {
        let source =
            """module Lib

[<StructAttribute>]
type Foo =
    | Bar
    | Barry
    """

        let ctx = getContext projectOptions source
        let! msgs = structDiscriminatedUnionCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

// A special test case for https://github.com/ionide/ionide-analyzers/issues/102
[<Test>]
let ``du with a StructuredFormatDisplay attribute`` () =
    async {
        let source =
            """module Lib

[<StructuredFormatDisplay("{DisplayText}")>]
type Foo =
    | Bar
    | Barry

    member this.DisplayText = ""
    """

        let ctx = getContext projectOptions source
        let! msgs = structDiscriminatedUnionCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains message msg, Is.True)
    }

[<Test>]
let ``du with only primitive field values`` () =
    async {
        let source =
            """module Lib

type Foo =
    | Bar of int
    | Barry of float
    | Bear of System.DateTime
    | B4 of string
    | B5 of System.TimeSpan
    | B6 of System.Guid
    | B7 of int16 
    | B8 of int64
    | B9 of uint
    | B10 of uint16
    | B11 of byte
    | B12 of sbyte
    | B13 of float32
    | B14 of decimal
    | B15 of char
    | B16 of bool
    """

        let ctx = getContext projectOptions source
        let! msgs = structDiscriminatedUnionCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains message msg, Is.True)
    }

[<Test>]
let ``negative: du with tuple fields`` () =
    async {
        let source =
            """module Lib

type Foo =
    | Bar of int * int
    """

        let ctx = getContext projectOptions source
        let! msgs = structDiscriminatedUnionCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``negative: du with anonymous type`` () =
    async {
        let source =
            """module Lib

type ParsedCell =
    | Code of
        {| lang: string
           source: string
           outputs: string[] option |}
    | Markdown of source: string
    """

        let ctx = getContext projectOptions source
        let! msgs = structDiscriminatedUnionCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``fix data for simple type`` () =
    async {
        let source =
            """module Lib

type Foo =
    | Bar of int
    | Barry
    """

        let ctx = getContext projectOptions source
        let! msgs = structDiscriminatedUnionCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains message msg, Is.True)
        let fix = msg.Fixes[0]
        Assert.That("[<Struct>]\n", Is.EqualTo fix.ToText)
    }

[<Test>]
let ``fix data for indented type `` () =
    async {
        let source =
            """namespace Lib

module N =
    type Foo =
        | Bar of int
        | Barry
    """

        let ctx = getContext projectOptions source
        let! msgs = structDiscriminatedUnionCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains message msg, Is.True)
        let fix = msg.Fixes[0]
        Assert.That("[<Struct>]\n    ", Is.EqualTo fix.ToText)
    }

[<Test>]
let ``fix data for recursive type`` () =
    async {
        let source =
            """module Lib

type X = int 

and Foo =
    | Bar of int
    | Barry
    """

        let ctx = getContext projectOptions source
        let! msgs = structDiscriminatedUnionCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains message msg, Is.True)
        let fix = msg.Fixes[0]
        Assert.That("[<Struct>] ", Is.EqualTo fix.ToText)
    }

[<Test>]
let ``negative: du field with function type`` () =
    async {
        let source =
            """module Lib

[<Struct>]
type Foo =
    | Bar of (int -> int)
    | Barry
    """

        let ctx = getContext projectOptions source
        let! msgs = structDiscriminatedUnionCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``negative: int array`` () =
    async {
        let source =
            """module Foo

type SingleCaseDU = | One of int[]
type TwoCaseDU = | Empty | Full of int[]
    """

        let ctx = getContext projectOptions source
        let! msgs = structDiscriminatedUnionCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }
