(*** raw ***)
//---
//layout: page
//title: Enterprise Goggles.
//---

(*** hide ***)
module EnterpriseOptics =

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

type Csv private(headers : string[], ?data : string [] []) = 
     
     let headers = ResizeArray<string>(headers)
     let data = 
        match data with
        | Some data -> ResizeArray(data |> Array.map (ResizeArray<string>))
        | None -> ResizeArray()

     static member Empty = Csv([||], [||])

     static member Create(headers, ?data) = 
        Csv(headers, defaultArg data [||])

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
                row.Add(value) 
        | None -> 
            while (indx >= data.Count) do
                data.Add(ResizeArray<_>())

            x.SetValue(col, indx, value)
    
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

(**
## A typical enterprise problem

Based on a gut feel and no real evidence :) apart from my own experience I would say that a large proportion of enterprise applications are essentially just some sort of Extract - Transform - Load for system A to system B. 

For the sake of argument, lets say that the requirement for our application is to read data from a [XML file](/code/data/lenses/source_data.xml), apply some transformation (say convert the sizes to a standard size definition S, M, L, XL) and then write it out to another CSV file.

So we can start by defining to functions to convert the sizes

*)

let longSizeToShortSize = function 
    | "Small" -> "S"
    | "Medium" -> "M"
    | "Large" -> "L"
    | "Extra Large" -> "XL"
    | a -> a

let shortSizeToLongSize = function  
    | "S" -> "Small"
    | "M" -> "Medium"
    | "L" -> "Large"
    | "XL" -> "Extra Large"
    | a -> a

(**
Now this is a fairly trivial problem, to solve and there are many ways to do it, for example a one solution might be to use a type provider and a simple CSV File representation do something like the following.
*)

type Catalog = XmlProvider<"data/lenses/source_data.xml">

let transformWithoutLenses (path:string) =  
    let data = Catalog.Load(path)
    let data = 
        [|
            for product in data.Products do
                for catalogItem in product.CatalogItems do
                for size in catalogItem.Sizes do
                    yield [| catalogItem.ItemNumber 
                             product.Description 
                             catalogItem.Gender
                             size.Description
                             longSizeToShortSize size.Description
                             catalogItem.Price |> string
                          |]
        |]
    Csv.Create([|"item_no"; "description"; "gender"; "size"; "short_size"; "price"|],data)

