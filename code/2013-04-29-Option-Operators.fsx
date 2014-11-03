---
layout: page
title: Option Operators
---
(*** hide ***)
#I "../packages"
open System

(**
We often have to represent the absence of data within a collection of values. In C# the default type to-go to is an Nullable, F# 3.0 introduced the Microsoft.FSharp.Linq.NullableOperators module to help with comparisons and arithmetic over this type. 

The Nullable type can still be somewhat tiresome to use in F# though as it can lead to adding lots of type constraints for example
*)


type NullableSeq<'a when 'a : (new : unit ->'a) 
                    and 'a : struct 
                    and 'a :> ValueType> = seq<Nullable<'a>>

(**
I think a nicer approach is to replace Nullable with Option, to relax some of these type constraints.
*)


type OptionSeq<'a> = seq<Option<'a>>

(**
however in doing this we have lost the nice operators that are available for the type `Nullable<'a>`. But this is fairly easy to recreate as the semantics of 
Nullable and Option are approximately the same. So with a quick copy of the NullableOperators module and some simple find and replace we have the exact same 
set of operators.
*)

module OptionOperators =

    let (?>=) (x : Option<'T>) (y: 'T) = x.IsSome && x.Value >= y

    let (?>) (x : Option<'T>) (y: 'T) = x.IsSome && x.Value > y

    let (?<=) (x : Option<'T>) (y: 'T) = x.IsSome && x.Value <= y

    let (?<) (x : Option<'T>) (y: 'T) = x.IsSome && x.Value < y

    let (?=) (x : Option<'T>) (y: 'T) = x.IsSome && x.Value = y

    let (?<>) (x : Option<'T>) (y: 'T) = not (x ?= y)

    let (>=?) (x : 'T) (y: Option<'T>) = y.IsSome && x >= y.Value

    let (>?) (x : 'T) (y: Option<'T>) = y.IsSome && x > y.Value

    let (<=?) (x : 'T) (y: Option<'T>) = y.IsSome && x <= y.Value

    let (<!--?) (x : 'T) (y: Option<'T>) = y.IsSome && x < y.Value

    let (=?) (x : 'T) (y: Option<'T>) = y.IsSome && x = y.Value

    let (<>?) (x : 'T) (y: Option<'T>) = not (x =? y)

    let (?>=?) (x : Option<'T>) (y: Option<'T>) = (x.IsSome && y.IsSome && x.Value >= y.Value)

    let (?>?) (x : Option<'T>) (y: Option<'T>) = (x.IsSome && y.IsSome && x.Value > y.Value)

    let (?<=?) (x : Option<'T>) (y: Option<'T>) = (x.IsSome && y.IsSome && x.Value <= y.Value)

    let (?<!--?) (x : Option<'T>) (y: Option<'T>) = (x.IsSome && y.IsSome && x.Value < y.Value)

    let (?=?) (x : Option<'T>) (y: Option<'T>) = (not x.IsSome && not y.IsSome) || (x.IsSome && y.IsSome && x.Value = y.Value)

    let (?<>?) (x : Option<'T>) (y: Option<'T>) = not (x ?=? y)

    let inline (?+) (x : Option<_>) y = if x.IsSome then Some(x.Value + y) else None
    let inline (+?) x (y: Option<_>) = if y.IsSome then Some(x + y.Value) else None
    let inline (?+?) (x : Option<_>) (y: Option<_>) = if x.IsSome && y.IsSome then Some(x.Value + y.Value) else None

    let inline (?-) (x : Option<_>) y = if x.IsSome then Some(x.Value - y) else None
    let inline (-?) x (y: Option<_>) = if y.IsSome then Some(x - y.Value) else None
    let inline (?-?) (x : Option<_>) (y: Option<_>) = if x.IsSome && y.IsSome then Some(x.Value - y.Value) else None

    let inline ( ?*  ) (x : Option<_>) y = if x.IsSome then Some(x.Value * y) else None
    let inline ( *?  ) x (y: Option<_>) = if y.IsSome then Some(x * y.Value) else None
    let inline ( ?*? ) (x : Option<_>) (y: Option<_>) = if x.IsSome && y.IsSome then Some(x.Value * y.Value) else None

    let inline ( ?%  ) (x : Option<_>) y = if x.IsSome then Some(x.Value % y) else None
    let inline ( %?  ) x (y: Option<_>) = if y.IsSome then Some(x % y.Value) else None
    let inline ( ?%? ) (x : Option<_>) (y: Option<_>) = if x.IsSome && y.IsSome then Some(x.Value % y.Value) else None

    let inline ( ?/  ) (x : Option<_>) y = if x.IsSome then Some(x.Value / y) else None
    let inline ( /?  ) x (y: Option<_>) = if y.IsSome then Some(x / y.Value) else None
    let inline ( ?/? ) (x : Option<_>) (y: Option<_>) = if x.IsSome && y.IsSome then Some(x.Value / y.Value) else None