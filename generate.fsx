#I "packages/FSharp.Formatting/lib/net40/"
#r "packages/RazorEngine/lib/net40/RazorEngine.dll"
#r "packages/FSharp.Compiler.Service/lib/net40/FSharp.Compiler.Service.dll"
#r "FSharp.Literate.dll"
#r "FSharp.CodeFormat.dll"
#r "FSharp.MetadataFormat.dll"
open System.IO
open FSharp.Literate

Literate.ProcessDirectory(__SOURCE_DIRECTORY__ + "/code", __SOURCE_DIRECTORY__ + "/code/post-template.html", __SOURCE_DIRECTORY__ +  "/_posts", OutputKind.Html)