(**
which gives us

    item_no,description,gender,size,short_size,price
    QWZ5671,FSharp Sweater,Men's,Medium,M,39.95
    QWZ5671,FSharp Sweater,Men's,Large,L,39.95
    RRX9856,FSharp Sweater,Women's,Small,S,42.50
    RRX9856,FSharp Sweater,Women's,Medium,M,42.50
    RRX9856,FSharp Sweater,Women's,Large,L,42.50
    RRX9856,FSharp Sweater,Women's,Extra Large,XL,42.50
    QWZ8976,FSharp T-Shirt,Men's,Medium,M,39.95
    QWZ8976,FSharp T-Shirt,Men's,Large,L,39.95
    RRX345,FSharp T-Shirt,Women's,Small,S,42.50
    RRX345,FSharp T-Shirt,Women's,Medium,M,42.50
    RRX345,FSharp T-Shirt,Women's,Large,L,42.50
    RRX345,FSharp T-Shirt,Women's,Extra Large,XL,42.50

this implementation is all well and good for a one off. It works and is maintainable, in the sense that the code is consise and easy to follow. However there are a few issues. Since this is the enterprise, everytime a business process changes or a external regulatory change happens it is likely that the data format is going to change. It would therefore be nice to be able to change the load formats without having to re-write/re-compile everything. Also some type providers don't deal well with data that isn't in the same format (missing nodes / properties) as the example additionally you because they are typically erased refelection is out of the question, so putting the property mapping in a configuration file, is made signifcantly more complicated. Now ofcourse we can always introduce some higher-order functions to abstract aways these problems, but this would be specific to this solution. It would be nice to try and solve this in a more general way.    

##Introducing Optics

At [F# exchange](https://skillsmatter.com/conferences/7145-f-exchange-2016#program), I got into a couple of conversations around enterprise development and the patterns that I use when developing line of business applications with F#. Typically the discussion focused on the lack of need for patterns. This is mainly due F#'s strong type system and the fact that higher order functions are a [pretty powerful abstraction](https://www.infoq.com/presentations/fp-design-patterns). However one topic that promoted a certain amount of contention was Lenses or more generally Optics.

Now I can't say I am surprised by this there are a couple of reasons why,

* *Performance* - Extra Allocations caused by individually setting properties on records can have a reasonable performace impact when use in time critical code.

* *No Language Support* - Writing lenses for records can be a large overhead and currently there is no language support. Maybe this will happen in the [future](https://fslang.uservoice.com/forums/245727-f-language/suggestions/6906132-implement-first-class-lensing-lenses-in-f) who knows??

With this considered thou, performance is often not so important in the enterprise and I will often favour maintainable and readable code over performant code. So what would a optic version of this solution look like. Well first we need to create our optics for xml and csv.      
*)

let xpath<'a> (path:string) : Lens<XElement, _> = 
    (fun x -> (x.XPathEvaluate(path) :?> IEnumerable).Cast<'a>().FirstOrDefault()),
    (fun s x -> x)

let xml<'a> defaultV path = 
    let getValue defaultV (x:obj) =
      match x with
      | null -> defaultV
      | :? XAttribute as a -> a.Value
      | :? XElement as e -> e.Value
      | _ -> failwithf "unable to get value on %O" x

    let setValue (x:obj) v = 
        match x with
        | :? XAttribute as a -> a.SetValue(v)
        | :? XElement as e -> e.SetValue(v)
        | _ -> failwithf "unable to set value %O" x
        unbox<_> x
    
    let l : Lens<_,_> = (fun x -> getValue defaultV x), (fun s x -> setValue x s)
    xpath<'a> path >-> l

let xattr = xml<XAttribute>

let xelem = xml<XElement> 

let csv indx (col:string) : Prism<Csv, _> =
    (fun x -> x.GetValue(col,indx)),
    (fun s x -> x.SetValue(col, indx, s); x)

(**

Here I am using [Aether](https://github.com/xyncro/aether) a lens library by Andrew Cherry and a simple (CSV file representation)[https://gist.github.com/colinbull/60797d5377be4d841f51e4f0776a24fa] but you could use anything. If you have never used Optics or just simply want a refresher then I might suggest the guide to Aether which can be found (here)[https://xyncro.tech/aether/guides/]. 

Now we have our optics we can define our mappings.
*)

let shortSize : Isomorphism<_,_> = 
    shortSizeToLongSize,
    longSizeToShortSize

let mappings indx =  
    [
       xelem "NULL" "parent::catalog_item/item_number", (csv indx "item_no")
       xattr "NULL" "ancestor::product/@description", (csv indx "description")
       xattr "NULL" "parent::catalog_item/@gender", (csv indx "gender")
       xattr "NULL" "@description", (csv indx "size")
       xattr "NULL" "@description", (csv indx "short_size" >?> shortSize)
       xelem "NULL" "parent::catalog_item/price", (csv indx "price")       
    ]

(**

Here we represent our mapping as a simple list of tuples the first item in the tuple is the optic we shall use to read the data, the second item is the optic we shall use to set the data, which can also be composed with any transformations we require. In our case we can create a isomorphism (a reversible function) that represents the size conversion then compose it with the `short_size` csv field. We now need to pull this all together, to do that we need a function that takes this mappings, the source data and the target and ties it all together. 
*)

let inline etl transform load extract = 
    let inline getSet (get,set) load source =
        let value = (Optic.get get source)
        Optic.set set value load

    extract()
    |> Seq.indexed
    |> Seq.fold (fun s (i, x) -> Seq.fold (fun s mapping -> getSet mapping s x) s (mappings i)) load


(**
A nasty as this function looks, it is quiet simple; and completly general. It simply iterates over the incoming data and applies an index to this data, this represents the row number. We then fold over this indexed data, with each fold we fold over each over the mappings, reading from the first optic supplied by the mappings list then sending the value to the second optic, because of the definition lenses of setting the value means it returns to object the value was applied to, which is then returned by the fold. 
*)

let extract() =
    let path = __SOURCE_DIRECTORY__ + "/data/lenses/source_data.xml"
    XDocument.Load(path).XPathSelectElements("//catalog_item/size")

etl mappings Csv.Empty extract

(**
This returns the same as out original solution which is good, 

    item_no,description,gender,size,short_size,price
    QWZ5671,FSharp Sweater,Men's,Medium,M,39.95
    QWZ5671,FSharp Sweater,Men's,Large,L,39.95
    RRX9856,FSharp Sweater,Women's,Small,S,42.50
    RRX9856,FSharp Sweater,Women's,Medium,M,42.50
    RRX9856,FSharp Sweater,Women's,Large,L,42.50
    RRX9856,FSharp Sweater,Women's,Extra Large,XL,42.50
    QWZ8976,FSharp T-Shirt,Men's,Medium,M,39.95
    QWZ8976,FSharp T-Shirt,Men's,Large,L,39.95
    RRX345,FSharp T-Shirt,Women's,Small,S,42.50
    RRX345,FSharp T-Shirt,Women's,Medium,M,42.50
    RRX345,FSharp T-Shirt,Women's,Large,L,42.50
    RRX345,FSharp T-Shirt,Women's,Extra Large,XL,42.50

except now the mapping is essentially just data, we can place this in some sort of configuration file so we can change the mapping at will, of course if we need to add more to our domain then we we are forced to re-compile, but all in all I think this is a nice general solution to a problem that occurs so often in the enterprise. 
*)
