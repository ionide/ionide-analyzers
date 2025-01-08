module Ionide.Analyzers.Tests.Suggestion.CopyAndUpdateRecordChangesAllFieldsAnalyzerTests

open NUnit.Framework
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text.Range
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.Testing
open Ionide.Analyzers.Suggestion.CopyAndUpdateRecordChangesAllFieldsAnalyzer

let mutable projectOptions: FSharpProjectOptions = FSharpProjectOptions.zero

[<SetUp>]
let Setup () =
    task {
        let! opts = mkOptionsFromProject "net8.0" []

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
        let! msgs = copyAndUpdateRecordChangesAllFieldsCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains "All record fields of record are being updated" msg, Is.True)
        Assert.That(msg.Fixes, Is.Not.Empty)
        let fix = msg.Fixes[0]

        let expectedFix =
            {
                FromText = ""
                ToText = ""
                FromRange =
                    mkRange "A.fs" (FSharp.Compiler.Text.Position.mkPos 6 16) (FSharp.Compiler.Text.Position.mkPos 6 23)
            }

        Assert.That((fix = expectedFix), Is.True)
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
        let! msgs = copyAndUpdateRecordChangesAllFieldsCliAnalyzer ctx
        Assert.That(msgs, Is.Not.Empty)
        let msg = msgs[0]
        Assert.That(Assert.messageContains "All record fields of record are being updated" msg, Is.True)
        Assert.That(msg.Fixes, Is.Not.Empty)
        let fix = msg.Fixes[0]

        let expectedFix =
            {
                FromText = ""
                ToText = ""
                FromRange =
                    mkRange "A.fs" (FSharp.Compiler.Text.Position.mkPos 6 16) (FSharp.Compiler.Text.Position.mkPos 6 23)
            }

        Assert.That((fix = expectedFix), Is.True)
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
        let! msgs = copyAndUpdateRecordChangesAllFieldsCliAnalyzer ctx
        Assert.That(msgs, Is.Empty)
    }
