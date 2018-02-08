(*** raw ***)
---
layout: page
title: Html Provider + Canopy.
---

(*** hide ***)

#r "../packages/Selenium.WebDriver/lib/net40/WebDriver.dll"
#r "../packages/Selenium.Support/lib/net40/WebDriver.Support.dll"
#r "../packages/canopy/lib/canopy.dll"
#r "../packages/FSharp.Data/lib/net40/FSharp.Data.dll"

open canopy
open System
open System.IO
open FSharp.Data
open OpenQA.Selenium.Support.UI

(**
So recently this tweet came across my timeline. 

<blockquote class="twitter-tweet" data-lang="en"><p lang="en" dir="ltr">Why F# is the best language for screen scraping <a href="https://t.co/VinRTFApzI">https://t.co/VinRTFApzI</a>. Nice article</p>Don Syme (@dsyme) <a href="https://twitter.com/dsyme/status/803670647515873280">November 29, 2016</a></blockquote>
<script async src="//platform.twitter.com/widgets.js" charset="utf-8"></script>

and indeed the article is definiately worth a read. However I have recently been using both canopy and the HTML Provider together to extract auction price data from [http://www.nordpoolspot.com/Market-data1/N2EX/Auction-prices/UK/Hourly/?view=table](http://www.nordpoolspot.com/Market-data1/N2EX/Auction-prices/UK/Hourly/?view=table) and thought it might be worth sharing some of the code I have been using. Now the problem with just using the [HTML Provider](http://fsharp.github.io/FSharp.Data/) to scrape this page is that you actually need the javascript on the page to execute and the HTML provider doesn't do this. Maybe this is something worth adding??  

However using [canopy](https://lefthandedgoat.github.io/canopy/) with [phantomjs](http://phantomjs.org/) we can get the javascript to execute and the table generated in the resulting HTML and therefore availble to the HTML provider. So how do we do this. Well first of all we need to find out which elements we need write a function that uses canopy to execute the page, 
*)

let getN2EXPage phantomJsDir targetUrl units withSource = 
    phantomJSDir <- phantomJsDir
    start phantomJS
    url targetUrl
    waitForElement "#datatable"

    if not(String.IsNullOrWhiteSpace(units))
    then 
        let currencySelector = new SelectElement(element "#data-currency-select")
        currencySelector.SelectByText(units)
        let unitDisplay = (element "div .dashboard-table-unit")
        printfn "%A" unitDisplay.Text
        while not(unitDisplay.Text.Contains(units)) do
            printfn "%A" unitDisplay.Text
            sleep 0.5
        printfn "%A" unitDisplay.Text
    let source = withSource browser.PageSource
    quit()
    source

(**
with this function we can now do a couple of things. 

* Run a phantomjs headless browser. 
* Wait for the javascript to run on the page and render the `#datatable` element
* If we specify the currency to use then change the currency select element to that currency then wait again for the page to render.
* Finally pass the source of the page to a function for further processing. 

So with this we can now create a snapshot of the page and dump it to a file.
*)

let toolPath = 
    Path.GetFullPath(__SOURCE_DIRECTORY__ + "/libs/Tools/phantomjs/bin")

let writePage path content = 
    if File.Exists(path) then File.Delete path
    File.WriteAllText(path, content)

getN2EXPage toolPath "http://www.nordpoolspot.com/Market-data1/N2EX/Auction-prices/UK/Hourly/?view=table" "GBP" (writePage "code/data/n2ex_auction_prices.html")

(**
Once we have executed the above function we have a template file that we can use in the type provider to generate our type space. 
*)

type N2EX = HtmlProvider<"data/n2ex_auction_prices.html">

let getAuctionPriceData() = 
    let page = getN2EXPage toolPath "http://www.nordpoolspot.com/Market-data1/N2EX/Auction-prices/UK/Hourly/?view=table" "GBP" (fun data -> N2EX.Parse(data))
    page.Tables.Datatable.Rows

(**
 at this point we can use the HTML Provider as we normally would.
*)

let data = 
    getAuctionPriceData() 
    |> Seq.map (fun x -> x.``UK time``, x.``30-11-2016``)

(** 
Finally, I think it is worth noting that even though the the headers will change on the page; due to the fact that it is a rolling 9 day window. At runtime this code will carry on working as expected, because the code behind this will still be accessing the 1st and 3rd columns in the table, even though the headers have changed. However at compile time the code will fail :( because the headers and therefore the types have changed. However all is not lost, when this occurs, since the underlying type is erased to a tuple. So we could just do the following
*)
let dataAsTuple = 
    getAuctionPriceData() 
    |> Seq.map (fun x -> 
        let (ukTime, _, firstData,_,_,_,_,_,_,_) = x |> box |> unbox<string * string * string * string * string * string * string * string * string * string>
        ukTime, firstData
    )

(**
A little verbose but, hey it's another option...
*)