module Ionide.Analyzers.Suggestion.IgnoreFunctionAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.TASTCollecting
open FSharp.Compiler.Symbols
open FSharp.Compiler.Text

[<CliAnalyzer("IgnoreFunctionAnalyzer",
              "A function is being ignored. Did you mean to execute this?",
              "https://ionide.io/ionide-analyzers/suggestion/003.html")>]
let ignoreFunctionAnalyzer (ctx: CliContext) =
    async {
        let messages = ResizeArray<Message>()

        let tastCollector =
            { new TypedTreeCollectorBase() with
                override x.WalkCall _ (mfv: FSharpMemberOrFunctionOrValue) _ _ (args: FSharpExpr list) (m: range) =
                    if
                        mfv.FullName = "Microsoft.FSharp.Core.Operators.ignore"
                        && args.Length = 1
                        && args.[0].Type.IsFunctionType
                    then
                        messages.Add
                            {
                                Type = "IgnoreFunctionAnalyzer"
                                Message = "A function is being ignored. Did you mean to execute this?"
                                Code = "003"
                                Severity = Severity.Warning
                                Range = m
                                Fixes = []
                            }
            }

        match ctx.TypedTree with
        | None -> return []
        | Some typedTree ->
            walkTast tastCollector typedTree
            return Seq.toList messages
    }
