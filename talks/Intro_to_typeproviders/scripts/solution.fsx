#I "libs"
#r "fsharp.data/lib/net45/FSharp.Data.dll"
#r "fsharp.charting/lib/net45/FSharp.Charting.dll"

open FSharp.Data
open FSharp.Charting

let [<Literal>] colors = __SOURCE_DIRECTORY__ + "/lego-database/colors.csv"
let [<Literal>] sets = __SOURCE_DIRECTORY__ + "/lego-database/sets.csv"
let [<Literal>] parts = __SOURCE_DIRECTORY__ + "/lego-database/parts.csv"
let [<Literal>] inventories = __SOURCE_DIRECTORY__ + "/lego-database/inventories.csv"
let [<Literal>] inventoryParts = __SOURCE_DIRECTORY__ + "/lego-database/inventory_parts.csv"
let [<Literal>] inventorySets = __SOURCE_DIRECTORY__ + "/lego-database/inventory_sets.csv"

type Stats = {
    Min : float 
    Max : float
    Avg : float 
    StdDev : float
    Length : float
}

let inline statsBy f data = 
    let min = data |> Seq.minBy f |> f
    let max = data |> Seq.maxBy f |> f
    let avg = data |> Seq.averageBy f
    let len = data |> Seq.length |> float
    let ssd = data |> Seq.sumBy (fun x -> ((f x) - avg) ** 2.)
    let stddev = sqrt (ssd / len)
    {
        Min = min; Max = max; Avg = avg 
        StdDev = stddev
        Length = len
    }

type Colors = CsvProvider<colors> 
type Sets = CsvProvider<sets>
type Parts = CsvProvider<parts>
type Inventories = CsvProvider<inventories>
type InventoryParts = CsvProvider<inventoryParts>
type InventorySets = CsvProvider<inventorySets>


let setData = 
    Sets.Load(sets).Rows
    
let inventoryData = 
    Inventories.Load(inventories).Rows
    |> Seq.map (fun x -> x.Id, x)
    |> Map.ofSeq

let inventorySetData = 
    InventoryParts.Load(inventoryParts).Rows
    |> Seq.map (fun p -> 
        let inv = inventoryData.[p.Inventory_id]
        inv.Set_num, p.Part_num
    )

let byYear f = 
    setData 
    |> Seq.groupBy (fun x -> x.Year)
    |> Seq.map (fun (year,sets) -> year, f sets)
    |> Seq.sortBy fst

let stats = 
   byYear (statsBy (fun x -> float x.Num_parts))

do
    Chart.Combine [
        Chart.Line (stats |> Seq.map (fun (x,y) -> x, y.Min), Name = "min")
        Chart.Line (stats |> Seq.map (fun (x,y) -> x, y.Avg), Name = "avg")
        Chart.Line (stats |> Seq.map (fun (x,y) -> x, y.Max), Name = "max")
        Chart.Line (stats |> Seq.map (fun (x,y) -> x, y.StdDev), Name = "stddev")
    ]
    |> Chart.Show 

do
    Chart.Combine [
        Chart.Line (stats |> Seq.map (fun (x,y) -> x, (y.StdDev / y.Avg)), Name = "growth")
    ]
    |> Chart.Show 


let setWithMostDistinctParts() = 
    //let setMap = setData |> Seq.map (fun x -> x.Set_num, x) |> Map.ofSeq
    //let (setId, distinctParts) =
        inventorySetData
        |> Seq.groupBy fst 
        |> Seq.map (fun (setNum, parts) -> setNum, parts |> Seq.distinctBy (fun (_,p) -> p.Part_num) |> Seq.length)
        |> Seq.maxBy snd
    //printfn "%A has the most distinct parts @ %d" setMap.[setId] distinctParts

type F1 = HtmlProvider<"https://en.wikipedia.org/wiki/Formula_One">

let flags =
    let tbls = F1.GetSample().Tables
    tbls.Flags.Rows 

do 
    for flag in flags do 
        printfn "%A - %s" flag.Flag flag.Meaning
 
    

