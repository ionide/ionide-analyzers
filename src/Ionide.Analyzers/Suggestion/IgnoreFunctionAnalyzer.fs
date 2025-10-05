module Ionide.Analyzers.Suggestion.IgnoreFunctionAnalyzer

open Ionide.Analyzers
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.TASTCollecting
open FSharp.Compiler.Symbols
open FSharp.Compiler.Text
open FSharp.Compiler.Syntax

[<Literal>]
let ignoreComment = "IGNORE: IONIDE-003"

let analyzer (sourceText: ISourceText) (input: ParsedInput) (typedTree: FSharpImplementationFileContents option) =
    let messages = ResizeArray<Message>()
    let comments = InputOperations.getCodeComments input
    let hasIgnoreComment = Ignore.hasComment ignoreComment comments sourceText

    let tastCollector =
        { new TypedTreeCollectorBase() with
            override x.WalkCall _ (mfv: FSharpMemberOrFunctionOrValue) _ _ (args: FSharpExpr list) (m: range) =
                if
                    mfv.FullName = "Microsoft.FSharp.Core.Operators.ignore"
                    && args.Length = 1
                    && args.[0].Type.IsFunctionType
                    && hasIgnoreComment m |> Option.isNone
                then
                    messages.Add
                        {
                            Type = "IgnoreFunctionAnalyzer"
                            Message = "A function is being ignored. Did you mean to execute this?"
                            Code = "IONIDE-003"
                            Severity = Severity.Warning
                            Range = m
                            Fixes = []
                        }
        }

    match typedTree with
    | None -> []
    | Some typedTree ->
        walkTast tastCollector typedTree
        Seq.toList messages

[<Literal>]
let name = "IgnoreFunctionAnalyzer"

[<Literal>]
let shortDescription = "A function is being ignored. Did you mean to execute this?"

[<Literal>]
let helpUri = "https://ionide.io/ionide-analyzers/suggestion/003.html"

[<CliAnalyzer(name, shortDescription, helpUri)>]
let ignoreFunctionCliAnalyzer (ctx: CliContext) = async { return analyzer ctx.SourceText ctx.ParseFileResults.ParseTree ctx.TypedTree }

[<EditorAnalyzer(name, shortDescription, helpUri)>]
let ignoreFunctionEditorAnalyzer (ctx: EditorContext) = async { return analyzer ctx.SourceText ctx.ParseFileResults.ParseTree ctx.TypedTree }
