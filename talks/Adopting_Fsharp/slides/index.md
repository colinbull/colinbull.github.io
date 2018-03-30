- title : Easing F# Adoption
- description : Easing F# Adoption.
- author : Colin Bull
- theme : league
- transition : default


***

# Easing F# Adoption

***

### The first problem

![the_enterprise](images/the_enterprise.png)

' Large enterprises 
' Not platforms
' Outsourcing adoption

***

### Enterprises aren't software houses

They don't claim to be best in market,
but they do recongise the value of software.

' Fall back to relying on buying in head count.
' this leads to another problem

*** 

### The Stack Overflow survey

![stackoverflow](images/stackoverflow_survey.png)

' Architects will apply the blindly
' To cover all basis

***

> Nobody got fired for hiring {insert-consultancy-here} 

' But you did end up with crap overspecced buggy system.

***

### The inconvenient truth

***

### C# Is really quiet good at this stuff

![deal_with_it](images/deal_With_it.jpg)

' Why should a dark matter dev care? 
' Will it help them get up the career ladder
' Investment in learning. 

***

## How do we deal with this? 

***

### Advertise F# features 

* Pattern matching / Active Patters 
* Type providers 
* Async 
* Immutability 
* etc.. 

' You might get a small response
' Most likely a shrug thou.
' They will still need to learn
' Focusing on FSharp.Core won't help either

*** 

### Don't try and introduce outside of Visual Studio

' This is important
' Sorry Krystof.. 

***

### Lower mismatch with C#

' We need to be able to write F# that isn't like a slap in the face  
' Even though the C# isn't 
' The rest of the talk will focus on this.

***

### Composing with let 

    let doStuff = f >> g

### vs 

    let doStuff f g a = 
        let y = f a
        g y

' Many newcomers aren't used to REPL lifestyle
' Still want to set breakpoints to debug

***

### Use operators wisely

    let inline (!>) (b : ^b) : ^a = 
        (^a : (static member op_Explicit : ^b -> ^a) (b)) 

    type Trade = 
        { Product:string; Volume:float }
        static member op_Explicit (product,volume) = 
            { Product = product; Volume = volume }

    let trade : Trade = !> ("power", 5000) 

###  vs 

    type Trade = 
        { Product:string; Volume:float }
        static member Create (product,volume) = 
            { Product = product; Volume = volume }

    let trade = Trade.Create ("power", 5000)

' contrived example but gets the point across
' I have seen this example many times 

***

### Oh yeah! Member constraints

    let inline (!>) (b : ^b) : ^a = 
        (^a : (static member op_Explicit : ^b -> ^a) (b))
    
' Cryptic syntax. 
' Rarely are actually required
' Often other ways to encode this Interfaces for example

***

### Prefer an interface to a record of functions 

    type ProductRepository = {
        Get : string -> Product 
        Save : Product -> unit 
    } 

### vs 

    type IProductRepository = 
        abstract Get : string -> Product 
        abstract Save : Product -> unit

' More familiar to C# devs
' Allows generic methods 
' Object expressions allow you to turn these into values

***

### Write interop interfaces in C#

* Carefully consider how you expose FSharp.Core
* Consider using `[<CompiledName>]` attribute
* Try to limit sharing to value types. 

' Some FSharp types are interesting to use from C#
' Provides a clear boundary
' Value types limit action at a distance issues
' Compiled Name to get naming conventions inline

***

### Utilise the .NET ecosystem 

* Don't worry to much about non-functional interfaces

' F# runs on the CLR utilise the libraries
' Can always wrap in a facade

***

### Structuring modules 

![microservice_db](images/microservice_db.png)
###### image - http://houseofbots.com/news-detail/519-1-monolithic-to-microservices-architecture-scalable-approach

' Consider modules like microservices
' n-tier seperate dataaccess and domain logic 

***

    module Trade = 

        let computeVolume trade = ...

    module DataAccess = 

        let saveTrade trade = ...

### vs 

    module Trade = 

        let computeVolume trade = ...

        let save trade = ...

' higher cohesion
' don't violate SRP in functions thou 
' similar considerations to microservices
' notcie in the picture stateful services

***

### As for the stateful ones

    module StatefulModule = 

        let private mutable state = 1

        let doStuff() = ..

### prefer objects 

    type StatefulObject(myState) = 

        let mutable state = myState

        member __.DoStuff() = ..
   
' When learning functional programming this seems like a paradox. 
' F# is a very good object oriented language use it
' If you have state, then an object is a good place to store it

***

### Consider dependencies 

    module Trade = 

        let update (eventHub:IEventHub option) f trade = ... 

### vs 

    type TradeService(eventHub:IEventHub) = 

        member x.UpdateTrade(trade:Trade, updateFunc) = 
            let newTrade = Trade.update updateFunc trade
            evntHub.raise (TradeUpdated(trade, newTrade))
            newTrade 

' Allows injection of cross cutting services
' Improves ability to add dependencies
' Allows you to use Trade.update without dragging in baggage.


*** 

### Select your abstractions wisely

![monad_moniod](images/monad_monoid.jpg)

' 20 mins to here

*** 

### These are all super powerful abstractions

' You definitely should learn and apprciate them
' but

*** 

![great_responsibility](images/with_great_power.png)

' No simple answers not going to scale
' Hence the number of monad tutorials
' At the begining of the talk I mentioned darket matter dev investment
' They aren't going to invest in this
' but these are useful

***

### Computation expressions to the rescue

    let attemptReadMapProduct row = 
        result { 
            let! marketArea = (read<String> "marketarea" row)
            let! product = (read "product" row) 
            return! tryMapProduct marketArea product
        }

### vs 

    let attemptReadMapProduct row = 
        read<String> "marketarea" row
        >>= (fun ma -> 
                read<String> "product" row
                >>= (fun prod -> tryMapProduct ma prod)
            )  

' provides a nice middle ground
' hides detail can still be utilised
' enables debugging
' also can be a little bit like a virus.
' end up infecting the whole call chain.

***

### But do you really need

    let foo = 
        asyncReaderStateResult { ... }

### or 

    let writeLine = 
        io { .. }

###### Probably not.

' First class support for I/O, Exceptions and mutation.
' These layer complexity 
' What happens if you get the implementation wrong.
' No one will thank you for that. 

*** 

### What happens if they go wrong?

' People abandon. Honestly.

*** 

### Limit to well defined abstractions 

* Maybe
* Result
* Async

' These are well understood 
' Although maybe and result builders aren't part of FSharp.Core 

*** 

### In Summary

* Except the type system for what it is  
* Except abstractions aren't for free.  
* Think! Will I understand this next year?

' Do you really need to create an encoding for higher kinded types? 
 

*** 

# Thanks for listening!!

### Questions?
