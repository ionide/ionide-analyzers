module Ionide.Analyzers.Performance.ListEqualsEmptyListAnalyzer

open Ionide.Analyzers
open System.Collections.Generic
open FSharp.Compiler.Text
open FSharp.Compiler.Syntax
open FSharp.Compiler.CodeAnalysis
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting
open Ionide.Analyzers.UntypedOperations
open Ionide.Analyzers.TypedOperations

[<Literal>]
let ignoreComment = "IGNORE: IONIDE-008"

[<Literal>]
let message = "list = [] is suboptimal, use List.isEmpty"

[<Struct>]
type private EqualsOperation = | EqualsOperation of opEquals: Ident * argExpr: range * range: range

[<return: Struct>]
let private (|EmptyList|_|) =
    function
    | SynExpr.ArrayOrList(false, [], _) -> ValueSome()
    | _ -> ValueNone

let private analyze
    (sourceText: ISourceText)
    (parsedInput: ParsedInput)
    (checkResults: FSharpCheckFileResults)
    : Message list
    =
    let xs = HashSet<EqualsOperation>()

    let comments =
        match parsedInput with
        | ParsedInput.ImplFile parsedFileInput -> parsedFileInput.Trivia.CodeComments
        | _ -> []

    let hasIgnoreComment = Ignore.hasComment ignoreComment comments sourceText >> Option.isSome

    let collector =
        { new SyntaxCollectorBase() with
            override x.WalkExpr(path, synExpr) =
                match synExpr with
                | SynExpr.App(ExprAtomicFlag.NonAtomic, false, OpEquality(operatorIdent, argExpr), EmptyList, m)
                | SynExpr.App(ExprAtomicFlag.NonAtomic, false, OpEquality(operatorIdent, EmptyList), argExpr, m) when not <| hasIgnoreComment m ->
                    xs.Add(EqualsOperation(operatorIdent, argExpr.Range, m)) |> ignore
                | _ -> ()
        }

    walkAst collector parsedInput

    xs
    |> Seq.choose (fun (EqualsOperation(operatorIdent, mArg, m)) ->
        tryFSharpMemberOrFunctionOrValueFromIdent sourceText checkResults operatorIdent
        |> Option.bind (fun mfv ->
            if not mfv.IsFunction then
                None
            else

            let fixes =
                [
                    {
                        FromText = ""
                        FromRange = m
                        ToText = $"List.isEmpty %s{sourceText.GetSubTextFromRange mArg}"
                    }
                ]

            Some
                {
                    Type = "listEqualsEmptyList"
                    Message = message
                    Code = "IONIDE-008"
                    Severity = Severity.Hint
                    Range = m
                    Fixes = fixes
                }
        )
    )
    |> Seq.toList

[<Literal>]
let name = "ListEqualsEmptyListAnalyzer"

[<Literal>]
let shortDescription = "Short description about ListEqualsEmptyListAnalyzer"

[<Literal>]
let helpUri = "https://ionide.io/ionide-analyzers/performance/008.html"

[<CliAnalyzer(name, shortDescription, helpUri)>]
let listEqualsEmptyListCliAnalyzer: Analyzer<CliContext> =
    fun (context: CliContext) ->
        async { return analyze context.SourceText context.ParseFileResults.ParseTree context.CheckFileResults }

[<EditorAnalyzer(name, shortDescription, helpUri)>]
let listEqualsEmptyListEditorAnalyzer: Analyzer<EditorContext> =
    fun (context: EditorContext) ->
        async {
            match context.CheckFileResults with
            | None -> return []
            | Some checkResults -> return analyze context.SourceText context.ParseFileResults.ParseTree checkResults
        }
