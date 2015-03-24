#I "tools"
#r "FSharp.Markdown.dll"
#r "FSharp.CodeFormat.dll"
#r "FSharp.Literate.dll"
#load "Formatters.fsx"
open FSharp.Literate
open System.IO

let codeDirectory = __SOURCE_DIRECTORY__ + "/code"
let postsDirectory = __SOURCE_DIRECTORY__ + "/_posts"
let fsiEval = Formatters.createFsiEvaluator "public/blog_imgs" "#.####"

Literate.ProcessDirectory(codeDirectory, outputDirectory = postsDirectory, format = OutputKind.Html, lineNumbers = true, fsiEvaluator = fsiEval, generateAnchors = true)
