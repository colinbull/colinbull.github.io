### Implementation

**C# interface**
	 
	 [lang=cs]
	 pubic interface IPNFollowing
     {
         BidOffer[] ComputeBidOffers(double currentOutput, Bmu unit);
         //other domain actions
     }
	 
**F# implementation**
     
	 let bidOfferEngine = 
       { new IBidOfferEngine with
            member x.ComputeBidOffers(currentOutput, bmu) =
                 let bmu = mapBmu currentOutput bmu
                 match computeBidOffers bmu with
                 | Success x -> x
                 | Failure err -> log err; raise(BidOfferComputation err)
       }
	 

' Describe how interfacing works C# inputs -> cast to F# domain 
' Do not return any F# specific types. (Note; exception)

***

	module Trade =
		module Web =
			let app = 
				choose [
					GET >=> pathScan "/trades/portfolio/%d" getTradesByPortfolio >=> asJson
					GET >=> pathScan "/trade/%d" getTrade >=> asJson
				]
		
	
	let app = 
		choose [
			Trade.Web.app
			Position.Web.app
		]
	
	let main() =
		startWebServer ..



### Definition: Enterpise

- Pic of diamond showing managers v people people who talk about doing
  things v people who actually do things

' $1bn+
' Lots of talking few doing
' Opaque requirements

***

### Enterprises don't care about software development

' they don't care about your build
' they don't care that you wrote ActivePattern X 
' you won't receive an accurate spec well an in date spec / story. 
' this means your dev approach has to handle this.  
' F# helps.

***

### So why am I talking about this

- U.K Power scheduling system
- U.K Gas pipeline management system
- Ancillary Service Contract Management
- Real-time Market Data interface

' These are all large projects
' Some reasonably important

***

### U.K Power Scheduling system

- C# desktop appliction with embedded F# libraries 
- C# interface defines interop layer for F#
- 0 bugs in production

**Key F# Features**
> REPL, Type System, Immutability 

' Early F# deployment to production (April 2010)
' Right click add F#

***

### Gas pipeline management system

- Complete F# Silverlight application
- Rewritten from exsiting C# application + Spreadsheet
- Found previously unknown significant financial error via Units of Measure

**Key F# Features**
> REPL, Type System, First Class Events, Async

' Rewritten due to big problems with calculation engine in C#
' After deciphering logic in Spreadsheet and typing with Units of measure price had (£^2 / th) fsharp compiler told me so..
' Actually tried TDD on this, but gave up and went to the REPL test are there fro regressions thou
' No bugs after go live in calculation engine, a few UI issues thou.

***

### Contract Management System

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

- Lots of data feeds (~500) some transient
- Lots of parsing (images, custom data formats, web scraping)
- Rate limiting / Concurrency
- User defined models and views over data

**Key F# Features**
> REPL, Actors / Async, Immutability, Type Providers, Active Patterns, Type System

' Had to be robust, as some data feeds where transientd
' Had to detect direction & position of arrows
' Ease of refactoring mention the mistake. 
' Active patterns heavily used to parse custom data formats
' Web scraping initial implementation Html parser
' Actors for rate limiting to prevent API bans, isolating state in computation graph.
' Immutability provided almost carefree concurrency. (mention mutable types in immutable objects, mistake!!!)
' Biggest thing learnt - people don't respect standards.

***

### How did F# help?

- example of simple internal DSL's

' Other than features mentioned above. 
'Internal dsl cut copy code give it to user they think it is psuedo
code. 
' Really easy to refactor

***

### What does an Enterprise F# application look like?

' Same as any application, Build, Data, Services, UI

***

### Build

*F# (FAKE + Paket + ProjectScaffold)*

' Nice build DSL.
' Lots of helper functions
' Have one build to rule them all
' See steffans talk

***

### Data Access

