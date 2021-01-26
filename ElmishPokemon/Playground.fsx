//open Elmish
//open System

//module Counter =

//    type Model = { 
//        name : string
//        count : int
//    }

//    type Msg =
//        | Increment
//        | Decrement

//    let init name msg =
//        { name = name; count = 0 }, msg

//    let update msg model =
//        match msg with
//        | Increment -> 
//            let newCount = model.count + 1
//            printfn $"Incrementing {model.name}... New Count: {newCount}"
//            { model with count = newCount }, 
//            match newCount with
//            | x when x > 9 -> Cmd.none
//            | _ -> Cmd.ofMsg Increment
        
//        | Decrement -> 
//            let newCount = model.count - 1
//            printfn $"Decrementing {model.name}... New Count: {newCount}"
//            { model with count = newCount },
//            match newCount with
//            | x when x < -9 -> Cmd.none
//            | _ -> Cmd.ofMsg Decrement

//module Main =

//    type Model = { 
//        top : Counter.Model
//        bottom : Counter.Model 
//    }

//    type Msg =
//        | Reset
//        | Top of Counter.Msg
//        | Bottom of Counter.Msg

//    let init() =
//        let top, topCmd = Cmd.ofMsg Counter.Increment |> Counter.init "top"
//        let bottom, bottomCmd = Cmd.ofMsg Counter.Decrement |> Counter.init "bottom"
//        printfn $"topCmd is {topCmd}"
//        printfn $"bottomCmd is {bottomCmd}"
//        { top = top
//          bottom = bottom }, 
//        Cmd.batch [ Cmd.map Top topCmd
//                    Cmd.map Bottom bottomCmd ]

//    let update msg model : Model * Cmd<Msg> =
//        printfn $"msg is {msg}"
//        match msg with
//        | Reset -> init()
//        | Top msg' ->
//            let res, cmd = Counter.update msg' model.top
//            { model with top = res }, Cmd.map Top cmd
//        | Bottom msg' ->
//            let res, cmd = Counter.update msg' model.bottom
//            { model with bottom = res }, Cmd.map Bottom cmd

//    let view model _ =
//        printf "%A\n" model

////Program.mkProgram Main.init Main.update Main.view
////|> Program.run

//let timerAsync (duration:int) =
//    async {
//        do! Async.Sleep duration
//    }

//module Second =
//    type Msg =
//        | Second of int

//    type Model = int

//    let subscribe initial =
//        let sub dispatch =
//            Async.RunSynchronously (timerAsync 1000)
//            dispatch (Second System.DateTime.Now.Second) |> ignore
//        Cmd.ofSub sub

//    let init () =
//        0

//    let update (Second seconds) model =
//        seconds

//module Minute =
//    type Msg =
//        | Minute of int

//    type Model = int

//    let init () =
//        0

//    let update (Minute minutes) model =
//        minutes

//    let subscribe initial =
//        let sub dispatch =
//            Async.RunSynchronously (timerAsync 5000)
//            dispatch (Minute System.DateTime.Now.Minute) |> ignore
//        Cmd.ofSub sub

//module App =
//    type Model = { 
//        seconds : Second.Model
//        minutes : Minute.Model 
//    }

//    type Msg =
//        | SecondMsg of Second.Msg
//        | MinuteMsg of Minute.Msg

//    let init () = {
//        seconds = Second.init()
//        minutes = Minute.init()
//    }

//    let update msg model =
//        match msg with
//        | MinuteMsg msg ->
//           { model with minutes = Minute.update msg model.minutes }
//        | SecondMsg msg ->
//            { model with seconds = Second.update msg model.seconds }

//    let subscription model =
//        Cmd.batch [ 
//            Cmd.map SecondMsg (Second.subscribe model.seconds)
//            Cmd.map MinuteMsg (Minute.subscribe model.minutes)
//        ]

////Program.mkSimple App.init App.update (fun model _ -> printf "%A\n" model)
////|> Program.withSubscription App.subscription
////|> Program.run



//// Utility functions
//let tryParseInt (str: string) =
//    match System.Int32.TryParse str with
//    | (false, _) -> None
//    | (true, int) -> Some int

//let loadData (apiKey: string) =
//    async {
//        do! Async.Sleep 1000
//        return [ "This"; "Is"; "Obviously"; "Some"; "Fake"; "Data" ]
//    }   

//// Model
//type State =
//    | PromptForApiKey
//    | ChooseDatum of data: string list
//    | DisplayDatum of datum: string

//type Model = {
//    IsLoading: bool
//    ApiKey: string option
//    State: State
//}

//let init () =
//    { IsLoading = false; ApiKey = None; State = PromptForApiKey }, Cmd.none

//type Message =
//    | ApiKeyEntered of apiKey: string
//    | DataLoaded of data: string list
//    | DatumChosen of datum: string
//    | InvalidInput
    
