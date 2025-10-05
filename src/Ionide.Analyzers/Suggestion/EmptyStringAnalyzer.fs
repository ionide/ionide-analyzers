module Ionide.Analyzers.Suggestion.EmptyStringAnalyzer

open Ionide.Analyzers
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.TASTCollecting
open FSharp.Compiler.Symbols
open FSharp.Compiler.Text
open FSharp.Compiler.Syntax
open FSharp.Compiler.SyntaxTrivia

[<return: Struct>]
let (|EmptyStringConst|_|) (e: FSharpExpr) =
    let name = e.Type.ErasedType.TypeDefinition.TryGetFullName()

    match name, e with
    | Some("System.String"), FSharpExprPatterns.Const(o, _type) when not (isNull o) && (string o).Length = 0 ->
        ValueSome()
    | _ -> ValueNone

[<Literal>]
let ignoreComment = "IGNORE: IONIDE-005"

let analyze (sourceText: ISourceText) (input: ParsedInput) (typedTree: FSharpImplementationFileContents) =
    let ranges = ResizeArray<range>()
    let comments = InputOperations.getCodeComments input
    let hasIgnoreComment = Ignore.hasComment ignoreComment comments sourceText

    let walker =
        { new TypedTreeCollectorBase() with
            override _.WalkCall _ (mfv: FSharpMemberOrFunctionOrValue) _ _ (args: FSharpExpr list) (m: range) =
                match mfv.Assembly.SimpleName, mfv.FullName, args, hasIgnoreComment m with
                | "FSharp.Core", "Microsoft.FSharp.Core.Operators.(=)", [ _; EmptyStringConst ], None
                | "FSharp.Core", "Microsoft.FSharp.Core.Operators.(=)", [ EmptyStringConst; _ ], None -> ranges.Add m
                | _ -> ()

        }

    walkTast walker typedTree

    ranges
    |> Seq.map (fun r ->
        {
            Type = "EmptyString analyzer"
            Message =
                "Test for empty strings should use the String.Length property or the String.IsNullOrEmpty method."
            Code = "IONIDE-005"
            Severity = Severity.Warning
            Range = r
            Fixes = []
        }
    )
    |> Seq.toList

[<Literal>]
let name = "EmptyStringAnalyzer"

[<Literal>]
let shortDescription = "Verifies testing for an empty string is done efficiently."

[<Literal>]
let helpUri = "https://ionide.io/ionide-analyzers/suggestion/005.html"

[<EditorAnalyzer(name, shortDescription, helpUri)>]
let emptyStringEditorAnalyzer (ctx: EditorContext) =
    async { return ctx.TypedTree |> Option.map (analyze ctx.SourceText ctx.ParseFileResults.ParseTree) |> Option.defaultValue [] }

[<CliAnalyzer(name, shortDescription, helpUri)>]
let emptyStringCliAnalyzer (ctx: CliContext) =
    async { return ctx.TypedTree |> Option.map (analyze ctx.SourceText ctx.ParseFileResults.ParseTree) |> Option.defaultValue [] }