*F# (TypeProviders or just ADO.NET)*
	
	type DB = SqlProvider<..>
		
	let getCosts = query { for trade in ctx.Trades .. } 
	
	let filterNxoMtm trade = 
		trade.MTM.IsSome
	
	let loadCosts connection = etl {
		nonQuery (sql connection "DELETE FROM cost_base")
		query (getCosts DB.GetContext(connection.ConnectionString))
		transform (Seq.filter filterNoMtm)
		bulkLoad targetConnection "cost_base"
	}

' Most enterprise work is shoveling data from system A to B
' DSL's can simplfiy this.. computation expression are really
powerful..  

***

### Services

*F# (Suave / Web API)*
	
' Prefer Suave at this point. 
' Nice and composable easy to extend
' But can be a bit of a jump for new devs.
' Web API interop, show reverse compat.

***

### UI

*F#, Javascript*

' Errm! try and leave that to someone else 
' XamlProvider
' First class events
' Observable combinators
' FunScript, WebSharper


***

### Deployment

*F# (FAKE as a scripting engine)*

' FSX as deployment scripts
' Can run standalone
' Can run via FAKE.
' Can run via octopus deploy.

***

### Documentation

*F# (FSharp.Formatting / FsReveal) + Markdown*

' FSharp.Formatting, FsReveal

***

### But whats different?

*After all it is still a .NET language.*

' We probably could have achieved that in C# couldnt we?
' Well / Yes it terms on pure technical level, but practically C#
isn't a scripting language. Make deployment etc. hard. 

***

### Simple core language

*#light syntax*

' More features often detriments syntax.
' Scala - all things to all people (mixins, type classes..)
' C# - heading similar direction to scala
' let / let rec - delimting cycles.
' doesn't scarifice readability.

***

### Consistent project structure

*Bottom up file ordering (Single pass compilation)*

- pic representing trying to find my way around a C# project compared
  to F#

' Things aren't ordered alphabetically (WTF!)
' This is probably the biggest win for the enterprise
' Also we use projectscaffold consistent solution structures

***

### Consistent file structure

    [lang=fs]
    
    [<AutoOpen>]
    module DomainTypes = 
    
        type A = .....
        
    
    module A = 
    
        let validate data = ....
        
        let map data = .....


' This one isn't forced, but you will probably fall onto something like this naturally given time. 

***

### No action at a distance

    [lang=fs]    
    let process context readContract validateContract  applyContract =
        use conn = context.GetConnection("ContractDb")
        readContract conn
        |> validateContract
        |> applyContract


' Major bug squasher (immutability FTW).
' partial application && higher order funcs thanks
' need context add it as a parameter
' strong return type- result type etc.. 
' results in no containers, only time is when something forces it on you (ehm, Web API)

***

### Seperation of IO and computation

- Find / draw a graph.. typical IO / computation in enterprise vs
  typical IO / computation in F#

' Important because this is a seperation of concerns
' Interlaced IO and Computation hard to reason about (Perf, Errors)
' Can structure a C# application like this but I haven't ever seen one
in a enterprise.

***

### Libraries not Frameworks 

* composition
* reuse
* Small sets of the correct combinators in your domain go a long way.
* Reduced dependency cycles

'At this point we have a common repository but I rarely ref dll. Use
file instead

***

### Whats the same? 

***

### Access to the whole .NET ecosystem. 

' BCL, Nuget
' Interop is pretty seamless. 

***

### It won't save you from a bad 3rd party API

*But you can limit the damage*

' Wrap the API (result types / Options)

***

### And all this means

- Lack of bugs
- Quick time to market
- Easy to change / maintain
- Familiar libaries

' Very few bug in production
' A few F# devs go a long way
' Archtiecture typically fell out throu playing in the REPL
' Minimal commitment to design (mention mistake in NTI, how this help w.r.t BA and Business reqs)
' Refactoring is simple (just move functions around)
' Maximised reuse.

*** 

### Great so how do I introduce F# 

* FAKE -> PAKET -> (C# Interface -> F# Lib) -> Full F# Application *

' Build is a familar problem
' PAKET solves lots of familiar problems. 
' C# interface breaks the ice. 
' Finally full F# 

***

### Thanks for listening

**Questions?**
