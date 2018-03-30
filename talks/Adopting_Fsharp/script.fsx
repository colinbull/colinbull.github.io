

let (>>=) m f = Option.bind f m 

let v = 
    (Some 1) 
    >>= (fun a ->
         (Some 2) 
         >>= (fun x -> Some(x + a))
    )

