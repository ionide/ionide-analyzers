module Ionide.Analyzers.Suggestion.ReplaceOptionGetWithGracefulHandlingAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.TASTCollecting
open FSharp.Compiler.Symbols
open FSharp.Compiler.Text

[<CliAnalyzer("ReplaceOptionGetWithGracefulHandlingAnalyzer",
              "Replace Option.get with graceful handling of each case.",
              "https://ionide.io/ionide-analyzers/suggestion/006.html")>]
let optionGetAnalyzer (ctx: CliContext) =
    async {
        let messages = ResizeArray<Message>()

        let walker =
            { new TypedTreeCollectorBase() with
                override x.WalkCall _ (mfv: FSharpMemberOrFunctionOrValue) _ _ (args: FSharpExpr list) (m: range) =
                    if mfv.FullName = "Microsoft.FSharp.Core.Option.get" && args.Length = 1 then
                        messages.Add
                            {
                                Type = "ReplaceOptionGetWithGracefulHandlingAnalyzer"
                                Message = "Replace Option.get with graceful handling of each case."
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
