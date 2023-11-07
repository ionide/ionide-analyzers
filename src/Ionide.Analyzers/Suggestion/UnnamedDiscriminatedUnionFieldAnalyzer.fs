module Ionide.Analyzers.Suggestion.UnnamedDiscriminatedUnionFieldAnalyzer

open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting
open FSharp.Compiler.Syntax

[<CliAnalyzer("UnnamedDiscriminatedUnionFieldAnalyzer",
              "Verifies each field in a union case is named.",
              "https://ionide.io/ionide-analyzers/hints/004.html")>]
let unnamedDiscriminatedUnionFieldAnalyzer (ctx: CliContext) =
    async {
        let messages = ResizeArray<Message>()

        let visitor =
            { new SyntaxCollectorBase() with
                override x.WalkUnionCase(unionCase: SynUnionCase) =
                    match unionCase with
                    | SynUnionCase(caseType = SynUnionCaseKind.Fields [ _ ]) -> ()
                    | SynUnionCase(caseType = SynUnionCaseKind.Fields fields) ->
                        let unNamedFields =
                            fields
                            |> List.choose (fun (SynField(idOpt = idOpt; range = mField)) ->
                                if Option.isNone idOpt then Some mField else None
                            )

                        for mField in unNamedFields do
                            messages.Add
                                {
                                    Type = "UnnamedDiscriminatedUnionFieldAnalyzer"
                                    Message = "Field inside union case is not named!"
                                    Code = "004"
                                    Severity = Severity.Error
                                    Range = mField
                                    Fixes = []
                                }

                    | _ -> ()
            }

        walkAst visitor ctx.ParseFileResults.ParseTree

        return Seq.toList messages
    }
