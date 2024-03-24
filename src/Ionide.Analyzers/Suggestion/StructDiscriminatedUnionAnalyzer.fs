module Ionide.Analyzers.Suggestion.StructDiscriminatedUnionAnalyzer

open System.Collections.Generic
open FSharp.Compiler.Text
open FSharp.Compiler.Syntax
open FSharp.Compiler.SyntaxTrivia
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting
open System
open FSharp.Compiler.CodeAnalysis
open Ionide.Analyzers.TypedOperations

[<Literal>]
let message = "Consider adding [<Struct>] to Discriminated Union"

type private FixData =
    {
        LeadingKeyword: SynTypeDefnLeadingKeyword
        TypeDefnRange: range
        TypeName: Ident
    }

let private hasStructAttribute (attrs: SynAttributes) =
    attrs
    |> List.exists (fun attrList ->
        attrList.Attributes
        |> List.exists (fun a ->
            match List.tryLast a.TypeName.LongIdent with
            | Some structIdent -> structIdent.idText.StartsWith("Struct", StringComparison.Ordinal)
            | _ -> false
        )
    )

/// This is a Set of primitive-ish.
let private primitives =
    set
        [
            "Microsoft.FSharp.Core.bool"
            "Microsoft.FSharp.Core.byte"
            "Microsoft.FSharp.Core.char"
            "Microsoft.FSharp.Core.decimal"
            "Microsoft.FSharp.Core.float"
            "Microsoft.FSharp.Core.float32"
            "Microsoft.FSharp.Core.int"
            "Microsoft.FSharp.Core.int16"
            "Microsoft.FSharp.Core.int64"
            "Microsoft.FSharp.Core.sbyte"
            "Microsoft.FSharp.Core.string"
            "Microsoft.FSharp.Core.uint"
            "Microsoft.FSharp.Core.uint16"
            "System.DateTime"
            "System.Guid"
            "System.TimeSpan"
        ]

let private analyze
    (sourceText: ISourceText)
    (parsedInput: ParsedInput)
    (checkResults: FSharpCheckFileResults)
    : Message list
    =
    let typeDefs = HashSet<FixData>()

    let collector =
        { new SyntaxCollectorBase() with
            override x.WalkTypeDefn
                (
                    path,
                    SynTypeDefn(
                        typeRepr = typeRepr
                        trivia = { LeadingKeyword = lk }
                        range = m
                        typeInfo = SynComponentInfo(attributes = attributes; longId = typeName))
                )
                =
                if hasStructAttribute attributes then
                    ()
                else

                match List.tryLast typeName, typeRepr with
                | Some typeName, SynTypeDefnRepr.Simple(simpleRepr = SynTypeDefnSimpleRepr.Union _) ->
                    typeDefs.Add
                        {
                            LeadingKeyword = lk
                            TypeDefnRange = m
                            TypeName = typeName
                        }
                    |> ignore
                | _ -> ()
        }

    walkAst collector parsedInput

    typeDefs
    |> Seq.choose (fun data ->
        tryFSharpEntityFromIdent sourceText checkResults data.TypeName
        |> Option.bind (fun entity ->
            let allTypesArePrimitive =
                entity.UnionCases
                |> Seq.forall (fun uc ->
                    uc.Fields
                    |> Seq.forall (fun ff ->
                        if ff.FieldType.IsFunctionType then
                            false
                        else

                        primitives.Contains ff.FieldType.BasicQualifiedName

                    )
                )

            if not allTypesArePrimitive then
                None
            else

            let fixes =
                match data.LeadingKeyword with
                | SynTypeDefnLeadingKeyword.Type mType ->
                    let spaces = String.replicate mType.StartColumn " "

                    [
                        {
                            FromText = ""
                            FromRange = mType.StartRange
                            ToText = $"[<Struct>]\n%s{spaces}"
                        }
                    ]
                | SynTypeDefnLeadingKeyword.And _ ->
                    [
                        {
                            FromText = ""
                            FromRange = data.TypeName.idRange.StartRange
                            ToText = "[<Struct>] "
                        }
                    ]
                | _ -> []

            Some
                {
                    Type = "structDiscriminatedUnion"
                    Message = message
                    Code = "IONIDE-012"
                    Severity = Severity.Hint
                    Range = data.TypeDefnRange
                    Fixes = fixes
                }

        )

    )
    |> Seq.toList

[<Literal>]
let name = "StructDiscriminatedUnionAnalyzer"

[<Literal>]
let shortDescription = "Short description about StructDiscriminatedUnionAnalyzer"

[<Literal>]
let helpUri = "https://ionide.io/ionide-analyzers/suggestion/012.html"

[<CliAnalyzer(name, shortDescription, helpUri)>]
let structDiscriminatedUnionCliAnalyzer: Analyzer<CliContext> =
    fun (context: CliContext) ->
        async { return analyze context.SourceText context.ParseFileResults.ParseTree context.CheckFileResults }

[<EditorAnalyzer(name, shortDescription, helpUri)>]
let structDiscriminatedUnionEditorAnalyzer: Analyzer<EditorContext> =
    fun (context: EditorContext) ->
        async {
            match context.CheckFileResults with
            | None -> return []
            | Some checkResults -> return analyze context.SourceText context.ParseFileResults.ParseTree checkResults
        }
