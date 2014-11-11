(*** raw ***)
---
layout: page
title: Introducting Cricket.
---

(*** hide ***)
#I "../packages"
#r "FsPickler/FsPickler.dll"
#r "Cricket/Cricket.dll"

open Cricket

(**
Over the last few years, I have been quite taken by the actor model of computing. Although not a silver bullet it does tend to make concurrent programming orders of magnitude easier to reason about. If you have never heard of Actors then an actor as defined by [wikipedia](http://en.wikipedia.org/wiki/Actor_model) is as follows

> The Actor model adopts the philosophy that everything is an actor. This is similar to the everyt>hing is an object philosophy used by some object-oriented programming languages, but differs in t>hat object-oriented software is typically executed sequentially, while the Actor model is inheren>tly concurrent.
>
>An actor is a computational entity that, in response to a message it receives, can concurrently:
>
>* send a finite number of messages to other actors;
>* create a finite number of new actors;
>* designate the behavior to be used for the next message it receives.

I also encourage you to look at [Erlang/Elixir](http://www.erlang.org), [Akka](https://github.com/akkadotnet/akka.net), [Orleans](http://research.microsoft.com/en-us/projects/orleans/) and the [`MailboxProcessor<'a>` in FSharp.Core](http://msdn.microsoft.com/en-us/library/ee370357.aspx).

##Introducing Cricket
    
[Cricket](http://github.com/fsprojects/Cricket), formally FSharp.Actor, is yet another actor framework. Built entirely in F#, Cricket is a lightweight alternative to Akka et. al. To this end it is not as feature rich as these out of the box, but all of the core requirements like location transpancy, remoting, supervisors, metrics and tracing. Other things like failure detection and clustering are in the pipeline it is just a question of time.

Some key links for Cricket:

* [Cricket Home Page](http://fsprojects.github.io/Cricket)
* [Cricket Source code](http://github.com/fsprojects/Cricket) on GitHub
* [Cricket NuGet package](http://www.nuget.org/packages/Cricket)

The nuget package, contains a single library `Cricket.dll` which contains the Cricket API. The following example, creates a `echo` actor using cricket.   
*)

ActorHost.Start()

let echo =
    actor {
        name "echo"
        body (
            let rec loop() = messageHandler {
                let! msg = Message.receive()
                printfn "%s" msg
                return! loop()
            }
            loop())
    } |> Actor.spawn

echo <-- "Hello, from Cricket"

(**

*)
