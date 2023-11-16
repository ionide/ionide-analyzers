module Ionide.Analyzers.Style.PostfixGenericsAnalyzer

open FSharp.Compiler.Text
open FSharp.Compiler.Syntax
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting

[<CliAnalyzer("PostfixGenericsAnalyzer",
              "Detect if generic type should be in the postfix position.",
              "https://ionide.io/ionide-analyzers/style/002.html")>]
let postfixGenericsAnalyzer: Analyzer<CliContext> =
    fun (context: CliContext) ->
        async {
            let ts = ResizeArray<string * range>()

            let collector =
                { new SyntaxCollectorBase() with
                    override x.WalkType(t: SynType) =
                        match t with
                        | SynType.Array _ -> ts.Add("Prefer postfix syntax for arrays.", t.Range)
                        | SynType.App(SynType.LongIdent synLongIdent, _, _, _, _, false, _) ->
                            match synLongIdent.LongIdent with
                            | [ ident ] ->
                                match ident.idText with
                                | "array" -> ts.Add("Prefer postfix syntax for arrays.", t.Range)
                                | "seq" -> ts.Add("Prefer postfix syntax for sequences.", t.Range)
                                | "list" -> ts.Add("Prefer postfix syntax for lists.", t.Range)
                                | "option" -> ts.Add("Prefer postfix syntax for options.", t.Range)
                                | "voption" -> ts.Add("Prefer postfix syntax for value options.", t.Range)
                                | "Ref"
                                | "ref" -> ts.Add("Prefer postfix syntax for reference cells.", t.Range)
                                | _ -> ()
                            | _ -> ()
                        | _ -> ()
                }

            walkAst collector context.ParseFileResults.ParseTree

            return
                ts
                |> Seq.map (fun (message, range) ->
                    {
                        Type = "PostfixGenericsAnalyzer"
                        Message = message
                        Code = "IONIDE-002"
                        Severity = Info
                        Range = range
                        Fixes = []
                    }
                )
                |> Seq.toList
        }
