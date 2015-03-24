(*** raw ***)
//---
//layout: page
//title: Why I like F#.
//---

(*** hide ***)
#r @"..\packages\FSharp.Data\lib\net40\FSharp.Data.dll"
open FSharp.Data
open System.IO
open System

let chunkBySize chunkSize (source : seq<_>) =
      seq { use e = source.GetEnumerator()
            let nextChunk() =
                let res = Array.zeroCreate chunkSize
                res.[0] <- e.Current
                let i = ref 1
                while !i < chunkSize && e.MoveNext() do
                    res.[!i] <- e.Current
                    i := !i + 1
                if !i = chunkSize then
                    res
                else
                    Array.sub res 0 !i
            while e.MoveNext() do
                yield nextChunk() }

let unixToDateTime (timestamp:int) =
    let dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)
    let ret = dateTime.AddSeconds(timestamp |> float).ToLocalTime()
    ret
        
(**
Recently Dr. James McCaffery, posted [Why he doesn't like the F# language](https://jamesmccaffery.wordpress.com/2015/03/01/why-i-dont-like-the-f-language/). Quite a few of his points are subjective. People have different preferences and it seems like F# and more generally functional programming takes him outside of this comfort zone. This is fine, and I have absolutly no objections about views like this. I have a similar feeling when I'm in C# or Java. I don't feel safe, or comfortable, again it is just a preference thing.

However, there are a few points raised in the blog post that I don't really agree with. I'll tackle each one seperatly not to loose any context.

>2. F# has a tiny user base.
>
>I did a quick search on the day I wrote this post at a job aggregation site and found 109 job listings that mentioned F#. There were over 34,000 job listings that mentioned C#. And at MSDN Magazine, where I`m the Senior Contributing Editor, our F# articles get very few reads. The tiny user base means there is relatively weak community technical support on sites like Stack Overflow, compared to mainstream languages. Additionally, unlike other languages with relatively few users (such as R), there`s no real motivation for me to adopt F# from a career point of view, because of the very limited job opportunities.

While I somewhat agree, that F# adoption in industry has been slow. I think alot of this is to do with the fact that in the early days F# wasn't pushed as a general purpose programming language. This was obviously a marketing decision made in Microsoft, for reasons that are unknown to me. This decision caused an elitist view of F# in the early days with the preception that you need a advanced degree in a mathematical subject to use it, categorising it as only being good for Data Science, Finance, Mathematics and Research. Thus these areas were the early adoptors. In fact a quick browse of the [testimonials page](http://www.fsharp.org/testimonials) on FSharp.org backs this up. With testimonails coming from one of these areas. There are of course some exceptions most notably the design of Louvre in Abu Dhabi and it's use at GameSys.

However this metric is only one dimension and just because there are currently only a few jobs in a language doesn't mean you should not learn it. I'm currently learning langauges like Haskell, Coq and Idris. For the latter I doubt there is a single role in this country (although I'm willing to be proved wrong on this). Why do I do this? I hear you ask. Well I believe by learning different langauges and paradigms pushes me slightly out of my comfort zone and makes me a better programmer, in which-ever language I ultimately end up coding a commercial product in.

With the commercial prospects aside a conslusion is drawn that a small user base => a weak technical community. I don't know about other languages but I can categorically say that with F# this is simply not true. In fact, as I started writing this blog post, I raised an issue in the [Paket](https://github.com/fsprojects/Paket/issues/719#event-263240044) project on github and within an hour, I had a fix presented too me.

For other sites like Stack Overflow, I can really comment on the experience as I don't tend to use it much myself, but we can use F# to do some data munging to see how the community it doing. i.e. What is the average time for questions with an accepted answer to have got that answer?

To acheive this we can download the first 10000 questions with the F# tag, and write the result of each request out to a set of files. 
*)


let baseUri = "https://api.stackexchange.com/2.2/"
let [<Literal>] dataPath = __SOURCE_DIRECTORY__ + "/data/stackoverflow/"

let dataDir =
    let path = new DirectoryInfo(dataPath)
    if not(path.Exists)
    then path.Create()
    path
    
let getQuestions(page) =
    let outputPath = new FileInfo(Path.Combine(dataDir.FullName, sprintf "questions_%d.questions" page))
    if(not <| outputPath.Exists)
    then
        let results =
            Http.RequestString(baseUri + sprintf "search?page=%d" page + "&pagesize=100&order=desc&sort=creation&tagged=f%23&site=stackoverflow")
        File.WriteAllText(outputPath.FullName, results)

let writeQuestions() =
    [1 .. 100] |> List.iter getQuestions

(**
 Next we can merge all of these questions using the [Json type provider](http://fsharp.github.io/FSharp.Data/library/JsonProvider.html) into a single list,  
*)

let [<Literal>] questionPath = dataPath + "questions.json"
type Questions = JsonProvider<questionPath>

let questions =
    [
       for file in  dataDir.EnumerateFiles("*.questions") do
           yield! Questions.Load(file.FullName).Items
    ]

(**
Next up is getting the accepted answers. Firstly we build a map of the accepted answersId against the questions so we can relate them again later, then we use `getAcceptedAnswers` to chunk the requests and write the results out to a file. Once we have the results we again use the Json type provider to merge the results up into a single list.  
*)

let questionAnswerMap = 
    questions
    |> Seq.fold (fun state question ->
        match question.AcceptedAnswerId with
        | Some answerId -> (answerId, question) :: state  
        | None -> state
    ) []  
    |> Map.ofSeq
    
let getAcceptedAnswers() =
    let answerIds =
        questionAnswerMap
        |> Map.toSeq
        |> Seq.map (fun (answerId,_) -> answerId.ToString())

    let counter = ref 1
    for answers in chunkBySize 100 answerIds do
        let outputPath = new FileInfo(Path.Combine(dataDir.FullName, sprintf "answers_%d.answers" !counter))
        if (not <| outputPath.Exists)
        then
            let answersStr = String.Join(";", answers)
            let answers =
                Http.RequestString(
                     baseUri + sprintf "answers/%s?order=desc&sort=creation&site=stackoverflow" answersStr
                )
            printfn "Writing answers %s" outputPath.FullName
            File.WriteAllText(outputPath.FullName, answers)
            incr(counter)

let [<Literal>] answersPath = dataPath + "answers.json"
type Answers = JsonProvider<answersPath>

let answers =
    [
       for file in  dataDir.EnumerateFiles("*.answers") do
           yield! Answers.Load(file.FullName).Items
    ]

(**
Next up we pair the questions with the accepted answers. 
*)

let mergeQuestionAnswers =
    [
        for answer in answers do
            match questionAnswerMap.TryFind answer.AnswerId with
            | Some question -> yield question, answer
            | None -> ()            
    ]

(**
An we are now at a point where we can work out the average time to close a question. 
*)

let getTimeToClose (question : Questions.Item, answer : Answers.Item) =
    (unixToDateTime answer.CreationDate).Subtract(unixToDateTime question.CreationDate)

let timeToClose =
    List.map getTimeToClose (mergeQuestionAnswers)

(*** define-output:average ***)
printfn "%f Hours - To Accepted answer" (timeToClose |> List.averageBy (fun x -> x.TotalHours))

(*** include-output:average ***)

(**
And actually we see 110 hours. Which actually, isn't as good as I was expecting, but then again, the F# compiler has been open sourced for quiet some time now. Typically this has mean't questions are directed to [fsharp github repository](https://github.com/Microsoft/visualfsharp) previously on codeplex, or people simply take a look at the code and figure it out for themselves. Or ask on twitter with the #fsharp tag, and wait for the plethora of responses to come in from the help and very active community members.

In fact as I write this I'm wondering whether the comment

> few irritatingly vocal people in the F# community implicitly try to guilt non-believers into F# adoption.

has been spurred by the willingness to help in the community. Yes there is a certain amount of advertisment that goes on for features specific to F#, but in general it is just sound fundamental programming advice. I'm fairly sure every single one of those people would offer examples in C#, Haskell or VB if asked. Anyway I digress.

The second comment that stood out for me in the post was,

>3. F# has no compelling technical advantage.

>Some overly zealous F# fans claim that F# can do things that general purpose languages like Perl and C# can��t. Thi>s all depends on how you define features of languages. But more reasonable F# proponents usually state, correctly,> that F# isn��t intended to replace languages like C# and that F# doesn��t have any unique, magic capabilities. So, >there��s no technical reason for me to use F#, and the cost of context switching between the primarily procedural C># and the primarily functional F# is a huge price to pay.

I think you only have to look at type providers, which are used to analyse the Stack Overflow questions above are certainly a nice feature. That to my knowledge only one other language has [Idris](https://github.com/david-christiansen/idris-type-providers). Sure you can do the analysis I have done above in C# but there will be alot more typing and additionally a lot less safety, since you will ultimately loose the strongly typed data access, that type providers offer. Moreover it is this safety that F# and statically typed functional programming languages in general offers you and makes it worth the context switch.

Since I adopted F# and functional programming it has completely changed the way I think about coding problems in all of the other languages I use, most notable C#. It has made me a better developer. 
*)