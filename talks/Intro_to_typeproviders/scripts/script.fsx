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




    

