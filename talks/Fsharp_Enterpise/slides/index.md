- title : Enterprise F#
- description : Introduction to F# in the Enterprise 
- author : Colin Bull
- theme : league
- transition : default

***

### About Me 

- Physics
- Software consultant
- Commodity Trading.
- Commercial F# since ~2008
- Open Source contributor
        - SqlProvider
        - HtmlProvider (FSharp.Data)
        - Fake
        - Plus a few others
- Twitter: @colinbul

***

### Enterpise - definition

![enterprise_heirarcy](images/enterprise_heirarcy.jpg)

' $1bn+
' Lots of talking few doing

***

### Enterprise - Software 

- Opaque Initial Requirements
- Moving Targets
- Pass the buck

' Necessary evil, not part of core business
' Easily dismissed - always blamed.
' Agile can be good but often it is bad, 
' this means your dev approach has to handle this. 
' the language / toolset must make a difference.

***

### So why am I talking about this

- U.K Power scheduling system
- U.K Gas pipeline management system
- Ancillary Service Contract Management
- Real-time Market Data interface

' These are all large projects
' Some reasonably important
' All successful

***

### U.K Power Scheduling system

- C# desktop appliction with embedded F# libraries 
- C# interface defines interop layer for F#
- 0 bugs in production

' Early F# deployment to production (April 2010)
' Right click add F#

***

### Gas pipeline management system

- Complete F# Silverlight application
- Rewritten from exsiting C# application + Spreadsheet
- Found previously unknown significant financial error via Units of Measure

' Rewritten due to big problems with calculation engine in C#
' After deciphering logic in Spreadsheet and typing with Units of measure price had (£^2 / th) fsharp compiler told me so..
' Actually tried TDD on this, but gave up and went to the REPL test are there fro regressions thou
' No bugs after go live in calculation engine, a few UI issues thou.

***

### Contract Management System

- Full F# web application 
- Rewrite from long running (failing) C# application
- http://simontylercousins.net/does-the-language-you-use-make-a-difference-revisited

' Can fit the F# entire solution in the blank lines of the C#
' Can process 24 hours of information in less time than the previous solution took to process a minute
' Time to market (3 devs max ~10 months, C# ~5 years, 8 devs, never finished)
' Little hindsight as no intersection between devs
' Actors provide rate limiting for job requests
' Async everywhere
' Immutability everywhere including at the DB.
' Easy to change as contracts change follow the trail of compiler errors

***

### So how does F# help?

***

### Simple core language

- #light syntax
- Easily picked up by non-professional programmers

' More features often detriments syntax.
' Scala - all things to all people (mixins, type classes..)
' C# - heading similar direction to scala
' let / let rec - delimting cycles.

***

### Strong type system - Unions, UoM

    let fuelCost (heatRate:float<GJ>) (fuelMix:seq<float*Fuel>) =
        let computeFuelCost fuel =
            match fuel with
            | Gas(gas) -> 
                let fuelCost = 
                    ((gas.Price + gas.ExitFee) * gas.GrossToNetConversionRate)
                    |> Therm.toGigajouleRate 
                heatRate * fuelCost
            | Oil(oil) -> heatRate * oil.Price
            | Coal(coal) -> heatRate * coal.Price
         fuelMix
         |> Seq.fold (fun total (factor,fuel) -> 
                   total + (factor * (computeFuelCost fuel))
         ) 0.<``£``>

' Does everyone understand this? Explain?
' Making the compiler do the hard work.
' DU exhustive matches, new fuel = warning
' Uom - Encourages use of SI units, which can simpify things greatly. 
' Expressive - Can give this to a domain expert to verify.

***

### Active Patterns

    let (|PARTICIPANT_UMM|TSO_UMM|INVALID|) (element : XElement) = 
	    match element with
        | ROOT "participant_umm" 
            (ELEMENTS 
             (ELEMENT "affected_fuels" fuels & 
              ELEMENT "affected_units" units & 
              ELEMENT "effect_during" effect_during & 
              ELEMENT "effect_installed" & 
              ELEMENT "prodcons" & 
              ELEMENT "station" station & 
              UMM umm)) -> PARTICIPANT_UMM({ .. set record fields .. })
        | ROOT "tso_umm"  
            (ELEMENTS 
             (ELEMENT "capacity_a_b_during" ab_during & 
              ELEMENT "capacity_a_b_installed" ab_installed & 
              ELEMENT "capacity_b_a_during" ba_during & 
              ELEMENT "capacity_b_a_installed" ba_installed & 
              ELEMENT "line" line &  
              UMM umm)) -> TSO_UMM({ .. set record fields .. })
        | _ -> INVALID

' Parsing elements out of a 
' Awesome for parsing / transformations
' Of course could replace this with a TP now.


***

### Computation Expressions

    type DB = SqlProvider<..>
		
    let trades ctx = 
        query { for trade in ctx.Trades .. } 
	
    let mapToCanonicalForm trade = ...
	
    let loadCosts sourceConnection targetConnection = etl {
        let! source = resolveConnectionString sourceConnection
        let! target = resolveConnectionString targetConnection
        nonQuery (sql connection "DELETE FROM trade_data")
        query (trades DB.GetContext(source))
        transform (Seq.filter mapToCanonicalForm)
        bulkLoad target "trade_data"
    }
 
' Async, Maybe, Result etc.. 
' Allows expressive dsl's
' More than monads..
' CustomOperations

***
### There is more

- Pattern Matching 
- Immutability
- Type Providers
- Mailbox Processors
- Type Inference
- Structural Equality / Comparison
- Partial Application

' And probably more.. 
' These are all language features, but there are macro features also 
' Scott Wlaschin has a good list. 

***

### REPL, REPL, REPL

' Iteratively develop solutions 

***

### Consistent project structure

*Bottom up file ordering (Single pass compilation)*

![alphabetical_wtf](images/alphabetical_wtf.jpg)

' Consistent place to start when I open new project.
' The project explorer is telling me something more about my project
' I actually miss this on large solutions when using emacs. 
' This is probably the biggest win for the enterprise.

***

### Easy to refactor

**No tools required just a keyboard with TAB, space, Ctrl, c, v**

' Mention NTI. 
' And maybe an editor of some sort. 

***

### Other observations

**YMMV**

' but YMMV with these but somethings I have observed

***

### Uniformly structured code. 

    [<AutoOpen>]
    module DomainTypes = 
    
        type A = .....
        
    
    module A = 
    
        let validate data = ....
        
        let map data = .....


' Without a conversation our team kinda converged on this.
' Not sure whether we all read the same blogs or what.

*** 

### Seperation of IO and computation

- Find / draw a graph.. typical IO / computation in enterprise vs
  typical IO / computation in F#

' Important because this is a seperation of concerns
' Interlaced IO and Computation hard to reason about (Perf, Errors)
' Can structure a C# application like this but I haven't ever seen one
in a enterprise.

***


### Very little magic. 

- No Containers (Except when frmeworks force it)
- No Action at a distance. 
- Far simpler dependencies

' Web API
' Pure functions - IO pushed to the edges
' Type system provides a nice balance between complexity and expressivity.












 







