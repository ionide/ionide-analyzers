namespace Ionide.Analyzers

open System
open FSharp.Compiler.SyntaxTrivia
open FSharp.Compiler.Text

[<RequireQualifiedAccess>]
module Ignore =
    
    /// checks there is an ignore comment on the line above the range
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