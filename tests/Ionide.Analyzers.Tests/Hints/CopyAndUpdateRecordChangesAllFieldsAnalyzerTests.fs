module Ionide.Analyzers.Tests.Hints.CopyAndUpdateRecordChangesAllFieldsAnalyzerTests

open NUnit.Framework
open FSharp.Compiler.CodeAnalysis
open FSharp.Analyzers.SDK.Testing
open Ionide.Analyzers.Hints.CopyAndUpdateRecordChangesAllFieldsAnalyzer

let mutable projectOptions: FSharpProjectOptions = FSharpProjectOptions.zero

[<SetUp>]
let Setup () =
    task {
        let! opts = mkOptionsFromProject "net7.0" []

        projectOptions <- opts
    }

[<Test>]
let ``single record field`` () =
    async {
        let source =
            """
module M

type R = { A: int }
let a = { A = 1 }
let updated = { a with A = 2 }
    """

        let ctx = getContext projectOptions source
        let! msgs = copyAndUpdateRecordChangesAllFieldsAnalyzer ctx
        Assert.IsNotEmpty msgs
        Assert.IsTrue(Assert.messageContains "All record fields of record are being updated" msgs[0])
    }

[<Test>]
let ``multiple record field`` () =
    async {
        let source =
            """
module M

type R = { A: int; B:int; C:int }
let a = { A = 1; B = 2; C = 3 }
let updated = { a with A = 2; B = 4; C = 5 }
    """

        let ctx = getContext projectOptions source
        let! msgs = copyAndUpdateRecordChangesAllFieldsAnalyzer ctx
        Assert.IsNotEmpty msgs
        Assert.IsTrue(Assert.messageContains "All record fields of record are being updated" msgs[0])
    }

[<Test>]
let ``multiple record field, neg`` () =
    async {
        let source =
            """
module M

type R = { A: int; B:int; C:int }
let a = { A = 1; B = 2; C = 3 }
let updated = { a with A = 2; B = 4 }
    """

        let ctx = getContext projectOptions source
        let! msgs = copyAndUpdateRecordChangesAllFieldsAnalyzer ctx
        Assert.IsEmpty msgs
    }
