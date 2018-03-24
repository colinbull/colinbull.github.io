- title : Intro to Type Providers
- description : Introduction to type providers
- author : Colin Bull
- theme : league
- transition : default


***

# Introduction to Type Providers

***


### Provide types for all the things!! 

* Database (SQL Server, Oracle, Postgres, MySql, InfluxDb)
* WSDL, ODATA
* HTML, CSV, XML JSON
* Powershell 
* RegEx 
* R Language 
* WMI 

* __Even a two player game of battleships__ 

* Plus many, many more.... 

***

### An example

#### given this (my_data.csv)

    Timestamp, ActivePower, ReactivePower
    01-03-2018T08:00Z, 52.4, 0.01
    01-03-2018T08:30Z, 43.4, 0.61

#### then in code

    type MyData = CsvProvider<"my_data.csv">

    //(string -> seq<DateTime * float>)
    let read path = 
        MyData.Load(path).Rows
        |> Seq.map (fun x -> x.Timestamp, x.ActivePower)

### This is not codegen!! 

***

## More formally
* Extend the F# type system capabilities at compile/design time 
* Provide a type space for language service to interact with.
* Provide erased or generative runtime implementation.

***

## WTF is Erased and Generative?

![confused](images/confused.jpg)

***

## Erased

* Only the types that are referenced are kept
* Type information is only used by the compiler and not emitted
* No reflection possible. 
* To be shared, generally needs to be converted to a another type. 

### Erased providers can deal with infinite type spaces.

***

## Generative

* All types evaluated by the provider are emitted
* Reflection is possible. 
* Types can be shared. 

***

### Generative providers are still not codegen!!

##### That said the WSDL provider uses svcutil ¯\\\_(ツ)_/¯

***

***

### Connecting to a DB

* Two strategies have emerged
    * Dapper Type
    * ORM Type


***

### Dapper Type 

    use cmd = new SqlCommandProvider<"
            SELECT TOP(@topN) FirstName, LastName, SalesYTD 
            FROM Sales.vSalesPerson
            WHERE CountryRegionName = @regionName AND SalesYTD 
                  > @salesMoreThan 
            ORDER BY SalesYTD
            " , connectionString>(connectionString)

        cmd.Execute(topN = 3L, 
                    regionName = "United States", 
                    salesMoreThan = 1000000M) 
        |> printfn "%A"

    //output
    //seq
    //    [("Pamela", "Ansman-Wolfe", 1352577.1325M);
    //     ("David", "Campbell", 1573012.9383M);
    //     ("Tete", "Mensa-Annan", 1576562.1966M)]

###### FSharp.Data.SqlClient example

***

### ORM Type 

    type sql = SqlDataProvider<
                    Common.DatabaseProviderTypes.SQLITE,
                    connectionString>
    let ctx = sql.GetDataContext()

    let example =
        query {
            for order in ctx.Main.Orders do
            where (order.Freight > 0m)
            sortBy (order.ShipPostalCode)
            skip 3
            take 4
            select (order)
        }


***

## A couple of CONS 

* May require the source of the type space available to CI servers.
* Badly written or greedy providers on large schemas can slow down editors.
* Making them thread safe can be difficult
* Errors can be some-what hard to interpret.

***

## Your turn!!

Using the lego data and script provided 

1. Find how the size of lego sets have grown over time?
2. Find the lego set with the most distinct parts?
3. Plot 1. on a graph.

*** 

## If you're interested example Type provider code.. 

***  
    type [<TypeProvider>] HelloWorldProvider (config : TypeProviderConfig) =
        inherit TypeProviderForNamespaces (config)
        let ns, asm = "HelloWorldProvider", Assembly.GetExecutingAssembly()
        
        let createType typeName (count:int) =
            let myType = ProvidedTypeDefinition(
                asm, ns, typeName, Some typeof<obj>)

            for i in 1 .. count do 
                let prop = 
                    ProvidedProperty(string i, typeof<int>, IsStatic = true, 
                        getterCode = fun args -> <@@ count @@>)
                myType.AddMember(prop)
            myType

        let myParamType = 
            let t = ProvidedTypeDefinition( ... )
            t.DefineStaticParameters(
                [ProvidedStaticParameter("Count", typeof<int>)], 
                fun typeName args -> createType typeName (unbox<int> args.[0]))
            t
        do _.AddNamespace(ns, [myParamType])
 
    [<assembly:TypeProviderAssembly>] do()
***


