﻿module Ionide.Analyzers.Suggestion.CopyAndUpdateRecordChangesAllFieldsAnalyzer

open Ionide.Analyzers
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting
open FSharp.Analyzers.SDK.TASTCollecting
open FSharp.Compiler.Symbols
open FSharp.Compiler.Text
open FSharp.Compiler.Syntax

type UpdateRecord = SynExprRecordField list * range * range

let ignoreComment = "IGNORE: IONIDE-001"

let analyze sourceText parseTree (typedTree: FSharpImplementationFileContents option) =
    let comments = InputOperations.getCodeComments parseTree

    let hasIgnoreComment =
        Ignore.hasComment ignoreComment comments sourceText >> Option.isSome

    let untypedRecordUpdates =
        let xs = ResizeArray<UpdateRecord>()

        let collector =
            { new SyntaxCollectorBase() with
                override x.WalkExpr(_, e: SynExpr) =
                    match e with
                    | SynExpr.Record(copyInfo = Some(synExpr, (withRange, _)); recordFields = fields; range = m) when
                        not <| hasIgnoreComment m
                        ->
                        let fixRange = Range.unionRanges synExpr.Range (Range.shiftEnd 0 1 withRange)
                        xs.Add(fields, e.Range, fixRange)
                    | _ -> ()
            }

        walkAst collector parseTree
        Seq.toList xs

    let messages = ResizeArray<Message> untypedRecordUpdates.Length

    let tastCollector =
        { new TypedTreeCollectorBase() with
            override x.WalkNewRecord (recordType: FSharpType) _ (mRecord: range) =
                let matchingUnTypedNode =
                    untypedRecordUpdates
                    |> List.tryFind (fun (_, mExpr, _) -> Range.equals mExpr mRecord)

                match matchingUnTypedNode with
                | None -> ()
                | Some(fields, mExpr, fixRange) ->

                if not recordType.TypeDefinition.IsFSharpRecord then
                    ()
                else if recordType.TypeDefinition.FSharpFields.Count = fields.Length then
                    messages.Add
                        {
                            Type = "CopyAndUpdateRecordChangesAllFieldsAnalyzer analyzer"
                            Message =
                                "All record fields of record are being updated. Consider creating a new instance instead."
                            Code = "IONIDE-001"
                            Severity = Severity.Hint
                            Range = mExpr
                            Fixes =
                                [
                                    {
                                        FromRange = fixRange
                                        FromText = ""
                                        ToText = ""
                                    }
                                ]
                        }
        }

    match typedTree with
    | None -> ()
    | Some typedTree -> walkTast tastCollector typedTree

    Seq.toList messages

[<Literal>]
let name = "CopyAndUpdateRecordChangesAllFieldsAnalyzer"

[<Literal>]
let shortDescription =
    "Detect if all fields in a record update expression are updated."

[<Literal>]
let helpUri = "https://ionide.io/ionide-analyzers/suggestion/001.html"

[<CliAnalyzer(name, shortDescription, helpUri)>]
let copyAndUpdateRecordChangesAllFieldsCliAnalyzer: Analyzer<CliContext> =
    fun (context: CliContext) ->
        async { return analyze context.SourceText context.ParseFileResults.ParseTree context.TypedTree }

[<EditorAnalyzer(name, shortDescription, helpUri)>]
let copyAndUpdateRecordChangesAllFieldsEditorAnalyzer: Analyzer<EditorContext> =
    fun (context: EditorContext) ->
        async { return analyze context.SourceText context.ParseFileResults.ParseTree context.TypedTree }
