module Ionide.Analyzers.Tests.Performance.ReturnStructPartialActivePatternAnalyzerTests

open NUnit.Framework
open FSharp.Compiler.CodeAnalysis
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.Testing
open Ionide.Analyzers.Performance.ReturnStructPartialActivePatternAnalyzer

let mutable projectOptions: FSharpProjectOptions = FSharpProjectOptions.zero

[<SetUp>]
let Setup () =
    task {
        let! opts =
            mkOptionsFromProject
                "net7.0"
                [
                    {
                        Name = "FSharp.Compiler.Service"
                        Version = "43.8.200"
                    }
                ]

        projectOptions <- opts
    }

[<Test>]
let ``partial active pattern with argument`` () =
    async {
        let source =
            """module Lib

let (|IsEvent|_|) (a:int) = if a % 2 = 0 then Some () else None
    """

        let ctx = getContext projectOptions source
        let! msgs = returnStructPartialActivePatternCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains message msg, Is.True)
    }

[<Test>]
let ``partial active pattern without argument`` () =
    async {
        let source =
            """module Lib

let (|IsOneOrTwo|_|) = function | 1 -> Some() | 2 -> Some() | _ -> None
    """

        let ctx = getContext projectOptions source
        let! msgs = returnStructPartialActivePatternCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains message msg, Is.True)
    }

[<Test>]
let ``negative: optimized partial active pattern`` () =
    async {
        let source =
            """module Lib

[<return: Struct>]
let (|IsEvent|_|) (a:int) = if a % 2 = 0 then ValueSome () else ValueNone
    """

        let ctx = getContext projectOptions source
        let! msgs = returnStructPartialActivePatternCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``fix data from match clauses`` () =
    async {
        let source =
            """module Lib

let (|IsEvent|_|) (a:int) =
    let x y = ()
    match a with
    | even when even % 2 = 0 -> Some()
    | _ -> None
    """

        let ctx = getContext projectOptions source
        let! msgs = returnStructPartialActivePatternCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(msg.Fixes.Length, Is.EqualTo 3)
    }

[<Test>]
let ``fix data from match lambda`` () =
    async {
        let source =
            """module Lib

let (|IsOneOrTwo|_|) = function
    | 1 -> Some ()
    | 2 -> Some ()
    | 3 -> None
    | _ -> None
    """

        let ctx = getContext projectOptions source
        let! msgs = returnStructPartialActivePatternCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(msg.Fixes.Length, Is.EqualTo 5)
    }

[<Test>]
let ``fix data from binding with return type`` () =
    async {
        let source =
            """module Lib

let (|IsEvent|_|) (a:int) : unit option =
    let x y = ()
    match a with
    | even when even % 2 = 0 -> Some()
    | _ -> None
    """

        let ctx = getContext projectOptions source
        let! msgs = returnStructPartialActivePatternCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(msg.Fixes.Length, Is.EqualTo 4)
    }

// We are not detecting the None in the visit for now.
// Out of scope
[<Test>]
let ``fix data from binding with return type + if then else`` () =
    async {
        let source =
            """module Lib

let (|EndsWithDualListApp|_|) _ : unit option =
    if not false then
        None
    else
        let mutable otherArgs = null

        let rec visit (args: int list) =
            match args with
            | [] -> None
            | _ -> None

        visit []
    """

        let ctx = getContext projectOptions source
        let! msgs = returnStructPartialActivePatternCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(msg.Fixes.Length, Is.EqualTo 3)
    }

[<Test>]
let ``fix data from recursive binding`` () =
    async {
        let source =
            """module Lib

open FSharp.Compiler.Syntax

let rec (|OpenL|_|) =
    function
    | SynModuleDecl.Open(target, range) :: OpenL(xs, ys) -> Some((target, range) :: xs, ys)
    | SynModuleDecl.Open(target, range) :: ys -> Some([ target, range ], ys)
    | _ -> None
    """

        let ctx = getContext projectOptions source
        let! msgs = returnStructPartialActivePatternCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(msg.Fixes.Length, Is.EqualTo 4)
    }

[<Test>]
let ``fix data for indented binding`` () =
    async {
        let source =
            """namespace Lib

module Foo =

    open FSharp.Compiler.Syntax

    let rec (|OpenL|_|) =
        function
        | SynModuleDecl.Open(target, range) :: OpenL(xs, ys) -> Some((target, range) :: xs, ys)
        | SynModuleDecl.Open(target, range) :: ys -> Some([ target, range ], ys)
        | _ -> None
    """

        let ctx = getContext projectOptions source
        let! msgs = returnStructPartialActivePatternCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        let attributeFix = msg.Fixes.[0]
        Assert.That("[<return: Struct>]\n    ", Is.EqualTo attributeFix.ToText)
    }
