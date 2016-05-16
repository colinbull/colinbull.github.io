(*** raw ***)
---
layout: page
title: Enterprise Lenses.
---

(*** hide ***)

type Volume = {
    Unit : string
    Notional : decimal
}

type BuySell = | Buy | Sell

type Trade = {
    Id : int
    BuySell : BuySell
    Volume : Volume
}

(**

##Introduction

At [F# exchange](https://skillsmatter.com/conferences/7145-f-exchange-2016#program), I got into a couple of converstaions around enterprise development and the patterns that I use when developing line of business patterns with F#. Typically the discussion focused on the lack of patterns and this is mainly due F#'s strong type system and the fact that higher order functions are a [pretty powerful abstraction](https://www.infoq.com/presentations/fp-design-patterns). However one topic that promoteda certain amount of contention was Lenses.

Now I can't say I am surprised by this there are a couple of reasons why,

* *Performance* - Extra Allocations caused by individually setting properties on records can have a reasonable performace impact when use in time critical code.

* *No Language Support* - Writing lenses for records can be a large overhead and currently there is no language support. Maybe this will happen in the [future](https://fslang.uservoice.com/forums/245727-f-language/suggestions/6906132-implement-first-class-lensing-lenses-in-f) who knows??

With this stated thou, performance is often not so important in the enterprise and I will often favour maintainable and readable code over performant code.

## A typical enterprise problem

So based on a gut feel and no real quantitive evidence, :) I would say that a large proportion of enterprise applications are essentially just some sort of Extract - Transform - Load for system A to system B. Now if we look at this in the context of a single application we can reduce this to a simple function. 

      (unit -> 'b) -> ('b -> 'c) -> ('c -> unit)

For the sake of argument, lets say that the requirement for our application is to read data from a CSV file, apply some transformations and that write it out to another CSV file.

Now this is a fairly trivial problem, to solve and there are many ways to do it all fairly simple, for example a naive solution might be to do something like the following.
      
*)

