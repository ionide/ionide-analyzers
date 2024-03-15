module Ionide.Analyzers.Suggestion.HeadConsEmptyListPatternAnalyzer

open System.Collections.Generic
open FSharp.Analyzers.SDK
open FSharp.Compiler.Syntax
open FSharp.Compiler.Text
open FSharp.Analyzers.SDK.ASTCollecting

[<Literal>]
let message = "Replace `[ x ] :: _` with `[ x ]`"

let private analyze (parsedInput: ParsedInput) =
    let patterns = HashSet<range>(Range.comparer)

    let collector =
        { new SyntaxCollectorBase() with
            override x.WalkPat(path, synPat) =
                match synPat with
                | SynPat.ListCons(rhsPat = SynPat.ArrayOrList(isArray = false; elementPats = [])) ->
                    patterns.Add synPat.Range |> ignore
                | _ -> ()
        }

    walkAst collector parsedInput

    patterns
    |> Seq.map (fun mPat ->
        {
            Type = "HeadConsEmptyListPattern analyzer"
            Message = message
            Code = "IONIDE-007"
            Severity = Severity.Hint
            Range = mPat
            Fixes = []
        }
    )
    |> Seq.toList

[<Literal>]
let name = "HeadConsEmptyListPatternAnalyzer"

[<Literal>]
let shortDescription = "Pattern match on single item list instead of head :: []"

[<Literal>]
let helpUri = "https://ionide.io/ionide-analyzers/suggestion/007.html"

[<CliAnalyzer(name, shortDescription, helpUri)>]
let headConsEmptyListPatternCliAnalyzer (ctx: CliContext) =
    async { return analyze ctx.ParseFileResults.ParseTree }

[<EditorAnalyzer(name, shortDescription, helpUri)>]
let headConsEmptyListPatternEditorAnalyzer (ctx: EditorContext) =
    async { return ctx.ParseFileResults.ParseTree }
