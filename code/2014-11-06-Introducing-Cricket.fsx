(*** raw ***)
---
layout: page
title: Introducing Cricket (formerly FSharp.Actor)
---

(*** hide ***)

(**
Over the last few years, I have been quite taken by the actor model of computing. Although not a silver bullet it does tend to make concurrent programming orders of magnitude easier to reason about. If you have never heard of Actors then an actor as defined by [wikipedia](http://en.wikipedia.org/wiki/Actor_model) is as follows

> The Actor model adopts the philosophy that everything is an actor. This is similar to the everything is an object philosophy used by some object-oriented programming languages, but differs in that object-oriented software is typically executed sequentially, while the Actor model is inherently concurrent.
>
>An actor is a computational entity that, in response to a message it receives, can concurrently:
>
>* send a finite number of messages to other actors;
>* create a finite number of new actors;
>* designate the behavior to be used for the next message it receives.

I also encourage you to look at [Erlang/Elixir](http://www.erlang.org), [Akka](https://github.com/akkadotnet/akka.net), [Orleans](http://research.microsoft.com/en-us/projects/orleans/) and the [`MailboxProcessor<'a>` in FSharp.Core](http://msdn.microsoft.com/en-us/library/ee370357.aspx).

##Introducing Cricket
    
[Cricket](http://github.com/fsprojects/Cricket), formerly FSharp.Actor, is yet another actor framework. Built entirely in F#, Cricket is a lightweight alternative to Akka et. al. To this end it is not as feature rich as these out of the box, but all of the core requirements like location transpancy, remoting, supervisors, metrics and tracing. Other things like failure detection and clustering are in the pipeline it is just a question of time.

Some key links for Cricket:

* [Cricket Home Page](http://fsprojects.github.io/Cricket)
* [Cricket Source code](http://github.com/fsprojects/Cricket) on GitHub
* [Cricket NuGet package](http://www.nuget.org/packages/Cricket)

The nuget package, contains a single library `Cricket.dll` and a reference to [FsPickler](https://github.com/nessos/FsPickler), which is used for serailization.

###Creating a simple actor

The following example, creates a `echo` actor using cricket.   
*)

#I "../packages"
#r "FsPickler/lib/net45/FsPickler.dll"
#r "Cricket/lib/Cricket.dll"

open Cricket


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

(**
A couple of things are happening in the code above. Firstly, we start an `ActorHost` which sets up an environment within the current process for the actor to live in. Next we define the actor, we give it a name `echo` and a body. The body is actually the only thing that is required. If the name is omitted then it is assinged as a `Guid`. All the body of an actor consists of is a recursive function, that describes how to handle the messages posted to the actor. In this case we simply print a message to the console. Once we have defined the actor we then spawn it using `Actor.spawn`. After an actor has been spawned it is ready to consume messages. We can send messages directly to the actor by using the `ActorRef` that is returned by `Actor.spawn`.      
*)

echo <-- "Hello, from Cricket"
(**
Alternatively we can resolve the actor by name and send the message that way. 
*)

"echo" <-- "Hello, from Cricket"

(**
From these basic begining we can build entire systems using actors. The systems can be spread over multiple machines, and as long as the underlying message transport supports it different data-centres. To create make our simply echo actor distributed. We don't have to change the implementation of the actor. All we have to do is enable remoting on the actor host.  


     //Node1 host configuration
     ActorHost.Start()
              .SubscribeEvents(fun (evnt:ActorEvent) -> printfn "%A" evnt)
              .EnableRemoting(
                        [new TCPTransport(TcpConfig.Default(IPEndPoint.Create(12002)))],
                        new BinarySerializer(),
                        new TcpActorRegistryTransport(TcpConfig.Default(IPEndPoint.Create(12003))),
                        new UdpActorRegistryDiscovery(UdpConfig.Default(), 1000))


All we have done is enchance the ActorHost with a collection of message transports, a serializer, a registry transport and a way for the actors to discover each other. Similar setif we used the same setup on another node.

     //Node2 host configuration
     ActorHost.Start()
              .SubscribeEvents(fun (evnt:ActorEvent) -> printfn "%A" evnt)
              .EnableRemoting(
                        [new TCPTransport(TcpConfig.Default(IPEndPoint.Create(12004)))],
                        new BinarySerializer(),
                        new TcpActorRegistryTransport(TcpConfig.Default(IPEndPoint.Create(12005))),
                        new UdpActorRegistryDiscovery(UdpConfig.Default(), 1000))

then we can on node 2 resolve any actors on node 1, using the example above. Alternatively if I had 10 nodes but wanted to resolve the `echo` actor on node 9, I could do something like the following
*)

"node9@*/echo" <-- "Hello, from Cricket"

(**
This would then resolve the actor on `node9`. If we had kept the original query which was simply `echo` then this would resolve any actor named `echo` all of the nodes participating in the group. For more details on remoting and a link to an example see [here](http://fsprojects.github.io/Cricket/remoting.html) 
*)
