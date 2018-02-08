(*** raw ***)
// ---
// layout: page
// title: Lazy Elmish React.
// ---

(*** hide ***)

#r "../packages/Fable.Core/lib/netstandard1.6/Fable.Core.dll"
#r "../packages/Fable.Import.Browser/lib/netstandard1.6/Fable.Import.Browser.dll"
#r "../packages/Fable.React/lib/netstandard1.6/Fable.React"
#r "../packages/Fable.Elmish/lib/netstandard1.6/Fable.Elmish.dll"
#r "../packages/Fable.Elmish.Browser/lib/netstandard1.6/Fable.Elmish.Browser.dll"
#r "../packages/Fable.Elmish.React/lib/netstandard1.6/Fable.Elmish.React.dll"

open Fable.Helpers.React.Props
open Fable.Core.JsInterop
open Elmish.React.Common

module R = Fable.Helpers.React

type Msg = 
    | SaveChanges
    | PeriodValueChanged of obj * string

type Model = { 
    PendingChanged : obj[]
    Data : obj[]
    Columns : obj[]
    Entity : string
}

type CellEditFactory = CellEditFactory of obj

type TableProps = 
    | KeyField of string 
    | CellEdit of CellEditFactory
    | Data of obj[]
    | Columns of obj[]
    interface IProp

let bootstrapTable (props:IProp list) = Unchecked.defaultof<_>

(*
Recently I have been doing a fair amount with Fable-React-Elmish, and I have had really good results. 
Quick time to production, little or no bugs. Overal I have been very happy with the toolchain. Now admittedly the apps 
I have been producing are small, boring line of business apps. Basically just utilities or forms that essentailly are nothing 
more than data entry. However the latest application I have had to produce was abit more involved. Basically there is a simple form
that has to query a API and then dispaly the data in a table with various levels of grouping. Armed with my recent successful experiences
with the Fable-Elmish-React toolset I was reasonably confident I could have something running in a few days. Which I did, and
I was pleased with my self. 

UNTIL... 

I started testing with more realistically sized datasets. And then I found out it was slow, like, really, really, slow. Over a second to render
a single key press. This was somewhat a surprise as everything you read about React says how blazing fast the Virtual DOM and rendering are.

In it's first cut I had a view which looked something like the following. 

*)

let view model dispatch = 
    R.div [ClassName "col-md-12"] [
        if model.Data.Length > 0 
        then
            yield R.form [ClassName "form form-inline col-md-12"; Style [PaddingBottom "10px"]] [
                        R.button [ClassName "btn btn-default col-md-1"; 
                                   Disabled (model.PendingChanged.Length = 0);
                                   OnClick (fun x -> x.preventDefault();  dispatch SaveChanges)
                                 ] [unbox "Save Changes"]

                        R.input [ Id "selectedentity"; 
                                  ClassName "form-control"; 
                                  Style[MarginLeft "20px"]; 
                                  Placeholder "Select Entity"; 
                                  Value model.Entity]
                ]
       
        yield R.div [ClassName "col-md-12 table-responsive"] [
                   if model.Data.Length > 0 
                   then
                      yield bootstrapTable [
                          KeyField "id"
                          Data model.Data
                          Columns model.Columns
                          CellEdit (CellEditFactory(
                            [
                              "mode" ==> "click"
                              "afterSaveCell" ==> ignore
                            ] |> createObj))
                      ]
                   else 
                     yield R.h1 [Style [BackgroundColor "#eeeeee"; TextAlign "center"; Height "100%"]] [R.str "No Data!"]
              ]
    ]

(*
    However as innocent as this looks it is going to cause react some problems. Why, well the table in this case is going to contain
52 x 31 (1612) elements. That is quiet a lot of DOM elements, and the problem in this instance is that this will get rendered on every single pass. 
So how do we go about solving this. Well.. 

TL;DR RTFM - There is a description of this exact issue on the [Elmish.React github page](https://fable-elmish.github.io/react/#Lazy-views). 

>By default, every time the main update function is called (upon receiving and processing a message), the entire DOM is constructed anew and passed to React for >reconciliation. If there are no changes in the model of some component, its view function will under normal circumstances not return a different result. React >will then still perform reconciliation and realize that there is no need to update the component's UI. Consequently, when the DOM is sufficiently large or its >construction extremely time-consuming, this unnecessary work may have noticeable repercussions in terms of application performance. Thanks to lazy views however, >the update process can be optimized by avoiding DOM reconciliation and construction steps, but only if the model remains unchanged. 

So lazy views are the answer. Elmish-React provides several [lazy view functions](https://fable-elmish.github.io/react/common.html). The varients without the `With` suffix require types with equality constraint. This means that if any property on the model changes then the DOM will be updated. This is often not the behaviour you desire, since it is unlikely you will interact with your UI without changing your model. To this end you have two choices, create a lazyView component and pass the specific property or properties (as a tuple) from the model you are interested in. Or use `lazyViewWith` which allows us to specify the predicate that decides when we should update the containing DOM elements. In this example we'll use the latter.   
*)

let viewLazy model dispatch = 
    R.div [ClassName "col-md-12"] [
        if model.Data.Length > 0 
        then
            yield R.form [ClassName "form form-inline col-md-12"; Style [PaddingBottom "10px"]] [
                        R.button [ClassName "btn btn-default col-md-1"; 
                                   Disabled (model.PendingChanged.Length = 0);
                                   OnClick (fun x -> x.preventDefault();  dispatch SaveChanges)
                                 ] [unbox "Save Changes"]

                        R.input [ Id "selectedentity"; 
                                  ClassName "form-control"; 
                                  Style[MarginLeft "20px"]; 
                                  Placeholder "Select Entity"; 
                                  Value model.Entity]
                ]
       
        yield lazyViewWith
                 (fun oldM newM -> oldM.Entity = newM.Entity && oldM.Data = newM.Data) 
                 (fun model -> 
                     R.div [ClassName "col-md-12 table-responsive"] [
                           if model.Data.Length > 0 
                           then
                              yield bootstrapTable [
                                  KeyField "id"
                                  Data model.Data
                                  Columns model.Columns
                                  CellEdit (CellEditFactory(
                                    [
                                      "mode" ==> "click"
                                      "afterSaveCell" ==> ignore
                                    ] |> createObj))
                              ]
                           else 
                             yield R.h1 [Style [BackgroundColor "#eeeeee"; TextAlign "center"; Height "100%"]] [R.str "No Data!"]
                      ]
                 ) model
    ]

(*
Simples. Basically we have just wrapped the `div` that contains the table with the `lazyViewWith` function, and specified the predicate that helps it decide
when to update. 

With this in place, the responsiveness of the UI returned and I could breath again. I have to admit it took me way too long to figure this out considering 
it is actually written on the Elmish React project home page. However on the brightside I learnt a lot about React performance analysis, mainly from [this post](https://marmelab.com/blog/2017/02/06/react-is-slow-react-is-fast.html) I suggest, you read this as I found it quiet useful as well as the links at the bottom.    
*)