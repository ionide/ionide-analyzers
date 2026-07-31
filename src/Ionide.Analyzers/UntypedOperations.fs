module Ionide.Analyzers.UntypedOperations

open FSharp.Compiler.Text
open FSharp.Compiler.Syntax
open FSharp.Compiler.SyntaxTrivia

/// Any infix operator applied to its left-hand side.
/// Yields the operator as written in the source, its identifier and the left-hand side.
[<return: Struct>]
let (|AnyInfixOperator|_|) =
    function
    | SynExpr.App(ExprAtomicFlag.NonAtomic,
                  true,
                  SynExpr.LongIdent(
                      longDotId = SynLongIdent(
                          id = [ operatorIdent ]; trivia = [ Some(IdentTrivia.OriginalNotation originalNotation) ])),
                  argExpr,
                  _) -> ValueSome(originalNotation, operatorIdent, argExpr)
    | _ -> ValueNone

[<return: Struct>]
let (|InfixOperator|_|) (originalText: string) =
    function
    | AnyInfixOperator(originalNotation, operatorIdent, argExpr) when originalNotation = originalText ->
        ValueSome(operatorIdent, argExpr)
    | _ -> ValueNone

[<return: Struct>]
let (|OpEquality|_|) = (|InfixOperator|_|) "="

[<return: Struct>]
let (|OpInequality|_|) = (|InfixOperator|_|) "<>"

[<return: Struct>]
let (|OpPipeRight|_|) = (|InfixOperator|_|) "|>"
