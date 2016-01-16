- title : Enterprise F#
- description : Introduction to F# in the Enterprise 
- author : Colin Bull
- theme : league
- transition : default

***

### About Me 

- Physics
- Software consultant
- Typically worked in Commodity Trading E.On, RWE, Petroineos..
- Commercial F# since ~2008
- Open Source contributor
        - SqlProvider
        - HtmlProvider (FSharp.Data)
        - Fake
        - Plus a few others
- Twitter: @colinbul

***

### A Few F# Projects

- U.K Power scheduling system
- U.K Gas pipeline management system
- Ancillary Service Contract Management
- Real-time Market Data interface

***

### U.K Power Scheduling system

- C# desktop appliction with embedded F# libraries 
- C# interface defines interop layer for F#
- 0 bugs in production

**Key F# Features**
> REPL, Type System (UoM, DU's), Immutability 

' Two calculation problems Balancing / P.N following
' First F# deployment (April 2010)
' Right click add F#
' Describe how interfacing works C# inputs -> cast to F# domain 
' (REPL) Quick feedback
' (Type System) Correctness, Tame Complex domain
' (Immutability) Parallelism

***

### U.K Gas pipeline management system

- Complete F# Silverlight application
- Rewritten from exsiting C# application + Spreadsheet
- Found previously unknown significant financial error via Units of Measure

**Key F# Features**
> REPL, Type System, First Class Events, Async

' Rewritten due to big problems with calculation engine in C#
' After deciphering logic in Spreadsheet and typing with Units of measure price had (£^2 / th) fsharp compiler told me so..
' Actually tried TDD on this, but gave up and went to the REPL test are there fro regressions thou
' No bugs after go live in calculation engine, a few UI issues thou (I'll admit I not the greatest UX guy)

***

### Ancillary Service Contract Management System

- Full F# web application 
- Rewrite from long running (failing) C# application
- http://simontylercousins.net/does-the-language-you-use-make-a-difference-revisited

**Key F# Features**
> REPL, Actors / Async, Type System, Type Providers

' Can fit the F# entire solution in the blank lines of the C#
' Can process 24 hours of information in less time than the previous solution took to process a minute
' Time to market (3 devs max ~10 months, C# ~5 years, 8 devs, never finished)
' Little hindsight as no intersection between devs
' Actors provide rate limiting for job requests
' Async everywhere
' Immutability everywhere including at the DB.
' Easy to change as contracts change follow the trail of compiler errors

***

### Market Data Interface System

- Full F# application
- Lots of data feeds (~500)
- Transient data feed
- Unique parsing problems (images, custom data formats, web scraping)
- User defined models and views over data

**Key F# Features**
> REPL, Actors / Async, Immutability, Type Providers, Active Patterns, Type System

' Had to be robust, as some data feeds where transient and if we missed the data it was for good
' Had to detect direction & position of arrows
' Active patterns heavily used to parse custom data formats
' People don't respect standards.
' Web scraping initial implementation Html parser
' Actors for rate limiting to prevent API bans, isolating state in computation graph.
' Immutability provided almost carefree concurrency. (mention mutable types in immutable objects, mistake!!!)

***

### Common Themes

- Lack of bugs
- Quick time to market
- Easy to change 

' Very few bug in production
' A few F# devs go a long way
' Archtiecture typically fell out throu playing in the REPL
' Minimal commitment to design (mention mistake in NTI, how this help w.r.t BA and Business reqs)
' Refactoring is simple (just move functions around)

***

### So what does a F# application look like?

***

### Build

- F#

' Fake + paket

***

### Data Access

- F#

' SqlProvider + JsonProvider

***

### Services

- F#

' WebApi (more recently Suave)

***

### UI

- F#

' XamlProvider, Observable
' First class events
' FunScript, WebSharper

***

### Deployment

- F#

' FSX as deployment scripts
' Can run standalone
' Can run via FAKE.

***

### Documentation

- F# + Markdown

' FSharp.Formatting, FsReveal


***

### But whats different?

- After all it is still a .NET language.

' We probably could have achieved that in C# couldnt we?
' Maybe but F# functional

***

### No Containers
    
    [lang=fs]
    
    type LogEntry = | Info of string | Error of string * exn option
    type Context = { Log : LogEntry -> unit }
    
    let complexBusinessLogic context predicate data = 
        match predicate(data) with
        | [] ->  context.Log (Error ("No data found", None))
        | data -> (* . dosomethingWithIt . *)

' partial application && higher order funcs  thanks
' need context add it as a parameter
' no action at a distance.
' only time is when something forces it on you (ehm, Web API)

***

### No Frameworks

    [lang=fs]
    
    let process context dataAccess processContract resultHandler =
        asyncResult {
            use conn = context.GetConnection("ContractDb")
            let! data = Result.protect (dataAccess conn)
            let! result = processContract data
            return! resultHandler result
        }

' composition
' reuse
' Small sets of the correct combinators go a long way.

***

### Simple core language

' More features often detriments syntax.
' Scala - all things to all people (mixins, type classes..)
' C# - heading similar direction to scala

***

### Easy to get devs up to speed

- pic representing trying to find my way around a C# project compared
  to F#

' project file ordering (new devs see this as a problem)
' small composable functions
' true single responsability
' one language

***
 







