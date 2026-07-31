module Ionide.Analyzers.Style.MixedPipeDirectionAnalyzer

open FSharp.Compiler.Text
open FSharp.Compiler.Syntax
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting
open Ionide.Analyzers.UntypedOperations

[<Literal>]
let message =
    "This pipeline mixes forward pipes (|>, ||>, |||>) with backward pipes (<|, <||, <|||). Use a single direction, or bind an intermediate value with let."

type private PipeDirection =
    | Forward
    | Backward

let private forwardPipes = set [ "|>"; "||>"; "|||>" ]
let private backwardPipes = set [ "<|"; "<||"; "<|||" ]

[<return: Struct>]
let private (|PipeNotation|_|) (originalNotation: string) =
    if forwardPipes.Contains originalNotation then
        ValueSome Forward
    elif backwardPipes.Contains originalNotation then
        ValueSome Backward
    else
        ValueNone

/// A single link of a pipe chain, such as `lhs |> rhs` or `lhs <|| rhs`.
/// Yields the direction, the operator identifier and the left-hand side.
[<return: Struct>]
let private (|PipeApp|_|) (e: SynExpr) =
    match e with
    | SynExpr.App(funcExpr = AnyInfixOperator(PipeNotation direction, operatorIdent, lhs)) ->
        ValueSome(direction, operatorIdent, lhs)
    | _ -> ValueNone

/// The pipe operators are left-associative, so a chain of them is a left spine.
/// Collect every operator on that spine, in source order.
let private collectChainOperators (e: SynExpr) =
    let rec loop e acc =
        match e with
        | PipeApp(direction, operatorIdent, lhs) -> loop lhs ((direction, operatorIdent.idRange) :: acc)
        | _ -> acc

    loop e []

/// True when the nearest enclosing expression is not another link of the same chain.
/// Inner links are skipped so a mixed chain reports once, at its outermost node.
let private isChainRoot (path: SyntaxNode list) =
    match path with
    | SyntaxNode.SynExpr(AnyInfixOperator(PipeNotation _, _, _)) :: _ -> false
    | _ -> true

let private analyze (parsedInput: ParsedInput) : Message list =
    let ranges = ResizeArray<Range>()

    let collector =
        { new SyntaxCollectorBase() with
            override _.WalkExpr(path, synExpr) =
                match synExpr with
                | PipeApp _ when isChainRoot path ->
                    let operators = collectChainOperators synExpr
                    let hasForward = operators |> List.exists (fun (d, _) -> d = Forward)
                    let hasBackward = operators |> List.exists (fun (d, _) -> d = Backward)

                    if hasForward && hasBackward then
                        let firstOperator = snd (List.head operators)
                        let lastOperator = snd (List.last operators)
                        ranges.Add(Range.unionRanges firstOperator lastOperator)
                | _ -> ()
        }

    walkAst collector parsedInput

    ranges
    |> Seq.map (fun range ->
        {
            Type = "mixedPipeDirection"
            Message = message
            Code = "IONIDE-013"
            Severity = Severity.Info
            Range = range
            Fixes = []
        }
    )
    |> Seq.toList

[<Literal>]
let name = "MixedPipeDirectionAnalyzer"

[<Literal>]
let shortDescription =
    "Detect expressions that mix the forward and backward pipe operators."

[<Literal>]
let helpUri = "https://ionide.io/ionide-analyzers/style/013.html"

[<CliAnalyzer(name, shortDescription, helpUri)>]
let mixedPipeDirectionCliAnalyzer: Analyzer<CliContext> =
    fun (context: CliContext) -> async { return analyze context.ParseFileResults.ParseTree }

[<EditorAnalyzer(name, shortDescription, helpUri)>]
let mixedPipeDirectionEditorAnalyzer: Analyzer<EditorContext> =
    fun (context: EditorContext) -> async { return analyze context.ParseFileResults.ParseTree }
