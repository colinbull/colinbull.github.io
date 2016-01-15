(*** raw ***)
//---
//layout: page
//title: Lenses
//---
(*** hide ***)



(**
##Introduction

A while ago, I stumbled over the `Aether` library by Andrew Cherry
(@kolektiv). This is a simple single file library that implements the
lens abstraction in F#. Now there are lots of blog posts about lenses
most of them about the haskell implementation and all of them are
impenitrable if you don't know the haskell language. However, lenses
in the basic form are a very simple concept that I shall now attempt
to explain.

> Disclaimer: By no means am I an expert at this. And in no way will
> this be a rigourous treatment of lenses, for that you may want to
> look at one of the lens references linked at the bottom of this post

##What are lenses? And whats the point?

A lens is simply a tuple of two functions, which act on a common type,
or set of types.
*)

type Lens<'a,'b> = ('a -> 'b) * ('b -> 'a -> 'a)

(**
 For example, given the following record type definition
*)

type MyRecord = {
    Id : string
    Average : float
    Values : float list
}

let numbers = { Id = "Numbers"; Average = 5. ;  Values = [0. .. 10.] }

(**
we can construct a lens for each property on that record.
*)

let idLens : Lens<MyRecord,string> =
    (fun instance -> instance.Id), (fun value instance -> { instance with Id = value })

let valueLens : Lens<MyRecord, float list> =
    (fun instance -> instance.Values),
    (fun value instance -> { instance with
                                Average = List.average value
                                Values = value })

(**

In this case we can see that all the above lens function describe are
the getters and setters for the properties 'Id' and 'Value'
respectively. However we cannot simply apply these to an instance of
our record, firstly we need some helper functions that allow us to use
the lens to get or set a value, on our instance.
*)


let getLens (lens:Lens<'a,'b>) (instance:'a) = (fst lens) instance 

let setLens (lens:Lens<'a, 'b>) (instance:'a) (value:'b) = (snd lens) value instance

(**
We can then get/set our respective values by using our functions
*)

getLens idLens numbers

(** or *)

setLens idLens numbers "Id_1"

(**
So whats the point? you may be asking; Why couldn't I
just write the respective getters and setters inline? Well, to be
honest you can do, in this simple example there is no difference,
But what if you wanted to do something extra with the values as they where being get
or set? For example what if instead of extracting the whole list we
just wanted to take the first value? How would that look? Something like this
imagine, 

*)

let firstValue (record:MyRecord) = record.Values |> List.head

numbers |> firstValue

(**

Which is fine, but it is tied to the type `MyRecord` and is a specific
implementation. If we want to use it with a different type that also
contains a list of values we can't. At this point we could just resort
to putting the code to get the value and read the head of the list in
line everywhere, or have an interface which allows use to abstract the operation.
Which in the spirit of keeping things simple my well be an option, however this may
cause us problems, as we shall see later. So, what would this look like if we took it down the
lens route? Well firstly we can define a lens, which represented
reading the head of a list and then setting a value back to the head
of a list

*)

let headListLens =
    List.head, (fun value instance -> value :: (List.tail instance))

(**

Now all we need to do is plug the or `valueLens` and `headListLens`. However this
isn't as simple as composing the two functions using function composition `>>` or
sequnetially applying functions using `|>` as above. Why? becuase what we have is a tuple
of 2 functions a getter and a setter. So how do we compose these? Well
we need some function that glues together the getter and another
function that glues together the setter. 

*)

let compose (getter, setter) (getter', setter') =
    (fun instance -> getter' (getter instance)),
    (fun value instance -> setter (setter' value (getter instance)) instance)

let (>-->) lensA lensB = compose lensA lensB

(**

Above is a function that does exactly that. On the getter leg (the
first element of the tuple), we create a function that takes the
instance we want to retrieve values from and apply the first getter,
then take the result of that and apply the second getter, which gives
use our final value. On the setter (the second element of the tuple)
it is a tad more involved but still simple if you work from the inner
most call outwards. Basically we get the value we are going to make
changes to from the first lens then set the desired value using the
setter from the second lens. Afterwards we put the new value back
using the setter from the first lens. Simple. Once we have the compose
function and operator we can now bring to together our two lenses from
before to make a single lens that does what we require. 
*)

let firstValueLens =
    valueLens >--> headListLens

getLens firstValueLens numbers 

(**
Which gives us exactly the same result as previous. Which is good, with the added
bonus that we now can compose these lenses together. But still what is
the point. Well notice here that the lens `firstValueLens` itself has no idea of the
internal structure of the record type, and this is good. This means we
are free to now change the shape of our record type and not have to
worry about having an impact on the rest of our codebase. The only
thing we would need to change in response is the implementation of the
lenses on the `myRecord` type directly. 

##Partial Lenses, handling errors

*)
