module Ionide.Analyzers.Suggestion.HandleOptionGracefullyAnalyzer

open System
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.TASTCollecting
open FSharp.Compiler.Symbols
open FSharp.Compiler.Text

let analyze (typedTree: FSharpImplementationFileContents option) =
    let messages = ResizeArray<Message>()

    let walker =
        { new TypedTreeCollectorBase() with
            override x.WalkCall _ (mfv: FSharpMemberOrFunctionOrValue) _ _ (args: FSharpExpr list) (m: range) =
                let fullyQualifiedCall =
                    let fullName =
                        mfv.DeclaringEntity
                        |> Option.map (fun e -> e.TryGetFullName())
                        |> Option.flatten
                        |> Option.defaultValue ""

                    String.Join(".", fullName, mfv.DisplayName)

                if
                    (mfv.FullName = "Microsoft.FSharp.Core.Option.get"
                     || mfv.FullName = "Microsoft.FSharp.Core.ValueOption.get"
                     || fullyQualifiedCall = "Microsoft.FSharp.Core.FSharpOption`1.Value"
                     || fullyQualifiedCall = "Microsoft.FSharp.Core.FSharpValueOption`1.Value")
                    && args.Length = 1
                then
                    messages.Add
                        {
                            Type = "HandleOptionGracefully"
                            Message = "Replace unsafe option unwrapping with graceful handling of each case."
                            Code = "IONIDE-006"
                            Severity = Severity.Warning
                            Range = m
                            Fixes = []
                        }
        }

    match typedTree with
    | None -> []
    | Some typedTree ->
        walkTast walker typedTree
        Seq.toList messages

[<Literal>]
let name = "HandleOptionGracefullyAnalyzer"

[<Literal>]
let shortDescription =
    "Replace unsafe option unwrapping with graceful handling of each case."

[<Literal>]
let helpUri = "https://ionide.io/ionide-analyzers/suggestion/006.html"

[<CliAnalyzer(name, shortDescription, helpUri)>]
let optionGetCliAnalyzer (ctx: CliContext) = async { return analyze ctx.TypedTree }

[<EditorAnalyzer(name, shortDescription, helpUri)>]
let optionGetEditorAnalyzer (ctx: EditorContext) = async { return analyze ctx.TypedTree }
