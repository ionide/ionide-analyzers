module Ionide.Analyzers.Tests.Performance.ListEqualsEmptyListAnalyzerTests

open NUnit.Framework
open FSharp.Compiler.CodeAnalysis
open FSharp.Analyzers.SDK.Testing
open Ionide.Analyzers.Performance.ListEqualsEmptyListAnalyzer

let mutable projectOptions: FSharpProjectOptions = FSharpProjectOptions.zero

[<SetUp>]
let Setup () =
    task {
        let! opts = mkOptionsFromProject "net7.0" []
        projectOptions <- opts
    }

[<Test>]
let ``value equals empty list`` () =
    async {
        let source =
            """module Lib
let a = [ 1; 2; 3 ]
let b = a = []
    """

        let ctx = getContext projectOptions source
        let! msgs = listEqualsEmptyListCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains message msg, Is.True)
    }

[<Test>]
let ``empty list equals value`` () =
    async {
        let source =
            """module Lib
let a = [ 1; 2; 3 ]
let b = [] = a
    """

        let ctx = getContext projectOptions source
        let! msgs = listEqualsEmptyListCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains message msg, Is.True)
    }

[<Test>]
let ``named ctor parameter does not trigger`` () =
    async {
        let source =
            """module Lib

type X(y:int list) = class end

let x = X(y = [])
    """

        let ctx = getContext projectOptions source
        let! msgs = listEqualsEmptyListCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }
