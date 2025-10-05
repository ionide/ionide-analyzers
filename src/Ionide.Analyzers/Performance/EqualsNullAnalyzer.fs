module Ionide.Analyzers.Performance.EqualsNullAnalyzer

open Ionide.Analyzers
open System.Collections.Generic
open FSharp.Compiler.Text
open FSharp.Compiler.Syntax
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting
open Ionide.Analyzers.UntypedOperations
open Ionide.Analyzers.TypedOperations
open FSharp.Compiler.CodeAnalysis

[<Literal>]
let ignoreComment = "IGNORE: IONIDE-011"

[<Literal>]
let equalsMessage = "`a = null` is suboptimal, use `isNull a` instead."

[<Literal>]
let inEqualsMessage = "a <> null is suboptimal, use `not (isNull a)` instead."

[<Struct>]
type private EqualsNullOperation =
    | EqualsNullOperation of negate: bool * argExpr: range * operator: Ident * addParens: bool * range: range

// TODO: replace with SynExpr.shouldBeParenthesizedInContext once it is shipped.
let private addParens e =
    match e with
    | SynExpr.Const _
    | SynExpr.Ident _ -> false
    | _ -> true

let private analyze
    (sourceText: ISourceText)
    (parsedInput: ParsedInput)
    (checkResults: FSharpCheckFileResults)
    : Message list
    =
    let xs = HashSet<EqualsNullOperation>()

    let comments =
        match parsedInput with
        | ParsedInput.ImplFile parsedFileInput -> parsedFileInput.Trivia.CodeComments
        | _ -> []

    let hasIgnoreComment = Ignore.hasComment ignoreComment comments sourceText >> Option.isSome

    let collector =
        { new SyntaxCollectorBase() with
            override x.WalkExpr(path, synExpr) =
                match synExpr with
                | SynExpr.App(ExprAtomicFlag.NonAtomic, false, OpEquality(opIdent, argExpr), SynExpr.Null _, m)
                | SynExpr.App(ExprAtomicFlag.NonAtomic, false, OpEquality(opIdent, SynExpr.Null _), argExpr, m) when not <| hasIgnoreComment m ->
                    xs.Add(EqualsNullOperation(false, argExpr.Range, opIdent, addParens argExpr, m))
                    |> ignore

                | SynExpr.App(ExprAtomicFlag.NonAtomic, false, OpInequality(opIdent, argExpr), SynExpr.Null _, m)
                | SynExpr.App(ExprAtomicFlag.NonAtomic, false, OpInequality(opIdent, SynExpr.Null _), argExpr, m) when not <| hasIgnoreComment m ->
                    xs.Add(EqualsNullOperation(true, argExpr.Range, opIdent, addParens argExpr, m))
                    |> ignore
                | _ -> ()
        }

    walkAst collector parsedInput

    xs
    |> Seq.choose (fun (EqualsNullOperation(negate, mArgExpr, operator, addParens, m)) ->
        tryFSharpMemberOrFunctionOrValueFromIdent sourceText checkResults operator
        |> Option.bind (fun mfv ->
            if not mfv.IsFunction then
                None
            else

            let fixes =
                let argText = sourceText.GetSubTextFromRange mArgExpr
                let lpr, rpr = if addParens then "(", ")" else "", ""

                let text =
                    if negate then
                        $"not (isNull %s{lpr}%s{argText}%s{rpr})"
                    else
                        $"isNull %s{lpr}%s{argText}%s{rpr}"

                [
                    {
                        FromText = ""
                        FromRange = m
                        ToText = text
                    }
                ]

            Some
                {
                    Type = "equalsNull"
                    Message = if negate then inEqualsMessage else equalsMessage
                    Code = "IONIDE-011"
                    Severity = Severity.Warning
                    Range = m
                    Fixes = fixes
                }
        )
    )
    |> Seq.toList

[<Literal>]
let name = "EqualsNullAnalyzer"

[<Literal>]
let shortDescription = "Short description about EqualsNullAnalyzer"

[<Literal>]
let helpUri = "https://ionide.io/ionide-analyzers/performance/011.html"

[<CliAnalyzer(name, shortDescription, helpUri)>]
let equalsNullCliAnalyzer: Analyzer<CliContext> =
    fun (context: CliContext) ->
        async { return analyze context.SourceText context.ParseFileResults.ParseTree context.CheckFileResults }

[<EditorAnalyzer(name, shortDescription, helpUri)>]
let equalsNullEditorAnalyzer: Analyzer<EditorContext> =
    fun (context: EditorContext) ->
        async {
            match context.CheckFileResults with
            | None -> return []
            | Some checkFileResults ->
                return analyze context.SourceText context.ParseFileResults.ParseTree checkFileResults
        }
