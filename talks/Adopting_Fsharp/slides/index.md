- title : Easing F# Adoption
- description : Easing F# Adoption.
- author : Colin Bull
- theme : league
- transition : default


***

# Easing F# Adoption.

***

## Avoid symbolics

    let buildDir = 
        !!("some" @@ "dir")
            ++ ("another" @@ "dir")
            -- "**/*.obj"  

' example from FAKE
' scares people off

***

## Composing with let 

    let doStuff f g a = 
        let y = f a
        g y

### vs 

    let doStuff = f >> g


' Many newcomers aren't used to REPL lifestyle
' Still want to set breakpoints to debug

***


## Don't be afraid of objects

    type StatefulObject(myState) = 

        let mutable state = myState

        member __.DoStuff() = .. 

### vs 

    module StatefulModule = 

        let private mutable state = 1

        let doStuff() = ..   

' F# is a very good object oriented language use it
' If you have state, then an object is a good place to store it
' When learning functional programming this seems like a paradox. 

***

## Structuring modules 
 
    module Trade = 

        let computeVolume trade = ...

        let save trade = ...

### vs 

    module Trade = 

        let computeVolume trade = ...

    module DataAccess = 

        let saveTrade trade = ...

' simpler navigation 
' similar considerations to service boundaries 
' YMMV on this judgement is critical

***

## Constructor injection 

    type TradeService(eventHub:IEventHub) = 

        member x.UpdateTrade(trade:Trade, updateFunc) = 
            let newTrade = Trade.update updateFunc trade
            evntHub.raise (TradeUpdated(trade, newTrade))
            newTrade 

### vs 

    module Trade = 

        let update (eventHub:IEventHub option) f trade = ... 


' Allows injection of cross cutting services
' Provides ability to update a trade with or without event dependency
' Easier testing

***

## Avoid member constraints

    let inline unit< ^S when ^S : 
        (static member Unit : ^S)> () : ^S =
    (^S : (static member Unit : ^S) ()) 

##### http://tomasp.net/blog/2014/update-monads/
##### Sorry Tomas

' Rarely are actually required
' Often other ways to encode this 

***

## Select your abstractions wisely

* Applicatives and Monads are super powerful abstractions
* But maybe limit the exposure
* `Maybe`, `Result` are useful in the domain. 
* But we have first class support for I/O, Exceptions and state as much as people hate this
* Do you really need `AsyncReaderStateResult` ?? 
* Also can be a bit like a virus 
* ONce you start using them everything ends up getting wrapped in them
* That being said computation expressions provide a nice middle ground.
* Make sure semantics are solid and well understood thou.

***

## Write interop interfaces in C#

* Hides the noise of F# types `FSharpOption<_>`
* Try to limit sharing to value types. 

' Some FSharp types are interesting to use from C#
' Provides a clear boundary
' Value types limit action at a distance issues  

***

## Consider array as the default data structure

* Familiar 
* Understood by the majority of devs


' If possible of course.
' Air of familiarity 


