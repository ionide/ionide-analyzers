module Ionide.Analyzers.Performance.ReturnStructPartialActivePatternAnalyzer

open System
open System.Collections.Generic
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Symbols
open FSharp.Compiler.Text
open FSharp.Compiler.Syntax
open FSharp.Compiler.SyntaxTrivia
open FSharp.Analyzers.SDK
open FSharp.Analyzers.SDK.ASTCollecting
open Ionide.Analyzers
open Ionide.Analyzers.TypedOperations

[<Literal>]
let message = "Consider adding [<return: Struct>] to partial active pattern."

[<return: Struct>]
let (|PartialActivePatternName|_|) (pat: SynPat) =
    match pat with
    | SynPat.LongIdent(longDotId = SynLongIdent(id = [ ident ]; trivia = [ Some(IdentTrivia.HasParenthesis _) ]))
    | SynPat.Named(ident = SynIdent(ident, Some(IdentTrivia.HasParenthesis _))) ->
        if not (ident.idText.EndsWith("|_|", StringComparison.Ordinal)) then
            ValueNone
        else
            ValueSome ident
    | _ -> ValueNone

let hasReturnStructAttribute (attributeList: SynAttributeList) =
    attributeList.Attributes
    |> List.exists (fun { Target = target; TypeName = typeName } ->
        match target, List.tryLast typeName.LongIdent with
        | Some target, Some structIdent ->
            target.idText = "return"
            && (structIdent.idText = "Struct" || structIdent.idText = "StructAttribute")
        | _ -> false
    )

type FixData =
    {
        FunctionName: Ident
        SomeOrNone: Ident list
        LeadingKeyword: SynLeadingKeyword
        ReturnTypeOption: range option
    }

