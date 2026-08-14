module Ionide.Analyzers.Tests.Style.MixedPipeDirectionAnalyzerTests

open NUnit.Framework
open FSharp.Compiler.CodeAnalysis
open FSharp.Analyzers.SDK.Testing
open Ionide.Analyzers.Style.MixedPipeDirectionAnalyzer

let mutable projectOptions: FSharpProjectOptions = FSharpProjectOptions.zero

[<SetUp>]
let Setup () =
    task {
        let! opts = mkOptionsFromProject "net8.0" []
        projectOptions <- opts
    }

[<Test>]
let ``forward then backward on a single line should produce diagnostic`` () =
    async {
        let source =
            """
module M

let add (x: int) (y: int) = x + y
let a b c = b |> add <| c
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains message msgs.[0], Is.True)
    }

[<Test>]
let ``backward then forward on a single line should produce diagnostic`` () =
    async {
        let source =
            """
module M

let a b = string <| b |> ignore
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains message msgs.[0], Is.True)
    }

[<Test>]
let ``mixed pipes spread over multiple lines should produce diagnostic`` () =
    async {
        let source =
            """
module M

let wrap (prefix: string) (suffix: string) = prefix + suffix

let a (items: int list) =
    items
    |> List.map string
    |> List.filter (fun s -> s <> "")
    |> String.concat ", "
    |> wrap
    <| "!"
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        Assert.That(Assert.messageContains message msgs.[0], Is.True)
    }

[<Test>]
let ``a mixed chain should only be reported once`` () =
    async {
        let source =
            """
module M

let add (x: int) (y: int) = x + y
let a b c d = b |> add <| c |> add <| d
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Has.Exactly(1).Items)
    }

[<Test>]
let ``the reported range should span the first and last operator`` () =
    async {
        let source =
            """
module M

let add (x: int) (y: int) = x + y
let a b c = b |> add <| c
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let range = msgs.[0].Range
        Assert.That(range.StartLine, Is.EqualTo 5)
        Assert.That(range.StartColumn, Is.EqualTo 14)
        Assert.That(range.EndLine, Is.EqualTo 5)
        Assert.That(range.EndColumn, Is.EqualTo 23)
    }

[<Test>]
let ``two mixed chains should produce two diagnostics`` () =
    async {
        let source =
            """
module M

let add (x: int) (y: int) = x + y
let a b c = b |> add <| c
let d e f = e |> add <| f
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Has.Exactly(2).Items)
    }

[<Test>]
let ``double forward then single backward should produce diagnostic`` () =
    async {
        let source =
            """
module M

let f3 (a: int) (b: int) (c: int) = a + b + c
let a x y z = (x, y) ||> f3 <| z
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Has.Exactly(1).Items)
        Assert.That(Assert.messageContains message msgs.[0], Is.True)
    }

[<Test>]
let ``triple forward then single backward should produce diagnostic`` () =
    async {
        let source =
            """
module M

let f4 (a: int) (b: int) (c: int) (d: int) = a + b + c + d
let a x y z w = (x, y, z) |||> f4 <| w
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Has.Exactly(1).Items)
    }

[<Test>]
let ``single forward then double backward should produce diagnostic`` () =
    async {
        let source =
            """
module M

let f3 (a: int) (b: int) (c: int) = a + b + c
let a x y z = x |> f3 <|| (y, z)
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Has.Exactly(1).Items)
    }

[<Test>]
let ``single forward then triple backward should produce diagnostic`` () =
    async {
        let source =
            """
module M

let f4 (a: int) (b: int) (c: int) (d: int) = a + b + c + d
let a x y z w = x |> f4 <||| (y, z, w)
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Has.Exactly(1).Items)
    }

[<Test>]
let ``double backward then single forward should produce diagnostic`` () =
    async {
        let source =
            """
module M

let f2 (a: int) (b: int) = a + b
let a x y = f2 <|| (x, y) |> string
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Has.Exactly(1).Items)
    }

[<Test>]
let ``single backward then double forward should produce diagnostic`` () =
    async {
        let source =
            """
module M

let mkPair (n: int) = n, n
let f2 (a: int) (b: int) = a + b
let a x = mkPair <| x ||> f2
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Has.Exactly(1).Items)
    }

[<Test>]
let ``single backward then triple forward should produce diagnostic`` () =
    async {
        let source =
            """
module M

let mkTriple (n: int) = n, n, n
let f3 (a: int) (b: int) (c: int) = a + b + c
let a x = mkTriple <| x |||> f3
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Has.Exactly(1).Items)
    }

[<Test>]
let ``forward pipes of different arity should not trigger diagnostic`` () =
    async {
        let source =
            """
module M

let f2 (a: int) (b: int) = a + b
let a x y = (x, y) ||> f2 |> string
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``backward pipes of different arity should not trigger diagnostic`` () =
    async {
        let source =
            """
module M

let f3 (a: int) (b: int) (c: int) = a + b + c
let a x y z = f3 <|| (x, y) <| z
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``a forward only pipeline should not trigger diagnostic`` () =
    async {
        let source =
            """
module M

let a b =
    b
    |> List.map string
    |> List.filter (fun s -> s <> "")
    |> String.concat ", "
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``a backward only application should not trigger diagnostic`` () =
    async {
        let source =
            """
module M

let a b = failwith <| sprintf "unexpected: %s" b
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``pipes in separate expressions should not trigger diagnostic`` () =
    async {
        let source =
            """
module M

let a b =
    b |> List.length |> ignore
    printfn "%s" <| string b
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``a backward pipe nested in a lambda should not trigger diagnostic`` () =
    async {
        let source =
            """
module M

let a b =
    b
    |> List.map (fun x -> string <| x + 1)
    |> List.length
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }

[<Test>]
let ``a parenthesized inner pipeline is its own expression`` () =
    async {
        let source =
            """
module M

let a b = ignore <| (b |> List.length)
    """

        let ctx = getContext projectOptions source
        let! msgs = mixedPipeDirectionCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }
