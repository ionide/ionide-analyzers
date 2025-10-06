module Ionide.Analyzers.Suggestion.UnnamedDiscriminatedUnionFieldAnalyzer

open Ionide.Analyzers
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting
open FSharp.Compiler.Syntax
open FSharp.Compiler.Text

[<Literal>]
let ignoreComment = "IGNORE: IONIDE-004"

let analyze (sourceText: ISourceText) parseTree =
    let messages = ResizeArray<Message>()
    let comments = InputOperations.getCodeComments parseTree
    let hasIgnoreComment = Ignore.hasComment ignoreComment comments sourceText

    let visitor =
        { new SyntaxCollectorBase() with
            override x.WalkUnionCase(_, unionCase: SynUnionCase) =
                match unionCase with
                | SynUnionCase(caseType = SynUnionCaseKind.Fields [ _ ]) -> ()
                | SynUnionCase(caseType = SynUnionCaseKind.Fields fields; range = range) when
                    hasIgnoreComment range |> Option.isNone
                    ->
                    let unnamedFields =
                        fields
                        |> List.choose (fun (SynField(idOpt = idOpt; range = mField)) ->
                            if Option.isNone idOpt then Some mField else None
                        )

                    for mField in unnamedFields do
                        messages.Add
                            {
                                Type = "UnnamedDiscriminatedUnionFieldAnalyzer"
                                Message = "Field inside union case is not named!"
                                Code = "IONIDE-004"
                                Severity = Severity.Hint
                                Range = mField
                                Fixes = []
                            }

                | _ -> ()
        }

    walkAst visitor parseTree

    Seq.toList messages

[<Literal>]
let name = "UnnamedDiscriminatedUnionFieldAnalyzer"

[<Literal>]
let shortDescription = "Verifies each field in a union case is named."

[<Literal>]
let helpUri = "https://ionide.io/ionide-analyzers/suggestion/004.html"

[<CliAnalyzer(name, shortDescription, helpUri)>]
let unnamedDiscriminatedUnionFieldCliAnalyzer (ctx: CliContext) =
    async { return analyze ctx.SourceText ctx.ParseFileResults.ParseTree }

[<EditorAnalyzer(name, shortDescription, helpUri)>]
let unnamedDiscriminatedUnionFieldEditorAnalyzer (ctx: EditorContext) =
    async { return analyze ctx.SourceText ctx.ParseFileResults.ParseTree }
