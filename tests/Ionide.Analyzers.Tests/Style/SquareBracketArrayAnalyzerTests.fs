module Ionide.Analyzers.Tests.Style.SquareBracketArrayAnalyzerTests

open NUnit.Framework
open FSharp.Compiler.CodeAnalysis
open FSharp.Analyzers.SDK.Testing
open Ionide.Analyzers.Style.SquareBracketArrayAnalyzer

let mutable projectOptions: FSharpProjectOptions = FSharpProjectOptions.zero

[<SetUp>]
let Setup () =
    task {
        let! opts = mkOptionsFromProject "net7.0" []

        projectOptions <- opts
    }

[<Test>]
let ``string array in binding`` () =
    async {
        let source =
            """
module M

let a (b: string[]) = ()
    """

        let ctx = getContext projectOptions source
        let! msgs = squareBracketArrayAnalyzer ctx
        Assert.IsNotEmpty msgs
        Assert.IsTrue(Assert.messageContains "Prefer postfix syntax for arrays." msgs[0])
    }

[<Test>]
let ``int array in val sig`` () =
    async {
        let source =
            """
module M

val a: b: int[] -> unit
    """

        let ctx = getContextForSignature projectOptions source
        let! msgs = squareBracketArrayAnalyzer ctx
        Assert.IsNotEmpty msgs
        Assert.IsTrue(Assert.messageContains "Prefer postfix syntax for arrays." msgs[0])
    }
