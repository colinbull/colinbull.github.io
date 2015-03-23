#I "tools"
#r "FSharp.Markdown.dll"
#r "FSharp.CodeFormat.dll"
#r "FSharp.Literate.dll"
open FSharp.Literate
open System.IO

let codeDirectory = __SOURCE_DIRECTORY__ + "/code"
let postsDirectory = __SOURCE_DIRECTORY__ + "/_posts"
let fsiEval = FsiEvaluator()

Literate.ProcessDirectory(codeDirectory, outputDirectory = postsDirectory, format = OutputKind.Html, lineNumbers = true, fsiEvaluator = fsiEval, generateAnchors = true)
