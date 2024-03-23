module Ionide.Analyzers.UntypedOperations

open FSharp.Compiler.Text
open FSharp.Compiler.Syntax
open FSharp.Compiler.SyntaxTrivia

[<return: Struct>]
let (|InfixOperator|_|) (originalText: string) =
    function
    | SynExpr.App(ExprAtomicFlag.NonAtomic,
                  true,
                  SynExpr.LongIdent(
                      longDotId = SynLongIdent(trivia = [ Some(IdentTrivia.OriginalNotation originalNotation) ])),
                  argExpr,
                  _) when originalNotation = originalText -> ValueSome argExpr
    | _ -> ValueNone

[<return: Struct>]
let (|OpEquality|_|) = (|InfixOperator|_|) "="

[<return: Struct>]
let (|OpInequality|_|) = (|InfixOperator|_|) "<>"

[<return: Struct>]
let (|OpPipeRight|_|) = (|InfixOperator|_|) "|>"
