module Ionide.Analyzers.Performance.CombinePipedModuleFunctionsAnalyzer

open System.Collections.Generic
open FSharp.Compiler.Text
open FSharp.Compiler.Syntax
open FSharp.Compiler.SyntaxTrivia
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting
open Ionide.Analyzers.UntypedOperations

[<return: Struct>]
let (|PipeInfixApp|_|) synExpr =
    match synExpr with
    | SynExpr.App(funcExpr = OpPipeRight e1; argExpr = e2) -> ValueSome(e1, e2)
    | _ -> ValueNone

[<return: Struct>]
let (|PipeInfixApps|_|) expr =
    let rec visitLeft expr continuation =
        match expr with
        | PipeInfixApp(lhs, rhs) ->
            visitLeft
                lhs
                (fun (head, xs: Queue<SynExpr>) ->
                    xs.Enqueue(rhs)
                    continuation (head, xs)
                )
        | e -> continuation (e, Queue())

    let head, xs = visitLeft expr id
    ValueSome [ head; yield! xs ]

let moduleNames = set [ "List"; "Array"; "Seq"; "Set" ]
let functionNames = set [ "map"; "filter" ]

/// Assume structure List.map
[<return: Struct>]
let private (|ModuleFunction|_|) (e: SynExpr) =
    match e with
    | SynExpr.App(funcExpr = SynExpr.LongIdent(longDotId = SynLongIdent(id = [ moduleIdent; functionIdent ]))) ->
        if
            not (moduleNames.Contains moduleIdent.idText)
            || not (functionNames.Contains functionIdent.idText)
        then
            ValueNone
        else
            ValueSome(moduleIdent, functionIdent)
    | _ -> ValueNone

type MessageData =
    {
        ModuleName: string
        Function1: string
        Function2: string
        Range: Range
    }

let private analyze (parsedInput: ParsedInput) : Message list =
    let xs = HashSet<MessageData>()

    let collector =
        { new SyntaxCollectorBase() with
            override x.WalkExpr(path, synExpr) =
                match synExpr with
                | PipeInfixApps es ->
                    let rec visit es =
                        match es with
                        | [] -> ()
                        | ModuleFunction(m1, f1) :: ModuleFunction(m2, f2) :: rest when m1.idText = m2.idText ->
                            let m = Range.unionRanges m1.idRange m2.idRange

                            xs.Add
                                {
                                    ModuleName = m1.idText
                                    Function1 = f1.idText
                                    Function2 = f2.idText
                                    Range = m
                                }
                            |> ignore

                            visit rest
                        | _ :: rest -> visit rest

                    visit es
                | _ -> ()
        }

    walkAst collector parsedInput

    xs
    |> Seq.map (fun data ->
        let message =
            if data.Function1 = data.Function2 then
                $"%s{data.ModuleName}.%s{data.Function1} is being piped into %s{data.ModuleName}.%s{data.Function1}, these can be combined into a single %s{data.ModuleName}.%s{data.Function1}"
            else
                $"%s{data.ModuleName}.%s{data.Function1} |> %s{data.ModuleName}.%s{data.Function2} can be combined into %s{data.ModuleName}.choose"

        {
            Type = "combinePipedModuleFunctions"
            Message = message
            Code = "IONIDE-010"
            Severity = Severity.Hint
            Range = data.Range
            Fixes = []
        }
    )
    |> Seq.toList

[<Literal>]
let name = "CombinePipedModuleFunctionsAnalyzer"

[<Literal>]
let shortDescription = "Short description about CombinePipedModuleFunctionsAnalyzer"

[<Literal>]
let helpUri = "https://ionide.io/ionide-analyzers/performance/010.html"

[<CliAnalyzer(name, shortDescription, helpUri)>]
let combinePipedModuleFunctionsCliAnalyzer: Analyzer<CliContext> =
    fun (context: CliContext) -> async { return analyze context.ParseFileResults.ParseTree }

[<EditorAnalyzer(name, shortDescription, helpUri)>]
let combinePipedModuleFunctionsEditorAnalyzer: Analyzer<EditorContext> =
    fun (context: EditorContext) -> async { return analyze context.ParseFileResults.ParseTree }
