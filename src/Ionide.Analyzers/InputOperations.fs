namespace Ionide.Analyzers

open FSharp.Compiler.Syntax

module InputOperations =

    let getCodeComments input =
        match input with
        | ParsedInput.ImplFile parsedFileInput -> parsedFileInput.Trivia.CodeComments
        | ParsedInput.SigFile parsedSigFileInput -> parsedSigFileInput.Trivia.CodeComments