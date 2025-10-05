module Ionide.Analyzers.Suggestion.HeadConsEmptyListPatternAnalyzer

open Ionide.Analyzers
open System.Collections.Generic
open FSharp.Analyzers.SDK
open FSharp.Compiler.Syntax
open FSharp.Compiler.Text
open FSharp.Analyzers.SDK.ASTCollecting

[<Literal>]
let ignoreComment = "IGNORE: IONIDE-007"

[<Literal>]
let message = "Replace `x :: _` with `[ x ]`"

let private analyze (sourceText: ISourceText) (parsedInput: ParsedInput) =
    let patterns = HashSet<range * string option>()

    let comments =
        match parsedInput with
        | ParsedInput.ImplFile parsedFileInput -> parsedFileInput.Trivia.CodeComments
        | _ -> []

    let hasIgnoreComment = Ignore.hasComment ignoreComment comments sourceText >> Option.isSome

    let collector =
        { new SyntaxCollectorBase() with
            override x.WalkPat(path, synPat) =
                match synPat with
                | SynPat.ListCons(lhsPat = lhsPat; rhsPat = SynPat.ArrayOrList(isArray = false; elementPats = []); range = m) when not <| hasIgnoreComment m ->
                    let text =
                        if lhsPat.Range.StartLine <> lhsPat.Range.EndLine then
                            None
                        else
                            Some($"[ %s{sourceText.GetSubTextFromRange(lhsPat.Range)} ]")

                    patterns.Add(synPat.Range, text) |> ignore
                | _ -> ()
        }

    walkAst collector parsedInput

    patterns
    |> Seq.map (fun (mPat, fixText) ->
        let fixes =
            match fixText with
            | None -> []
            | Some fixText ->
                [
                    {
                        FromText = ""
                        FromRange = mPat
                        ToText = fixText
                    }
                ]

        {
            Type = "HeadConsEmptyListPattern analyzer"
            Message = message
            Code = "IONIDE-007"
            Severity = Severity.Hint
            Range = mPat
            Fixes = fixes
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
    async { return analyze ctx.SourceText ctx.ParseFileResults.ParseTree }

[<EditorAnalyzer(name, shortDescription, helpUri)>]
let headConsEmptyListPatternEditorAnalyzer (ctx: EditorContext) =
    async { return analyze ctx.SourceText ctx.ParseFileResults.ParseTree }