let rec sequence<'a, 'ret> (recursions: (('a -> 'ret) -> 'ret) list) (finalContinuation: 'a list -> 'ret) : 'ret =
    match recursions with
    | [] -> [] |> finalContinuation
    | recurse :: recurses -> recurse (fun ret -> sequence recurses (fun rets -> ret :: rets |> finalContinuation))

/// Collect all occurrences of Some or None at the end of an expression branch
let rec collectSomeAndNoneFromExprBody (expr: SynExpr) (finalContinuation: Ident list -> Ident list) : Ident list =
    match expr with
    | SynExpr.App(funcExpr = SynExpr.Ident someIdent) when someIdent.idText = "Some" -> finalContinuation [ someIdent ]
    | SynExpr.Ident ident when ident.idText = "None" -> finalContinuation [ ident ]
    | SynExpr.TryFinally(tryExpr = ax; finallyExpr = bx)
    | SynExpr.IfThenElse(thenExpr = ax; elseExpr = Some bx)
    | SynExpr.Sequential(expr1 = ax; expr2 = bx) ->
        let continuations =
            [ collectSomeAndNoneFromExprBody ax; collectSomeAndNoneFromExprBody bx ]

        let finalContinuation (nodes: Ident list list) : Ident list =
            List.collect id nodes |> finalContinuation

        sequence continuations finalContinuation

    | SynExpr.Match(clauses = clauses)
    | SynExpr.MatchBang(clauses = clauses)
    | SynExpr.MatchLambda(matchClauses = clauses) ->
        let continuations =
            clauses
            |> List.map (fun (SynMatchClause(resultExpr = e)) -> collectSomeAndNoneFromExprBody e)

        let finalContinuation (nodes: Ident list list) : Ident list =
            List.collect id nodes |> finalContinuation

        sequence continuations finalContinuation

    | SynExpr.TryWith(tryExpr = tryExpr; withCases = clauses) ->
        let continuations =
            clauses
            |> List.map (fun (SynMatchClause(resultExpr = e)) -> collectSomeAndNoneFromExprBody e)
            |> fun tail -> (collectSomeAndNoneFromExprBody tryExpr) :: tail

        let finalContinuation (nodes: Ident list list) : Ident list =
            List.collect id nodes |> finalContinuation

        sequence continuations finalContinuation

    | SynExpr.LetOrUse(body = expr)
    | SynExpr.LetOrUseBang(body = expr)
    | SynExpr.Paren(expr = expr)
    | SynExpr.Typed(expr = expr) -> collectSomeAndNoneFromExprBody expr finalContinuation
    | _ -> finalContinuation []

let analyze (sourceText: ISourceText) (parsedInput: ParsedInput) (checkResults: FSharpCheckFileResults) : Message list =
    let idents = HashSet<FixData>()

    let collector =
        { new SyntaxCollectorBase() with
            override x.WalkSynModuleDecl(_path, decl) =
                match decl with
                | SynModuleDecl.Let(bindings = bindings) ->
                    for b in bindings do
                        match b with
                        | SynBinding(
                            headPat = PartialActivePatternName ident
                            attributes = attributes
                            returnInfo = returnInfo
                            expr = expr
                            trivia = { LeadingKeyword = leadingKeyword }) ->
                            if not (List.exists hasReturnStructAttribute attributes) then
                                let someOrNone = collectSomeAndNoneFromExprBody expr id

                                let optionInReturnInfo =
                                    returnInfo
                                    |> Option.bind (fun (SynBindingReturnInfo(typeName = t)) ->
                                        match t with
                                        | SynType.App(
                                            typeName = SynType.LongIdent(longDotId = SynLongIdent(id = [ optionIdent ]))) when
                                            optionIdent.idText = "option"
                                            ->
                                            Some optionIdent.idRange
                                        | _ -> None
                                    )

                                idents.Add
                                    {
                                        LeadingKeyword = leadingKeyword
                                        FunctionName = ident
                                        SomeOrNone = someOrNone
                                        ReturnTypeOption = optionInReturnInfo
                                    }
                                |> ignore
                        | _ -> ()
                | _ -> ()
        }

    walkAst collector parsedInput

    idents
    |> Seq.choose (fun data ->
        let ident = data.FunctionName

        tryFSharpMemberOrFunctionOrValueFromIdent sourceText checkResults ident
        |> Option.bind (fun mfv ->
            if
                not mfv.IsActivePattern
                || mfv.ReturnParameter.Type.IsFunctionType
                || mfv.ReturnParameter.Type.BasicQualifiedName = "Microsoft.FSharp.Core.voption`1"
            then
                None
            else

            let fixes =
                let indentFunction = String.replicate data.LeadingKeyword.Range.StartColumn " "

                [
                    // Add the [<return: Struct>] attribute
                    {
                        FromText = ""
                        FromRange = data.LeadingKeyword.Range.StartRange
                        ToText = $"[<return: Struct>]\n%s{indentFunction}"
                    }
                    // Replace : x option to : x voption
                    match data.ReturnTypeOption with
                    | None -> ()
                    | Some mOption ->
                        {
                            FromText = ""
                            FromRange = mOption
                            ToText = "voption"
                        }
                    // Replace Some with ValueSome and None with ValueNone
                    for someOrNone in data.SomeOrNone do
                        {
                            FromText = ""
                            FromRange = someOrNone.idRange
                            ToText = $"Value%s{someOrNone.idText}"
                        }
                ]

            Some
                {
                    Type = "returnStructPartialActivePattern"
                    Message = message
                    Code = "IONIDE-009"
                    Severity = Severity.Info
                    Range = ident.idRange
                    Fixes = fixes
                }
        )
    )
    |> Seq.toList

[<Literal>]
let name = "ReturnStructPartialActivePatternAnalyzer"

[<Literal>]
let shortDescription =
    "Short description about ReturnStructPartialActivePatternAnalyzer"

[<Literal>]
let helpUri = "https://ionide.io/ionide-analyzers/performance/009.html"

let languageFeature = lazy LanguageFeatureShim("StructActivePattern")

let runIfLanguageFeatureIsSupported
    (sourceText: ISourceText)
    (parsedInput: ParsedInput)
    (checkResults: FSharpCheckFileResults)
    (projectOptions: AnalyzerProjectOptions)
    : Message list
    =
    let languageVersionShim =
        LanguageVersionShim.fromOtherOptions projectOptions.OtherOptions

    if not (languageVersionShim.SupportsFeature(languageFeature.Value)) then
        []
    else
        analyze sourceText parsedInput checkResults

[<CliAnalyzer(name, shortDescription, helpUri)>]
let returnStructPartialActivePatternCliAnalyzer: Analyzer<CliContext> =
    fun (context: CliContext) ->
        async {
            return
                runIfLanguageFeatureIsSupported
                    context.SourceText
                    context.ParseFileResults.ParseTree
                    context.CheckFileResults
                    context.ProjectOptions
        }

[<EditorAnalyzer(name, shortDescription, helpUri)>]
let returnStructPartialActivePatternEditorAnalyzer: Analyzer<EditorContext> =
    fun (context: EditorContext) ->
        async {
            match context.CheckFileResults with
            | None -> return []
            | Some checkResults ->
                return
                    runIfLanguageFeatureIsSupported
                        context.SourceText
                        context.ParseFileResults.ParseTree
                        checkResults
                        context.ProjectOptions
        }
