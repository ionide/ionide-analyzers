namespace Ionide.Analyzers

open System
open FSharp.Compiler.SyntaxTrivia
open FSharp.Compiler.Text

[<RequireQualifiedAccess>]
module Ignore =
    
    /// A standard pattern within an analyzer is to look for a specific comment preceding a problematic line,
    /// indicating "suppress the analyzer on this line".
    /// This function performs that common check, returning the comment that caused the analyzer to deactivate.
    let hasComment
        (magicComment : string)
        (comments : CommentTrivia list)
        (sourceText : ISourceText)
        (analyzerTriggeredOn : Range)
        : CommentTrivia option
        =
        comments
        |> List.tryFind (fun c ->
            match c with
            | CommentTrivia.BlockComment r
            | CommentTrivia.LineComment r ->
                if r.StartLine <> analyzerTriggeredOn.StartLine - 1 then
                    false
                else
                    let lineOfComment = sourceText.GetLineString (r.StartLine - 1) // 0-based

                    lineOfComment.Contains (magicComment, StringComparison.OrdinalIgnoreCase)
        )