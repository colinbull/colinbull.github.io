module Formatters
#I "tools"
#r "FSharp.Markdown.dll"
#r "FSharp.Literate.dll"
#r "packages/FAKE/tools/FakeLib.dll"
#load "packages/FSharp.Charting/FSharp.Charting.fsx"

open System.IO
open FSharp.Literate
open FSharp.Markdown
open FSharp.Charting
open System.Drawing

// --------------------------------------------------------------------------------------
// Helper functions etc.
// --------------------------------------------------------------------------------------

open System.Windows.Forms
open FSharp.Charting.ChartTypes

/// Reasonably nice default style for charts
let chartStyle ch =
  let grid = ChartTypes.Grid(LineColor=System.Drawing.Color.LightGray)
  ch 
  |> Chart.WithYAxis(MajorGrid=grid)
  |> Chart.WithXAxis(MajorGrid=grid)

/// Checks if the given directory exists. If not then this functions creates the directory.
let ensureDirectory dir =
  let di = new DirectoryInfo(dir)
  if not di.Exists then di.Create()

/// Combine two paths
let (@@) a b = Path.Combine(a, b)


/// Builds FSI evaluator that can render System.Image, F# Charts, series & frames
let createFsiEvaluator output (floatFormat:string) =

  /// Counter for saving files
  let imageCounter = 
    let count = ref 0
    (fun () -> incr count; !count)

  let transformation (value:obj, typ:System.Type) =
    match value with 
    | :? System.Drawing.Image as img ->
        // Pretty print image - save the image to the "images" directory 
        // and return a DirectImage reference to the appropriate location
        let id = imageCounter().ToString()
        let file = "chart" + id + ".png"
        ensureDirectory (output @@ "images")
        img.Save(output @@ "images" @@ file, System.Drawing.Imaging.ImageFormat.Png) 
        Some [ Paragraph [DirectImage ("", ("images/" + file, None))]  ]

    | :? ChartTypes.GenericChart as ch ->
        // Pretty print F# Chart - save the chart to the "images" directory 
        // and return a DirectImage reference to the appropriate location
        let id = imageCounter().ToString()
        let file = "chart" + id + ".png"
        ensureDirectory (output @@ "images")
      
        // We need to reate host control, but it does not have to be visible
        ( use ctl = new ChartControl(chartStyle ch, Dock = DockStyle.Fill, Width=500, Height=300)
          ch.CopyAsBitmap().Save(output @@ "images" @@ file, System.Drawing.Imaging.ImageFormat.Png) )
        Some [ Paragraph [DirectImage ("", ("images/" + file, None))]  ]

    | _ -> None 
    
  // Create FSI evaluator, register transformations & return
  let fsiEvaluator = FsiEvaluator() 
  fsiEvaluator.RegisterTransformation(transformation)
  fsiEvaluator
