(*** raw ***)
---
layout: page
title: Handling constant type unsupported error for type providers
---

(*** hide ***)
#I "libs"
#load "ProvidedTypes.fsi"
#load "ProvidedTypes.fs"

open System
open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Quotations

type SomeRecord = { 
    Name : string
    DateTime : DateTime
}

type SomeType() =
    member x.SomeMethod(record:SomeRecord) = ()

(*** define:helper-defn ***)
module QuotationHelpers = 

    open Microsoft.FSharp.Quotations
    open Microsoft.FSharp.Reflection

    let rec coerceValues fieldTypeLookup fields = 
        Array.mapi (fun i v ->
                let expr = 
                    if v = null then simpleTypeExpr v
                    elif FSharpType.IsUnion (v.GetType()) then unionExpr v |> snd
                    elif FSharpType.IsRecord (v.GetType()) then recordExpr v |> snd
                    else simpleTypeExpr v
                Expr.Coerce(expr, fieldTypeLookup i)
        ) fields |> List.ofArray
    
    and simpleTypeExpr instance = Expr.Value(instance)

    and unionExpr instance = 
        let caseInfo, fields = FSharpValue.GetUnionFields(instance, instance.GetType())    
        let fieldInfo = caseInfo.GetFields()
        let fieldTypeLookup indx = fieldInfo.[indx].PropertyType
        caseInfo.DeclaringType, Expr.NewUnionCase(caseInfo, coerceValues fieldTypeLookup fields)

    and recordExpr instance = 
        let tpy = instance.GetType()
        let fields = FSharpValue.GetRecordFields(instance)
        let fieldInfo = FSharpType.GetRecordFields(tpy)
        let fieldTypeLookup indx = fieldInfo.[indx].PropertyType
        tpy, Expr.NewRecord(instance.GetType(), coerceValues fieldTypeLookup fields)

    and arrayExpr (instance : 'a array) =
        let typ = typeof<'a>
        let arrayType = instance.GetType()
        let exprs = coerceValues (fun _ -> typ) (instance |> Array.map box)
        arrayType, Expr.NewArray(typ, exprs)

    let createLetExpr varType instance body args = 
        let var = Var("instance", varType)  
        Expr.Let(var, instance, body args (Expr.Var(var)))

    let quoteUnion instance = unionExpr instance ||> createLetExpr
    let quoteRecord instance = recordExpr instance ||> createLetExpr
    let quoteArray instance = arrayExpr instance ||> createLetExpr

(**
When writing type providers you are required to define code that will run at run-time with a quotation. This in itself is not a problem, 
however if you try and pass a none native type to the quotation you will receive the following error,

     Unsupported constant type: xxxx

There is a stack-overflow post [here](http://stackoverflow.com/questions/10161437/type-provider-providing-me-with-an-unsuported-constant-type-system-double-er) which has an example and a good explanation of the reasons why. A typical work around is to use each 
field from a record and pass it to a function call in the quotation as an array or as individual parameters. Either way this can end up being quite painful.

So how can we work around this. Well, what we need to do is build a new instance of the object we are trying to pass to the quotation within the quotation 
itself, and then use the variable that holds this new instance as the parameter in the function call in the Quotation. I have probably not explained that 
the best but the final code looks like this.
*)

let recordInstance = { Name = "Tester"; DateTime = DateTime.UtcNow }

let providedMethod  = 
     ProvidedMethod("MethodName",
               [(* some parameters *)],typeof<SomeType>, 
               InvokeCode = 
                    QuotationHelpers.quoteRecord 
                         recordInstance 
                         (fun args var ->  <@@ ((%%args.[0] : SomeType).SomeMethod(%%var)) @@>))

(**
Where the args are the original arguments passed by provided method invoke code and var is a quotation that represents our record instance to pass to our method. 
The implementation of QuotationHelpers is as follows.
*)

(*** include:helper-defn ***)

(** And thats it. Hopefully this should remove some pain points in developing type providers. *)