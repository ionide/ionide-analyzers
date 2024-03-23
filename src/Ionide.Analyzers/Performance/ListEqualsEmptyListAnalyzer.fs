module Ionide.Analyzers.Performance.ListEqualsEmptyListAnalyzer

open System.Collections.Generic
open FSharp.Compiler.Text
open FSharp.Compiler.Syntax
open FSharp.Compiler.SyntaxTrivia
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting
open Ionide.Analyzers.UntypedOperations

[<Literal>]
let message = "list = [] is suboptimal, use List.isEmpty"

[<Struct>]
type private EqualsOperation = | EqualsOperation of argExpr: range * range: range

[<return: Struct>]
let private (|EmptyList|_|) =
    function
    | SynExpr.ArrayOrList(false, [], _) -> ValueSome()
    | _ -> ValueNone

let private analyze (sourceText: ISourceText) (parsedInput: ParsedInput) : Message list =
    let xs = HashSet<EqualsOperation>()

    let collector =
        { new SyntaxCollectorBase() with
            override x.WalkExpr(path, synExpr) =
                match synExpr with
                | SynExpr.App(ExprAtomicFlag.NonAtomic, false, OpEquality argExpr, EmptyList, m)
                | SynExpr.App(ExprAtomicFlag.NonAtomic, false, OpEquality EmptyList, argExpr, m) ->
                    xs.Add(EqualsOperation(argExpr.Range, m)) |> ignore
                | _ -> ()
        }

    walkAst collector parsedInput

    xs
    |> Seq.map (fun (EqualsOperation(mArg, m)) ->
        let fixes =
            [
                {
                    FromText = ""
                    FromRange = m
                    ToText = $"List.isEmpty %s{sourceText.GetSubTextFromRange mArg}"
                }
            ]

        {
            Type = "listEqualsEmptyList"
            Message = message
            Code = "IONIDE-008"
            Severity = Severity.Hint
            Range = m
            Fixes = fixes
        }
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
    fun (context: CliContext) -> async { return analyze context.SourceText context.ParseFileResults.ParseTree }

[<EditorAnalyzer(name, shortDescription, helpUri)>]
let listEqualsEmptyListEditorAnalyzer: Analyzer<EditorContext> =
    fun (context: EditorContext) -> async { return analyze context.SourceText context.ParseFileResults.ParseTree }
