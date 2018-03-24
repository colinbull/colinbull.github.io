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
        inv.Set_num, p
    )

let minAvgMax f data = 
    data |> Seq.minBy f |> f,
    data |> Seq.averageBy (f >> float),
    data |> Seq.maxBy f |> f

let byYear f = 
    setData 
    |> Seq.groupBy (fun x -> x.Year)
    |> Seq.map (fun (year,sets) -> year, f sets)
    |> Seq.sortBy fst

do
    let data = 
        byYear (minAvgMax (fun x -> x.Num_parts))

    Chart.Combine [
        Chart.Line (data |> Seq.map (fun (x,(y,_,_)) -> x, y), Name = "min")
        Chart.Line (data |> Seq.map (fun (x,(_,y,_)) -> x, y), Name = "avg")
        Chart.Line (data |> Seq.map (fun (x,(_,_,y)) -> x, y), Name = "max")
    ]
    |> Chart.Show 

let setWithMostDistinctParts() = 
    let setMap = setData |> Seq.map (fun x -> x.Set_num, x) |> Map.ofSeq
    let (setId, distinctParts) =
        inventorySetData
        |> Seq.groupBy fst 
        |> Seq.map (fun (setNum, parts) -> setNum, parts |> Seq.distinctBy (fun (_,p) -> p.Part_num) |> Seq.length)
        |> Seq.maxBy snd
    printfn "%A has the most distinct parts @ %d" setMap.[setId] distinctParts


 
    

