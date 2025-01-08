module Ionide.Analyzers.Tests.Style.PostfixGenericsAnalyzerTests

open NUnit.Framework
open FSharp.Compiler.CodeAnalysis
open FSharp.Analyzers.SDK.Testing
open Ionide.Analyzers.Style.PostfixGenericsAnalyzer

let mutable projectOptions: FSharpProjectOptions = FSharpProjectOptions.zero

[<SetUp>]
let Setup () =
    task {
        let! opts = mkOptionsFromProject "net8.0" []

        projectOptions <- opts
    }

[<Test>]
let ``array should produce diagnostic`` () =
    async {
        let source =
            """
module M

let a (b: int[]) = ()
let b: int[] = Array.empty
    """

        let message = "Prefer postfix syntax for arrays."
        let ctx = getContext projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains message msgs.[0], Is.True)
        Assert.That(Assert.messageContains message msgs.[1], Is.True)
    }

[<Test>]
let ``alt array should produce diagnostic`` () =
    async {
        let source =
            """
module M

let a (b: array<int>) = ()
let b: array<int> = Array.empty
    """

        let message = "Prefer postfix syntax for arrays."
        let ctx = getContext projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains message msgs.[0], Is.True)
        Assert.That(Assert.messageContains message msgs.[1], Is.True)
    }

[<Test>]
let ``array in val sig should produce diagnostic`` () =
    async {
        let source =
            """
module M

val a: b: int[] -> unit
val b: int[]
    """

        let message = "Prefer postfix syntax for arrays."
        let ctx = getContextForSignature projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains message msgs.[0], Is.True)
        Assert.That(Assert.messageContains message msgs.[1], Is.True)
    }

[<Test>]
let ``alt array in val sig should produce diagnostic`` () =
    async {
        let source =
            """
module M

val a: b: array<int> -> unit
val b: array<int>
    """

        let message = "Prefer postfix syntax for arrays."
        let ctx = getContextForSignature projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains message msgs.[0], Is.True)
        Assert.That(Assert.messageContains message msgs.[1], Is.True)
    }

[<Test>]
let ``option should produce diagnostic`` () =
    async {
        let source =
            """
module M

let a (b: option<int>) = ()
let b: option<int> = None
    """

        let message = "Prefer postfix syntax for options."
        let ctx = getContext projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains message msgs.[0], Is.True)
        Assert.That(Assert.messageContains message msgs.[1], Is.True)
    }

[<Test>]
let ``option in val sig should produce diagnostic`` () =
    async {
        let source =
            """
module M

val a: b: option<int> -> unit
val b: option<int>
    """

        let message = "Prefer postfix syntax for options."
        let ctx = getContextForSignature projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains message msgs.[0], Is.True)
        Assert.That(Assert.messageContains message msgs.[1], Is.True)
    }

[<Test>]
let ``value option should produce diagnostic`` () =
    async {
        let source =
            """
module M

let a (b: voption<int>) = ()
let b: voption<int> = ValueNone
    """

        let message = "Prefer postfix syntax for value options."
        let ctx = getContext projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains message msgs.[0], Is.True)
        Assert.That(Assert.messageContains message msgs.[1], Is.True)
    }

[<Test>]
let ``value option in val sig should produce diagnostic`` () =
    async {
        let source =
            """
module M

val a: b: voption<int> -> unit
val b: voption<int>
    """

        let message = "Prefer postfix syntax for value options."
        let ctx = getContextForSignature projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains message msgs.[0], Is.True)
        Assert.That(Assert.messageContains message msgs.[1], Is.True)
    }

[<Test>]
let ``reference cell should produce diagnostic`` () =
    async {
        let source =
            """
module M

let a (b: ref<int>) = ()
let b: ref<int> = ref 0
    """

        let message = "Prefer postfix syntax for reference cells."
        let ctx = getContext projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains message msgs.[0], Is.True)
        Assert.That(Assert.messageContains message msgs.[1], Is.True)
    }

[<Test>]
let ``alt reference cell should produce diagnostic`` () =
    async {
        let source =
            """
module M

let a (b: Ref<int>) = ()
let b: Ref<int> = ref 0
    """

        let message = "Prefer postfix syntax for reference cells."
        let ctx = getContext projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains message msgs.[0], Is.True)
        Assert.That(Assert.messageContains message msgs.[1], Is.True)
    }

[<Test>]
let ``reference cell in val sig should produce diagnostic`` () =
    async {
        let source =
            """
module M

val a: b: ref<int> -> unit
val b: ref<int>
    """

        let message = "Prefer postfix syntax for reference cells."
        let ctx = getContextForSignature projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains message msgs.[0], Is.True)
        Assert.That(Assert.messageContains message msgs.[1], Is.True)
    }

[<Test>]
let ``alt reference cell in val sig should produce diagnostic`` () =
    async {
        let source =
            """
module M

val a: b: Ref<int> -> unit
val b: Ref<int>
    """

        let message = "Prefer postfix syntax for reference cells."
        let ctx = getContextForSignature projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains message msgs.[0], Is.True)
        Assert.That(Assert.messageContains message msgs.[1], Is.True)
    }

[<Test>]
let ``nested generics should produce diagnostic`` () =
    async {
        let source =
            """
module M

let a: array<list<int>> = Array.empty
    """

        let ctx = getContext projectOptions source
        let! msgs = postfixGenericsAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains "Prefer postfix syntax for arrays." msgs[0], Is.True)
        Assert.That(Assert.messageContains "Prefer postfix syntax for lists." msgs[1], Is.True)
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
        Assert.That(msgs, Is.Empty)
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
        Assert.That(msgs, Is.Empty)
    }
