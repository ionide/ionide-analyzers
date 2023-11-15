module Ionide.Analyzers.Suggestion.HandleOptionGracefullyAnalyzer

open System
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.TASTCollecting
open FSharp.Compiler.Symbols
open FSharp.Compiler.Text

[<CliAnalyzer("HandleOptionGracefullyAnalyzer",
              "Replace unsafe option unwrapping with graceful handling of each case.",
              "https://ionide.io/ionide-analyzers/suggestion/006.html")>]
let optionGetAnalyzer (ctx: CliContext) =
    async {
        let messages = ResizeArray<Message>()

        let walker =
            { new TypedTreeCollectorBase() with
                override x.WalkCall _ (mfv: FSharpMemberOrFunctionOrValue) _ _ (args: FSharpExpr list) (m: range) =
                    let fullyQualifiedCall =
                        String.Join(".", mfv.DeclaringEntity.Value.FullName, mfv.DisplayName)

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
                                Severity = Severity.Hint
                                Range = m
                                Fixes = []
                            }
            }

        match ctx.TypedTree with
        | None -> return []
        | Some typedTree ->
            walkTast walker typedTree
            return Seq.toList messages
    }
