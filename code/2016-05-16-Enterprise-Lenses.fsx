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

type CsvFile private(data : IDictionary<_,_>) = 
     
     let data = Dictionary<string, string[]>(data)

     static member Read(file:string) = 
            let data = new Dictionary<_,_>() :> IDictionary<_,_>
            let csv = CsvFile.Load(file)
            let headers = 
                match csv.Headers with
                | Some hs -> hs 
                | None -> failwithf "The csv file must have headers"
            
            for header in headers do
                data.[header] <- [|for row in csv.Rows  -> row.GetColumn(header)|]

            new CsvFile(data)

     member x.GetValue(col, indx) = data.[col].[indx]      
     member x.SetValue(col, indx, value) = data.[col].[indx] <- value

[<AutoOpen>]
module Lenses = 

      let getValue (x:obj) =
            match x with
            | :? XAttribute as a -> a.Value
            | :? XElement as e -> e.Value
            | _ -> failwithf "unable to set value"

      let setValue (x:obj) v = 
          match x with
          | :? XAttribute as a -> a.SetValue(v)
          | :? XElement as e -> e.SetValue(v)
          | _ -> failwithf "unable to set value"
          unbox<_> x

      let xpath_<'a> (path:string) : Prism<XElement, _> = 
          (fun x -> 
            match (x.XPathEvaluate(path) :?> IEnumerable).Cast<'a>() |> Seq.toList with
            | [] -> None
            | h :: _ -> Some h),
          (fun s x -> x)
      
      let xml_<'a> path = 
          let l : Lens<_,_> = (fun x -> getValue x), (fun s x -> setValue x s)
          xpath_<'a> path >?> l

      let xattr_ = xml_<XAttribute>

      let xelem_ =xml_<XElement>

      let csvValue_ (indx:int) (col:string) : Lens<CsvFile, _> =
          (fun x -> x.GetValue(col,indx)),
          (fun s x -> x.SetValue(col, indx, s); x)

let doc = XDocument.Load("code/data/lenses/source_data.xml")
let products = doc.XPathSelectElements("//catalog_item") |> Seq.toArray

let values = 
    [
       xattr_ "parent::product/@description"
       xelem_ "item_number"
       xelem_ "price"          
    ]

let readValues values doc = 
    List.map (fun (path:Prism<_,_>) -> Optic.get path doc) values

[ for product in products -> readValues values product ]

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

