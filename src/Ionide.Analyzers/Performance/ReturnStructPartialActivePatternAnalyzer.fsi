module Ionide.Analyzers.Performance.ReturnStructPartialActivePatternAnalyzer

open FSharp.Analyzers.SDK

[<Literal>]
val message: string = "Consider adding [<return: Struct>] to partial active pattern."

[<Literal>]
val name: string = "ReturnStructPartialActivePatternAnalyzer"

[<Literal>]
val shortDescription: string = "Short description about ReturnStructPartialActivePatternAnalyzer"

[<Literal>]
val helpUri: string = "https://ionide.io/ionide-analyzers/performance/009.html"

[<CliAnalyzer(name, shortDescription, helpUri)>]
val returnStructPartialActivePatternCliAnalyzer: context: CliContext -> Async<Message list>

[<EditorAnalyzer(name, shortDescription, helpUri)>]
val returnStructPartialActivePatternEditorAnalyzer: context: EditorContext -> Async<Message list>
