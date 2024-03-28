module Ionide.Analyzers.TypedOperations

open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Symbols
open FSharp.Compiler.Text
open FSharp.Compiler.Syntax

let tryFSharpMemberOrFunctionOrValueFromIdent
    (sourceText: ISourceText)
    (checkResults: FSharpCheckFileResults)
    (ident: Ident)
    =
    let line = sourceText.GetLineString(ident.idRange.EndLine - 1)

    checkResults.GetSymbolUseAtLocation(ident.idRange.EndLine, ident.idRange.EndColumn, line, [ ident.idText ])
    |> Option.bind (fun symbolUse ->
        match symbolUse.Symbol with
        | :? FSharpMemberOrFunctionOrValue as mfv -> Some mfv
        | _ -> None
    )

let tryFSharpEntityFromIdent (sourceText: ISourceText) (checkResults: FSharpCheckFileResults) (ident: Ident) =
    let line = sourceText.GetLineString(ident.idRange.EndLine - 1)

    checkResults.GetSymbolUseAtLocation(ident.idRange.EndLine, ident.idRange.EndColumn, line, [ ident.idText ])
    |> Option.bind (fun symbolUse ->
        match symbolUse.Symbol with
        | :? FSharpEntity as fe -> Some fe
        | _ -> None
    )
