(*** raw ***)
//---
//layout: page
//title: Enterprise Lenses.
//---

(*** hide ***)
#r "../packages/Aether/lib/net35/Aether.dll"
#r "../packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#r "System.Xml.Linq.dll"

open System
open System.Linq
open System.Collections
open System.Collections.Generic
open System.Xml.Linq
open System.Xml.XPath
open Aether
open Aether.Operators
open FSharp.Data

type Csv private(headers : string[], data : string [] []) = 
     
     let headers = ResizeArray<string>(headers)
     let data = ResizeArray(data |> Array.map (ResizeArray)) 

     static member Empty = Csv([||], [||])

     static member Read(file:string) = 
            let csv = CsvFile.Load(file)
            let headers = 
                match csv.Headers with
                | Some hs -> hs 
                | None -> failwithf "The csv file must have headers"

            new Csv(headers, [| for header in headers do yield [|for row in csv.Rows -> row.GetColumn(header)|] |])

     member x.GetValue(col, indx) = 
        match data |> Seq.tryItem indx with
        | Some row ->
            headers 
            |> Seq.tryFindIndex ((=) col)
            |> Option.bind (fun colIndx -> row |> Seq.tryItem colIndx)
        | None -> None

     member x.SetValue(col, indx, value) = 
        match data |> Seq.tryItem indx with
        | Some row ->
            match headers |> Seq.tryFindIndex ((=) col) with
            | Some colIndex ->
                row.Insert(colIndex, value)
            | None -> 
                headers.Add(col)
                row.Insert(headers.Count - 1, value) 
        | None -> 
            let row = ResizeArray<_>()
            match headers |> Seq.tryFindIndex ((=) col) with
            | Some colIndex ->
                row.Insert(colIndex, value)
            | None -> 
                headers.Add(col)
                row.Insert(headers.Count - 1, value)
            data.Insert(indx, row) 

     member x.Data 
        with get() = 
            [|
                yield headers |> Seq.toArray
                for row in data do
                    yield [| for value in row -> value |]
            |]
    
     override x.ToString() = 
        let sb = new Text.StringBuilder()
        for row in x.Data do 
            sb.AppendLine(String.concat "," row) |> ignore
        sb.ToString()

     member x.Save(path) = 
        IO.File.WriteAllText(path, x.ToString())


[<AutoOpen>]
module Lenses = 

      let getValue defaultV (x:obj) =
            match x with
            | null -> defaultV
            | :? XAttribute as a -> a.Value
            | :? XElement as e -> e.Value
            | _ -> failwithf "unable to set value"

      let setValue (x:obj) v = 
          match x with
          | :? XAttribute as a -> a.SetValue(v)
          | :? XElement as e -> e.SetValue(v)
          | _ -> failwithf "unable to set value"
          unbox<_> x

      let xpath_<'a> (path:string) : Lens<XElement, _> = 
          (fun x -> (x.XPathEvaluate(path) :?> IEnumerable).Cast<'a>().FirstOrDefault()),
          (fun s x -> x)
      
      let xml_<'a> defaultV path = 
          let l : Lens<_,_> = (fun x -> getValue defaultV x), (fun s x -> setValue x s)
          xpath_<'a> path >-> l

      let xattr_ = xml_<XAttribute>

      let xelem_ = xml_<XElement> 

      let null_ defaultV : Lens<_,_> = 
          (fun x -> if x = defaultV then None else Some x),
          (fun s x -> defaultArg s defaultV)

      let csv_ indx (col:string) : Prism<Csv, _> =
          (fun x -> x.GetValue(col,indx)),
          (fun s x -> x.SetValue(col, indx, s); x)

      let inline getSet (get,set) target source =
          let value = (Optic.get get source)
          Optic.set set value target

      let inline etl mappings target data = 
          data 
          |> Seq.indexed
          |> Seq.fold (fun s (i, x) -> Seq.fold (fun s mapping -> getSet mapping s x) s (mappings i)) target

let doc = XDocument.Load("code/data/lenses/source_data.xml")
let products = doc.XPathSelectElements("//catalog_item/size")

let xmlToCsv indx = 
    let shortHandSize : Isomorphism<_,_> = 
        (function 
         | "S" -> "Small"
         | "M" -> "Medium"
         | "L" -> "Large"
         | "XL" -> "Extra Large"
         | a -> a),
        (function 
         | "Small" -> "S"
         | "Medium" -> "M"
         | "Large" -> "L"
         | "Extra Large" -> "XL"
         | a -> a)
        
    [
       xelem_ "NULL" "parent::catalog_item/item_number", (csv_ indx "item_no")
       xattr_ "NULL" "ancestor::product/@description", (csv_ indx "description")
       xattr_ "NULL" "parent::catalog_item/@gender", (csv_ indx "gender")
       xattr_ "NULL" "@description", (csv_ indx "size")
       xattr_ "NULL" "@description", (csv_ indx "short_size" >?> shortHandSize)
       xelem_ "NULL" "parent::catalog_item/price", (csv_ indx "price")       
    ]

(etl xmlToCsv Csv.Empty products).ToString()
    
(**

## A typical enterprise problem

Based on a gut feel and no real evidence :) apart from my own experience I would say that a large proportion of enterprise applications are essentially just some sort of Extract - Transform - Load for system A to system B. Now if we look at this in the context of a single application we can reduce this to a simple function. 

      (unit -> 'b) -> ('b -> 'c) -> ('c -> unit)

For the sake of argument, lets say that the requirement for our application is to read data from a XML file, apply some transformation and then write it out to another CSV file.

Now this is a fairly trivial problem, to solve and there are many ways to do it all fairly simple, for example a naive solution might be to do something like the following.


So the above implementation is all well and good for a one off implementation. It works and is reasonably maintainable. However this is a trivial problem and often the functions that extract, transform and load the data aren't as simple as the above. For example, the above example we do not    

##Introducing Lenses

At [F# exchange](https://skillsmatter.com/conferences/7145-f-exchange-2016#program), I got into a couple of converstaions around enterprise development and the patterns that I use when developing line of business patterns with F#. Typically the discussion focused on the lack of patterns and this is mainly due F#'s strong type system and the fact that higher order functions are a [pretty powerful abstraction](https://www.infoq.com/presentations/fp-design-patterns). However one topic that promoteda certain amount of contention was Lenses.

Now I can't say I am surprised by this there are a couple of reasons why,

* *Performance* - Extra Allocations caused by individually setting properties on records can have a reasonable performace impact when use in time critical code.

* *No Language Support* - Writing lenses for records can be a large overhead and currently there is no language support. Maybe this will happen in the [future](https://fslang.uservoice.com/forums/245727-f-language/suggestions/6906132-implement-first-class-lensing-lenses-in-f) who knows??

With this stated thou, performance is often not so important in the enterprise and I will often favour maintainable and readable code over performant code.

      
*)

