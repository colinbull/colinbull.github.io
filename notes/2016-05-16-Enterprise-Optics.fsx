(*** raw ***)
//---
//layout: page
//title: Enterprise Goggles.
//---

(*** hide ***)


#r "../packages/Aether/lib/net35/Aether.dll"
#r "../packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#r "System.Xml.Linq.dll"
#r "System.Data.dll"

open System
open System.Linq
open System.Collections
open System.Collections.Generic
open System.Xml.Linq
open System.Xml.XPath
open Aether
open Aether.Operators
open FSharp.Data

module EnterpriseOptics =

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

    let run (path:string) =  
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

this implementation is all well and good for a one off. It works and is maintainable, in the sense that the code is consise and easy to follow. However there are a few issues. Since this is the enterprise, everytime a business process changes or a external regulatory change happens it is likely that the data format is going to change. It would therefore be nice to be able to change the format without having to re-write everything, in fact in enterprises I have seen release times beyond 6 weeks, so ideally we would like to push as much to scripts or configuation as we can. Also some type providers don't deal well with data that isn't in the same format (missing nodes / properties) as the example provided at compile time; Additionally because they are typically erased, reflection is out of the question, so putting the property mapping in a configuration file, is made significantly more complicated. Now of-course we can always introduce some higher-order functions to abstract aways these problems, but this would be specific to this solution. It would be nice to try and solve this in a more general way.    

##Introducing Optics

At [F# exchange](https://skillsmatter.com/conferences/7145-f-exchange-2016#program), I got into a couple of conversations around enterprise development and the patterns that I use when developing line of business applications with F#. Typically the discussion focused on the lack of need for patterns. This is mainly due F#'s strong type system and the fact that higher order functions are a [pretty powerful abstraction](https://www.infoq.com/presentations/fp-design-patterns). However one topic that promoted a certain amount of contention was Lenses or more generally Optics.

Now I can't say I am surprised by this there are a couple of reasons why,

* *Performance* - Extra Allocations caused by individually setting properties on records can have a reasonable performace impact when used in time critical code.

* *No Language Support* - Writing lenses for records can be a large overhead and currently there is no language support. Maybe this will happen in the [future](https://fslang.uservoice.com/forums/245727-f-language/suggestions/6906132-implement-first-class-lensing-lenses-in-f) who knows??

With this considered thou, performance is often not so important in the enterprise, and even maintainability is often sacrificed in the aim of making things flexible. Now there is probably an entire other blog post around why this is the case but thats for another day. So what does an optic solution look like to this problem. Well we can first start by defining some Optics over an XML structure accessed using XPATH and one over CSV.      
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
Here I am using [Aether](https://github.com/xyncro/aether) a lens library by Andrew Cherry and a simple (CSV file representation)[https://gist.github.com/colinbull/60797d5377be4d841f51e4f0776a24fa] but you could use anything. If you have never used Optics or just simply want a refresher then I might suggest the guide to Aether which can be found (here)[https://xyncro.tech/aether/guides/]. Also after you have read that guide you may notice that I'm not defining Optics per se, instead I'm defining functions that return optics a subtle but important distinction. 

Now we have our optics (sorry, functions) we can define our mappings. But first we need to add a little extra to our domain functions, so we can use them with Optics. In terms of transformations there are two three things we can compose with Optics. Optics, Isomorphisms (a reversible function e.g. `string -> char[]` and `char[] -> string`) and Epimorphisms (which is a weaker version of the Isomorphism in that the left hand conversion may fail e.g. `string -> char[] option` and `char[] -> string`). For our little problem we only need to concern ourselves with the former since we have a total function (since we include the unknown mappings). So we can define our isomorphism as follows. 
*)

    let shortSize : Isomorphism<_,_> = 
        shortSizeToLongSize,
        longSizeToShortSize

(**
Now we can represent our mappings as a simple list of tuples the first item in the tuple is the optic we shall use to read the data using XPATH, the second item is the optic we shall use to set the data (pop it into our CSV file representation).
*)

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
Now we have our mappings along with our transformations we need to actually be able to run this code. This is where things get a little bit nasty.
*)

    let inline etl transform load extract = 
        let inline getSet (get,set) load source =
            let value = (Optic.get get source)
            Optic.set set value load

        extract()
        |> Seq.indexed
        |> Seq.fold (fun loader (rowNumber, x) -> 
                        (transform rowNumber)
                        |> Seq.fold (fun loader mapping -> getSet mapping loader x) loader) load


(**
URRGH!! WTF! But as nasty as this function looks, it is actaully quiet simple; and completly general, and only needs to be written once. It simply iterates over the incoming data and applies an index to this data, this represents the row number. We then fold over this indexed data, with each fold we fold over each over the mappings, reading from the first optic supplied by the mappings list then sending the value to the second optic, because of the definition lenses of setting the value means it returns to object the value was applied to, which is then returned by the fold. 
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

and now the mapping is essentially just data. However it is still compiled, so all we have really bought ourselves is turning the mappings into a list. Or have we? Lets have a closer look.

When we defined our lenses and transformations we defined things in a very specific way. The reason for this is for composablility, the lens solution allows us to implement pretty much any mapping and transformation we want without ever changing the overall structure of the solution. If I wanted to for example write this to a .NET datatable rather than CSV file then I create a function that provides, getters and setters to an instance of a datatable replace that with the CSV ones. In addition the Optic over the datatable can then be put as a gist, shared assembly or however you use common code and re-used.

So, what do you think? Are optics worth it? In my opinion apart from the plumbing code `etl` function (which is written once) the rest of the code is very simple and could be read by anyone, even maybe the business; which at the end of the day is what all enterprise devs try to acheive as it makes, their lives that little bit easier :).

*Many YAK's have been shorn but none hurt*
*)
