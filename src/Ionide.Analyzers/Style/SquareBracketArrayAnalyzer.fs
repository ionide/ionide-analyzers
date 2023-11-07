module Ionide.Analyzers.Style.SquareBracketArrayAnalyzer

open FSharp.Compiler.Text
open FSharp.Compiler.Syntax
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting

[<CliAnalyzer("SquareBracketArrayAnalyzer",
              "Detect if type[] is used instead of type array",
              "https://ionide.io/ionide-analyzers/style/002.html")>]
let squareBracketArrayAnalyzer: Analyzer<CliContext> =
    fun (context: CliContext) ->
        async {
            let ts = ResizeArray<range>()

            let collector =
                { new SyntaxCollectorBase() with
                    override x.WalkType(t: SynType) =
                        match t with
                        | SynType.Array _ -> ts.Add t.Range
                        | _ -> ()
                }

            walkAst collector context.ParseFileResults.ParseTree

            return
                ts
                |> Seq.map (fun m ->
                    {
                        Type = "SquareBracketArrayAnalyzer"
                        Message = "Prefer postfix syntax for arrays."
                        Code = "IONIDE-002"
                        Severity = Info
                        Range = m
                        Fixes = []
                    }
                )
                |> Seq.toList
        }
