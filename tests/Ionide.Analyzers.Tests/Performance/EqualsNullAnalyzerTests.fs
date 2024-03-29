module Ionide.Analyzers.Tests.Performance.EqualsNullAnalyzerTests

open NUnit.Framework
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text.Range
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.Testing
open Ionide.Analyzers.Performance.EqualsNullAnalyzer

let mutable projectOptions: FSharpProjectOptions = FSharpProjectOptions.zero

[<SetUp>]
let Setup () =
    task {
        let! opts = mkOptionsFromProject "net7.0" []
        projectOptions <- opts
    }

[<Test>]
let ``a = null`` () =
    async {
        let source =
            """module Lib

let a = "3"
do (a = null)
    """

        let ctx = getContext projectOptions source
        let! msgs = equalsNullCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains equalsMessage msg, Is.True)
    }

[<Test>]
let ``null = a`` () =
    async {
        let source =
            """module Lib

let a = "3"
do (null = a)
    """

        let ctx = getContext projectOptions source
        let! msgs = equalsNullCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains equalsMessage msg, Is.True)
    }

[<Test>]
let ``equals fix with parens`` () =
    async {
        let source =
            """module Lib

let a () = "3"
a () = null
|> ignore
    """

        let ctx = getContext projectOptions source
        let! msgs = equalsNullCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains equalsMessage msg, Is.True)
        let fix = msg.Fixes.[0]
        Assert.That("isNull (a ())", Is.EqualTo fix.ToText)
    }

[<Test>]
let ``equals fix without parens`` () =
    async {
        let source =
            """module Lib

"meh" = null
|> ignore
    """

        let ctx = getContext projectOptions source
        let! msgs = equalsNullCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains equalsMessage msg, Is.True)
        let fix = msg.Fixes.[0]
        Assert.That("isNull \"meh\"", Is.EqualTo fix.ToText)
    }

[<Test>]
let ``a <> null`` () =
    async {
        let source =
            """module Lib

let a = "3"
do (a <> null)
    """

        let ctx = getContext projectOptions source
        let! msgs = equalsNullCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains inEqualsMessage msg, Is.True)
    }

[<Test>]
let ``null <> a`` () =
    async {
        let source =
            """module Lib

let a = "3"
do (null <> a)
    """

        let ctx = getContext projectOptions source
        let! msgs = equalsNullCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains inEqualsMessage msg, Is.True)
    }

[<Test>]
let ``inequals fix with parens`` () =
    async {
        let source =
            """module Lib

let a () = "3"
a () <> null
|> ignore
    """

        let ctx = getContext projectOptions source
        let! msgs = equalsNullCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains inEqualsMessage msg, Is.True)
        let fix = msg.Fixes.[0]
        Assert.That("not (isNull (a ()))", Is.EqualTo fix.ToText)
    }

[<Test>]
let ``inequals fix without parens`` () =
    async {
        let source =
            """module Lib

"meh" <> null
|> ignore
    """

        let ctx = getContext projectOptions source
        let! msgs = equalsNullCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains inEqualsMessage msg, Is.True)
        let fix = msg.Fixes.[0]
        Assert.That("not (isNull \"meh\")", Is.EqualTo fix.ToText)
    }

[<Test>]
let ``named ctor parameter does not trigger`` () =
    async {
        let source =
            """module Lib

type X(y:obj) = class end

let x = X(y = null)
    """

        let ctx = getContext projectOptions source
        let! msgs = equalsNullCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }
