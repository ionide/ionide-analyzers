module Ionide.Analyzers.Style.MixedFlowDirectionAnalyzer

open FSharp.Compiler.Text
open FSharp.Compiler.Syntax
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting
open Ionide.Analyzers.UntypedOperations

[<Literal>]
let message =
    "This expression mixes forward flow operators (|>, ||>, |||>, >>) with backward flow operators (<|, <||, <|||, <<). Use a single direction, or bind an intermediate value with let."

type private FlowDirection =
    | Forward
    | Backward

let private forwardFlowOperators = set [ "|>"; "||>"; "|||>"; ">>" ]
let private backwardFlowOperators = set [ "<|"; "<||"; "<|||"; "<<" ]

[<return: Struct>]
let private (|FlowNotation|_|) (originalNotation: string) =
    if forwardFlowOperators.Contains originalNotation then
        ValueSome Forward
    elif backwardFlowOperators.Contains originalNotation then
        ValueSome Backward
    else
        ValueNone

/// A single link of a flow-operator chain, such as `lhs |> rhs` or `lhs << rhs`.
/// Yields the direction, the operator identifier and the left-hand side.
[<return: Struct>]
let private (|FlowOperatorApp|_|) (e: SynExpr) =
    match e with
    | SynExpr.App(funcExpr = AnyInfixOperator(FlowNotation direction, operatorIdent, lhs)) ->
        ValueSome(direction, operatorIdent, lhs)
    | _ -> ValueNone

/// Flow operators are left-associative, so a chain of them is a left spine.
/// Collect every operator on that spine, in source order.
let private collectChainOperators (e: SynExpr) =
    let rec loop e acc =
        match e with
        | FlowOperatorApp(direction, operatorIdent, lhs) -> loop lhs ((direction, operatorIdent.idRange) :: acc)
        | _ -> acc

    loop e []

/// True when the nearest enclosing expression is not another link of the same chain.
/// Inner links are skipped so a mixed chain reports once, at its outermost node.
let private isChainRoot (path: SyntaxNode list) =
    match path with
    | SyntaxNode.SynExpr(AnyInfixOperator(FlowNotation _, _, _)) :: _ -> false
    | _ -> true

let private analyze (parsedInput: ParsedInput) : Message list =
    let ranges = ResizeArray<Range>()

    let collector =
        { new SyntaxCollectorBase() with
            override _.WalkExpr(path, synExpr) =
                match synExpr with
                | FlowOperatorApp _ when isChainRoot path ->
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
            Type = "mixedFlowDirection"
            Message = message
            Code = "IONIDE-013"
            Severity = Severity.Info
            Range = range
            Fixes = []
        }
    )
    |> Seq.toList

[<Literal>]
let name = "MixedFlowDirectionAnalyzer"

[<Literal>]
let shortDescription =
    "Detect expressions that mix forward and backward pipe or composition operators."

[<Literal>]
let helpUri = "https://ionide.io/ionide-analyzers/style/013.html"

[<CliAnalyzer(name, shortDescription, helpUri)>]
let mixedFlowDirectionCliAnalyzer: Analyzer<CliContext> =
    fun (context: CliContext) -> async { return analyze context.ParseFileResults.ParseTree }

[<EditorAnalyzer(name, shortDescription, helpUri)>]
let mixedFlowDirectionEditorAnalyzer: Analyzer<EditorContext> =
    fun (context: EditorContext) -> async { return analyze context.ParseFileResults.ParseTree }
