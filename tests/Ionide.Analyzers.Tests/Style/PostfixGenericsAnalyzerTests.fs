module Ionide.Analyzers.Tests.Style.PostfixGenericsAnalyzerTests

open NUnit.Framework
open FSharp.Compiler.CodeAnalysis
open FSharp.Analyzers.SDK.Testing
open Ionide.Analyzers.Style.PostfixGenericsAnalyzer

let mutable projectOptions: FSharpProjectOptions = FSharpProjectOptions.zero

[<SetUp>]
let Setup () =
    task {
        let! opts = mkOptionsFromProject "net7.0" []

        projectOptions <- opts
    }

[<Test>]
let ``array in binding`` () =
    async {
        let source =
            """
module M

let a (b: int[]) = ()
    """

        let ctx = getContext projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.IsNotEmpty msgs
        Assert.IsTrue(Assert.messageContains "Prefer postfix syntax for arrays." msgs[0])
    }

[<Test>]
let ``alt array in binding`` () =
    async {
        let source =
            """
module M

let a (b: array<int>) = ()
    """

        let ctx = getContext projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.IsNotEmpty msgs
        Assert.IsTrue(Assert.messageContains "Prefer postfix syntax for arrays." msgs[0])
    }

[<Test>]
let ``array in val sig`` () =
    async {
        let source =
            """
module M

val a: b: int[] -> unit
    """

        let ctx = getContextForSignature projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.IsNotEmpty msgs
        Assert.IsTrue(Assert.messageContains "Prefer postfix syntax for arrays." msgs[0])
    }

[<Test>]
let ``option in binding`` () =
    async {
        let source =
            """
module M

let a (b: option<int>) = ()
    """

        let ctx = getContext projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.IsNotEmpty msgs
        Assert.IsTrue(Assert.messageContains "Prefer postfix syntax for options." msgs[0])
    }

[<Test>]
let ``option in val sig`` () =
    async {
        let source =
            """
module M

val a: b: option<int> -> unit
    """

        let ctx = getContextForSignature projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.IsNotEmpty msgs
        Assert.IsTrue(Assert.messageContains "Prefer postfix syntax for options." msgs[0])
    }

[<Test>]
let ``value option in binding`` () =
    async {
        let source =
            """
module M

let a (b: voption<int>) = ()
    """

        let ctx = getContext projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.IsNotEmpty msgs
        Assert.IsTrue(Assert.messageContains "Prefer postfix syntax for value options." msgs[0])
    }

[<Test>]
let ``value option in val sig`` () =
    async {
        let source =
            """
module M

val a: b: voption<int> -> unit
    """

        let ctx = getContextForSignature projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.IsNotEmpty msgs
        Assert.IsTrue(Assert.messageContains "Prefer postfix syntax for value options." msgs[0])
    }

[<Test>]
let ``reference cell in binding`` () =
    async {
        let source =
            """
module M

let a (b: ref<int>) = ()
    """

        let ctx = getContext projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.IsNotEmpty msgs
        Assert.IsTrue(Assert.messageContains "Prefer postfix syntax for reference cells." msgs[0])
    }

[<Test>]
let ``reference cell in val sig`` () =
    async {
        let source =
            """
module M

val a: b: ref<int> -> unit
    """

        let ctx = getContextForSignature projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.IsNotEmpty msgs
        Assert.IsTrue(Assert.messageContains "Prefer postfix syntax for reference cells." msgs[0])
    }

[<Test>]
let ``postfix generics should not trigger diagnostic`` () =
    async {
        let source =
            """
module M

let a (name: int array) = ()
let b (name: int list) = ()
let c (name: int seq) = ()
let d (name: int option) = ()
let e (name: int voption) = ()
let f (name: int ref) = ()
    """

        let ctx = getContext projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.IsEmpty msgs
    }

let ``postfix generics should not trigger diagnostic in sig file`` () =
    async {
        let source =
            """
module M

val a: name: int array -> unit
val b: name: int list -> unit
val c: name: int seq -> unit
val d: name: int option -> unit
val e: name: int voption -> unit
val f: name: int ref -> unit
    """

        let ctx = getContextForSignature projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.IsEmpty msgs
    }
