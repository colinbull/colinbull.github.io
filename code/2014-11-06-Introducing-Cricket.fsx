(*** raw ***)
---
layout: page
title: Introducting Cricket.
---

(*** hide ***)
#I "../packages"
#r "FsPickler/FsPickler.dll"
#r "Cricket/Cricket.dll"

type 

(**
Over the last few years, I have been quite taken by the actor model of computing. Although not a silver bullet it does tend to make concurrent programming many orders of magnitude easier to reason about. If you have never heard of Actors then an actor as defined by [wikipedia](http://en.wikipedia.org/wiki/Actor_model) is as follows

> The Actor model adopts the philosophy that everything is an actor. This is similar to the everyt>hing is an object philosophy used by some object-oriented programming languages, but differs in t>hat object-oriented software is typically executed sequentially, while the Actor model is inheren>tly concurrent.
>
>An actor is a computational entity that, in response to a message it receives, can concurrently:
>
>* send a finite number of messages to other actors;
>* create a finite number of new actors;
>* designate the behavior to be used for the next message it receives.

I also encourage you to look at [Erlang/Elixir](http://www.erlang.org), [Akka](https://github.com/akkadotnet/akka.net), [Orleans](http://research.microsoft.com/en-us/projects/orleans/) and the [`MailboxProcessor<'a>` in FSharp.Core](http://msdn.microsoft.com/en-us/library/ee370357.aspx).

##Introducing Cricket
    
[Cricket](http://github.com/fsprojects/Cricket) (formally FSharp.Actor)  is yet another actor framework. Built entirely in F#, Cricket is a lightweight alternative to Akka et. al. To this end it is not as feature rich as these out of the box, but all of the core requirements like location transpancy, remoting, supervisors, metrics and tracing. Other things like failure detection and clustering are in the pipeline it is just a question of time. Currently the focus of the project is to get things running smoothly on both .NET CLR and Mono Runtimes. Anyway enough of that, to the code.
*)

ActorHost.Start()

let sayHello =
    actor {
        name "hello"
        body (
            let rec loop() = messageHandler {
                let! msg = Message.receive()
                printfn "%s" msg
                return! loop()
            }
            loop())
    } |> Actor.spawn


sayHello <-- "Hello"

(**

*)
