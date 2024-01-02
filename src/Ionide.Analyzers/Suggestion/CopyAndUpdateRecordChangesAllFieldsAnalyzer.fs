module Ionide.Analyzers.Suggestion.CopyAndUpdateRecordChangesAllFieldsAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting
open FSharp.Analyzers.SDK.TASTCollecting
open FSharp.Compiler.Symbols
open FSharp.Compiler.Text
open FSharp.Compiler.Syntax

type UpdateRecord = SynExprRecordField list * range

let analyze parseTree (typedTree: FSharpImplementationFileContents option) =
    let untypedRecordUpdates =
        let xs = ResizeArray<UpdateRecord>()

        let collector =
            { new SyntaxCollectorBase() with
                override x.WalkExpr(_, e: SynExpr) =
                    match e with
                    | SynExpr.Record(copyInfo = Some _; recordFields = fields) -> xs.Add(fields, e.Range)
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
                    |> List.tryFind (fun (_, mExpr) -> Range.equals mExpr mRecord)

                match matchingUnTypedNode with
                | None -> ()
                | Some(fields, mExpr) ->

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
                            Fixes = []
                        }
        }

    match typedTree with
    | None -> ()
    | Some typedTree -> walkTast tastCollector typedTree

    Seq.toList messages

[<CliAnalyzer("CopyAndUpdateRecordChangesAllFieldsAnalyzer",
              "Detect if all fields in a record update expression are updated.",
              "https://ionide.io/ionide-analyzers/suggestion/001.html")>]
let copyAndUpdateRecordChangesAllFieldsCliAnalyzer: Analyzer<CliContext> =
    fun (context: CliContext) -> async { return analyze context.ParseFileResults.ParseTree context.TypedTree }

[<EditorAnalyzer("CopyAndUpdateRecordChangesAllFieldsAnalyzer",
                 "Detect if all fields in a record update expression are updated.",
                 "https://ionide.io/ionide-analyzers/suggestion/001.html")>]
let copyAndUpdateRecordChangesAllFieldsEditorAnalyzer: Analyzer<EditorContext> =
    fun (context: EditorContext) -> async { return analyze context.ParseFileResults.ParseTree context.TypedTree }
