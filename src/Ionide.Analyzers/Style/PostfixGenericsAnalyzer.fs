module Ionide.Analyzers.Style.PostfixGenericsAnalyzer

open Ionide.Analyzers
open FSharp.Compiler.Text
open FSharp.Compiler.Syntax
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting

[<Literal>]
let ignoreComment = "IGNORE: IONIDE-002"

let analyze (sourceText: ISourceText) (input: ParsedInput) =
    let ts = ResizeArray<string * range>()

    let comments =
        match input with
        | ParsedInput.ImplFile parsedFileInput -> parsedFileInput.Trivia.CodeComments
        | ParsedInput.SigFile parsedSigFileInput -> parsedSigFileInput.Trivia.CodeComments

    let hasIgnoreComment = Ignore.hasComment ignoreComment comments sourceText

    let collector =
        { new SyntaxCollectorBase() with
            override x.WalkType(_, t: SynType) =
                match t, hasIgnoreComment t.Range with
                | SynType.Array _, None -> 
                    ts.Add("Prefer postfix syntax for arrays.", t.Range)
                | SynType.App(typeName = SynType.LongIdent synLongIdent; isPostfix = false), None ->
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

    walkAst collector input

    ts
    |> Seq.map (fun (message, range) ->
        {
            Type = "PostfixGenericsAnalyzer"
            Message = message
            Code = "IONIDE-002"
            Severity = Severity.Info
            Range = range
            Fixes = []
        }
    )
    |> Seq.toList

[<Literal>]
let name = "PostfixGenericsAnalyzer"

[<Literal>]
let shortDescription = "Detect if generic type should be in the postfix position."

[<Literal>]
let helpUri = "https://ionide.io/ionide-analyzers/style/002.html"

[<EditorAnalyzer(name, shortDescription, helpUri)>]
let postfixGenericsEditorAnalyzer (ctx: EditorContext) =
    async { return analyze ctx.SourceText ctx.ParseFileResults.ParseTree }

[<CliAnalyzer(name, shortDescription, helpUri)>]
let postfixGenericsCliAnalyzer (ctx: CliContext) =
    async { return analyze ctx.SourceText ctx.ParseFileResults.ParseTree }