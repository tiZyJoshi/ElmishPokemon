open Elmish
open System

module Program =
    // http://fssnip.net/80F
    let mkProgramWithOrderExecute
            (init: 'arg' -> 'model * 'order)
            (update: 'msg -> 'model -> 'model * 'order)
            (view: 'model -> Dispatch<'msg> -> 'view)
            (execute: 'order -> Dispatch<'msg> -> unit) =
        let convert (model, order) = 
            model, order |> execute |> Cmd.ofSub 
        Program.mkProgram
            (init >> convert)
            (fun msg model -> update msg model |> convert)
            view
            

module ConsoleProgram = 
    type console = {
        handle : CsWin32.Handle
        line : int
    }

    let stdHandle = CsWin32.Kernel32.GetStdHandle()
    let newHandle = CsWin32.Kernel32.CreateConsoleScreenBuffer()

    let setCursorInvisible console = 
        CsWin32.Kernel32.SetCursorInvisible console.handle
        console

    let setActiveConsole console = 
        CsWin32.Kernel32.SetConsoleActiveScreenBuffer(console.handle) |> ignore
        console

    let clearOutput console =
        CsWin32.Kernel32.ClearScreen console.handle
        { console with line = 0 }

    let writeLineToOutput line console =
        CsWin32.Kernel32.WriteConsoleOutput(console.handle, line, 0, console.line, line.Length, 1) |> ignore
        { console with line = console.line + 1 }

    let rec writeLinesToOutput (lines : string list) console = 
        match lines with
        | [] -> console
        | x::xs -> 
            console
            |> writeLineToOutput x
            |> writeLinesToOutput xs 

    let writeTextToOutput (text : string) console = 
        writeLinesToOutput (text.Split '\n' |> Array.toList) console
    
    let writeDescription console =
        console |> writeTextToOutput "Count-o-matic : watch integers being counted on your console.
Press Space to start, pause or resume.
Press Enter to step while in pause.
Press +/- to increase/decrease speed while running.v
Press Q to quit."

    type Model = { 
        Running: bool
        Count: int
        Interval: int 
        FrontBuffer: console
        BackBuffer: console
    }

    let printModel model console = 
        console
        |> clearOutput
        |> writeDescription
        |> writeLineToOutput $"{model.Count}"
        |> setActiveConsole
        |> ignore

    let switchBuffers model = 
        { model with FrontBuffer = model.BackBuffer; BackBuffer = model.FrontBuffer }
    let increaseCount model =
        { model with Count = model.Count + 1 }
    let setRunning model =
        { model with Running = true }
    let setPaused model =
        { model with Running = false }
    let changeInterval x model = 
        { model with Interval = model.Interval+x |> min 2500 |> max 50 }

    type Msg = 
        | TimerTick
        | KeyboardTick
        | Toggle
        | ChangeInterval of offset: int
    /// user-defined order type
    type Order =
        | StartKeyListener
        | Print of value: Model
        | DelayTick of delay: int
        | CancelDelayedTick
        | Orders of Order list
        | NoOrder

    let init (running, interval) =
        //let backBuffer = 
        let model = { 
            Running = running
            Count = 0
            Interval = interval
            FrontBuffer = { handle = newHandle; line = 0 } |> setCursorInvisible |> setActiveConsole |> writeDescription
            BackBuffer = { handle = newHandle; line = 0 } |> setCursorInvisible
        }
        model, Orders [ StartKeyListener ; if running then DelayTick 0 ]

    let update msg model =
        match msg, model.Running with
        | TimerTick, true
        | Toggle, false ->
            let model' = model |> setRunning |> increaseCount |> switchBuffers
            model', Orders [ Print model' ; DelayTick model.Interval ]
        | Toggle, true ->
            model |> setPaused, CancelDelayedTick
        | KeyboardTick, false -> 
            let model' = model |> increaseCount |> switchBuffers
            model', Print model'
        | ChangeInterval x, true ->
            model |> changeInterval x, NoOrder
        | KeyboardTick, true | ChangeInterval _, false | TimerTick, false -> 
            model, NoOrder

    let view _ _ = ()
    
    /// Function executing orders, with a dispatch function as second argument.
    let rec execute order dispatch =
        match order with
        | StartKeyListener ->
            async {
                seq { while true do (Console.ReadKey true).KeyChar }
                |> Seq.takeWhile (fun key -> key <> 'q' && key <> 'Q')  // press q to quit
                |> Seq.iter (function
                    | ' ' -> dispatch Toggle
                    | '\013' -> dispatch KeyboardTick   // Enter key
                    | '-' -> dispatch (ChangeInterval 50)
                    | '+' -> dispatch (ChangeInterval -50)
                    | _ -> ())
                Async.CancelDefaultToken () }
            |> Async.StartImmediate
        | Print model -> 
            model.FrontBuffer
            |> printModel model
        | DelayTick delay ->
            async { do! Async.Sleep delay
                    dispatch TimerTick }
            |> Async.Start
        | CancelDelayedTick -> Async.CancelDefaultToken ()
        | Orders orders -> for order in orders do execute order dispatch
        | NoOrder -> ()

Program.mkProgramWithOrderExecute ConsoleProgram.init ConsoleProgram.update ConsoleProgram.view ConsoleProgram.execute
|> Program.runWith (false, 350)