//// View
//let viewApiKeyPrompt model dispatch =
//    printf "Enter your api key: "
//    System.Console.ReadLine()
//    |> ApiKeyEntered |> dispatch

//let viewDataChooser (model, data) dispatch =
//    let dataMap = data |> List.mapi (fun i datum -> (i + 1), datum) |> Map.ofList

//    printfn "Available data"
//    dataMap |> Map.iter (fun key datum ->
//        printfn "- [%i]: %s" key datum
//    )

//    printf "Choose a datum: "
    
//    System.Console.ReadLine()
//    |> tryParseInt
//    |> Option.bind (fun key -> Map.tryFind key dataMap)
//    |> function
//        | Some datum -> DatumChosen datum |> dispatch
//        | None -> InvalidInput |> dispatch

//let viewDatum (model, datum) dispatch =
//    printfn "You've chosen %s" datum
    
//    printfn "Press enter to exit."
//    System.Console.ReadLine() |> ignore

//let view model dispatch =
//    System.Console.Clear()
//    if model.IsLoading = true then
//        printfn "Loading"

//    match model.State with
//    | PromptForApiKey -> viewApiKeyPrompt model dispatch
//    | ChooseDatum data -> viewDataChooser (model, data) dispatch
//    | DisplayDatum datum -> viewDatum (model, datum) dispatch

//// Update
//let update message model =
//    match model.State, message with
//    | _, InvalidInput -> { model with IsLoading = false }, Cmd.none

//    | PromptForApiKey, ApiKeyEntered apiKey ->
//        { model with IsLoading = true; ApiKey = Some apiKey },
//        Cmd.OfAsync.perform loadData apiKey DataLoaded
    
//    | PromptForApiKey, DataLoaded data ->
//        { model with State = ChooseDatum data; IsLoading = false }, Cmd.none

//    | ChooseDatum _, DatumChosen datum ->
//        { model with State = DisplayDatum datum }, Cmd.none

//    | _ -> model, Cmd.none

////[<EntryPoint>]
////let main argv =
////    Program.mkProgram init update view
////    |> Program.run
////    0

//module Program =
//    /// <summary>
//    /// Program with user-defined orders instead of usual command.
//    /// Orders are processed by <code>execute</code> which can dispatch messages,
//    /// called in place of usual command processing.
//    /// </summary>
//    let mkProgramWithOrderExecute
//            (init: 'arg' -> 'model * 'order)
//            (update: 'msg -> 'model -> 'model * 'order)
//            (view: 'model -> Dispatch<'msg> -> 'view)
//            (execute: 'order -> Dispatch<'msg> -> unit) =
//        let convert (model, order) = 
//            model, order |> execute |> Cmd.ofSub 
//        Program.mkProgram
//            (init >> convert)
//            (fun msg model -> update msg model |> convert)
//            view

////let createConsoleScreenBuffer = 
////    CreateConsoleScreenBuffer(
////        GENERIC_READ ||| GENERIC_WRITE,
////        FILE_SHARE_READ ||| FILE_SHARE_WRITE,
////        IntPtr.Zero,
////        CONSOLE_TEXTMODE_BUFFER,
////        IntPtr.Zero)

////[<DllImport("kernel32.dll", SetLastError = true)>]
////extern IntPtr GetStdHandle(
////    int nStdHandle);

////let STD_OUTPUT_HANDLE = -11

////let getStdHandle = 
////    GetStdHandle(STD_OUTPUT_HANDLE)

////[<DllImport("kernel32.dll", SetLastError = true)>]
////extern bool SetConsoleActiveScreenBuffer(
////    IntPtr hConsoleOutput);

////let setConsoleActiveScreenBuffer hConsoleOutput =
////    SetConsoleActiveScreenBuffer(hConsoleOutput)
////    Console.

////[<type:StructLayout(LayoutKind.Sequential)>]
////type Coord = {
////    X : int16
////    Y : int16
////}

////[<type:StructLayout(LayoutKind.Explicit, CharSet=CharSet.Unicode)>]
////type CharUnion = {
////    [<FieldOffset(0)>] UnicodeChar : char
////    [<FieldOffset(0)>] AsciiChar : byte
////}

////[<type:StructLayout(LayoutKind.Explicit)>]
////type CharInfo = {
////    [<FieldOffset(0)>] Char : CharUnion
////    [<FieldOffset(2)>] Attributes : int16
////}

////[<type:StructLayout(LayoutKind.Sequential)>]
////type SmallRect = {
////    Left : int16
////    Top : int16
////    Right : int16
////    Bottom : int16
////}

////[<DllImport("kernel32.dll", SetLastError = true, EntryPoint = "WriteConsoleOutputW")>]
////extern bool WriteConsoleOutputW(
////    IntPtr hConsoleOutput, 
////    CharInfo[] lpBuffer, 
////    Coord dwBufferSize, 
////    Coord dwBufferCoord, 
////    SmallRect lpWriteRegion);

