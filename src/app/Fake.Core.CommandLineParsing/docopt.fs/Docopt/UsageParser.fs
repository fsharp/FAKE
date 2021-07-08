namespace Fake.Core

exception DocoptException of string
  with override x.Message = sprintf "%s" x.Data0

namespace Fake.Core.CommandLineParsing

open Fake.Core
open FParsec
open System
open System.Text

exception private InternalException of ErrorMessageList
exception UsageException of string
  with override x.Message = sprintf "%s" x.Data0

module private Helpers =
    let raiseArgvException errlist' =
      let pos = Position(null, 0L, 0L, 0L) in
      let perror = ParserError(pos, null, errlist') in
      raise (DocoptException(perror.ToString()))
    let improveErrorText (lnNr:int64) (colNr:int64) (arg:string) (oldText:string) =
      oldText.Replace(
        sprintf "argv: Ln: %d Col: %d" lnNr colNr,
        sprintf "Argument %d ('%s')" (colNr + 1L) arg)    
    let unexpectedShort = ( + ) "short option -"
                          >> unexpected
    let unexpectedLong = ( + ) "long option --"
                              >> unexpected
    let expectedArg = ( + ) "argument "
                      >> expected
    let unexpectedArg = ( + ) "argument "
                        >> unexpected
    let raiseInternal exn' = raise (InternalException exn')
    let raiseUnexpectedShort s' = raiseInternal (unexpectedShort s')
    let raiseUnexpectedLong l' = raiseInternal (unexpectedLong l')
    let raiseUnexpectedArg a' = raiseInternal (unexpectedArg a')
    let inline printSOption s =
      match s with
      | None -> ""
      | Some s -> s

    let inline printReplyStatus (r:ReplyStatus) =
      match r with
      | ReplyStatus.Ok -> "Ok"
      | ReplyStatus.Error -> "Error"
      | ReplyStatus.FatalError -> "FatalError"
      | _ -> sprintf "%d" (int r)
          

open Helpers

module ArgumentArray =
  let isShortArgument (arg:string)=
    arg.StartsWith "-" && not (arg.StartsWith "--") && arg <> "-"


type ArgumentStreamPosition =
  // The position of the argument
  | ArgumentPos of int
  // For short options like -adf we iterate over every letter
  | ShortArgumentPartialPos of int * int
  override x.ToString () =
    match x with
    | ArgumentPos p -> sprintf "ArgumentPos(%d)" p
    | ShortArgumentPartialPos (p, part) -> sprintf "ArgumentPos(%d, %d)" p part 
  member x.ArgIndex =
    match x with
    | ShortArgumentPartialPos (p, _)
    | ArgumentPos p -> p
  member x.InnerIndex =
    match x with
    | ShortArgumentPartialPos (_, i) -> i
    | ArgumentPos _ -> 0

  member x.IsEndOf (argv:string array) =
    match x with
    | ArgumentPos x -> x >= argv.Length
    | ShortArgumentPartialPos (x, innerPos) ->
      if x >= argv.Length then true
      else
        if x = argv.Length - 1 then
          let arg = argv.[x]
          innerPos >= arg.Length
        else false

  member x.Next (argv:string array) =
    let res =
      match x with
      | ArgumentPos x ->
        assert (x >= argv.Length || not (ArgumentArray.isShortArgument argv.[x]))
        if x + 1 < argv.Length then
          let next = argv.[x+1]
          if not (ArgumentArray.isShortArgument next) then
            ArgumentPos (x + 1)
          else
            ShortArgumentPartialPos(x + 1, 1)
        else ArgumentPos (argv.Length)
      | ShortArgumentPartialPos (x, i) ->
        let c = argv.[x]
        if i + 1 < c.Length then
          ShortArgumentPartialPos (x, i + 1)
        else
          if x + 1 < argv.Length then
            let next = argv.[x+1]
            if not (ArgumentArray.isShortArgument next) then
              ArgumentPos (x + 1)
            else
              ShortArgumentPartialPos(x + 1, 1)
          else ArgumentPos (argv.Length)
    //printfn "%A -> Next(%A) -> %A" x argv res
    res

  member x.NextArg (argv: string array) =
    let res =
      match x with
      | ArgumentPos x
      | ShortArgumentPartialPos (x, _) ->
        if x + 1 < argv.Length then
          let next = argv.[x+1]
          if not (ArgumentArray.isShortArgument next) then
            ArgumentPos (x + 1)
          else
            ShortArgumentPartialPos(x + 1, 1)
        else ArgumentPos (argv.Length)
    //printfn "%A -> NextArg(%A) -> %A" x argv res
    res

type IArgumentStreamState<'TUserState> = interface end // { Pos : int; StateTag : int64; UserState : 'TUserState }
type IArgumentStream<'TUserState> =
  abstract CurrentState : IArgumentStreamState<'TUserState>
  abstract Position : ArgumentStreamPosition
  abstract RestoreState : IArgumentStreamState<'TUserState> -> unit
  abstract StateTag : int64
  abstract UserState :  'TUserState with get, set
  abstract UpdateStateTag : unit -> unit
  abstract Seek : ArgumentStreamPosition -> unit
  abstract Peek : unit -> string option
  abstract PeekFull : unit -> string option
  abstract IsEnd : bool
  abstract Skip : unit -> unit
  abstract SkipFull : unit -> unit
  abstract SkipAndPeek : unit -> string option
  abstract SkipAndPeekFull : unit -> string option
  abstract Read : unit -> string option
  abstract ReadFull : unit -> string option
  abstract Argv : string array


type ArgumentStreamState<'TUserState> =
  { Pos : ArgumentStreamPosition; StateTag : int64; UserState : 'TUserState }
  with interface IArgumentStreamState<'TUserState>
type ArgumentStream<'TUserState> private (argv:string array, initState:'TUserState) =
  let mutable pos =
    if argv.Length > 0 && ArgumentArray.isShortArgument argv.[0]
    then ShortArgumentPartialPos(0, 1)
    else ArgumentPos 0
  let mutable stateTag = 0L
  let mutable state = initState
  let markChange () =
    stateTag <- stateTag + 1L
  let inc () =
    markChange()
    pos <- pos.Next(argv)
  let incFull () =
    markChange()
    pos <- pos.NextArg(argv)
    //if pos.ArgumentPosition < argv.Length then pos <- pos + 1
  let current () =
    match pos with
    | ArgumentPos x -> 
      if x < argv.Length then Some argv.[x] else None
    | ShortArgumentPartialPos (x, i) ->
      if x < argv.Length then Some (sprintf "-%c" argv.[x].[i])
      else None  
  let currentFull () =
    match pos with
    | ShortArgumentPartialPos (x, _)
    | ArgumentPos x ->
      if x < argv.Length then Some argv.[x] else None
  interface IArgumentStream<'TUserState> with  
    member x.CurrentState = { Pos = pos; StateTag = stateTag; UserState = state} :> IArgumentStreamState<'TUserState>
    member x.Position = pos
    member x.RestoreState (oldState:IArgumentStreamState<'TUserState>) =
      match oldState with
      | :? ArgumentStreamState<'TUserState> as oldState ->
        pos <- oldState.Pos
        stateTag <- oldState.StateTag
        state <- oldState.UserState
      | _ -> failwithf "invalid state object"      
    member x.StateTag = stateTag
    member x.UserState 
      with get () = state
      and set v = 
        if not (obj.ReferenceEquals(v, state)) then
          markChange (); state <- v
    member x.UpdateStateTag () = markChange()
    member x.Seek newPos =
      markChange()
      pos <- newPos
    member x.Peek () = current()
    member x.PeekFull () = currentFull()
    member x.IsEnd = pos.IsEndOf argv
    member x.Skip () = inc()
    member x.SkipFull () = incFull()
    member x.SkipAndPeek () =
      let xx = x :> IArgumentStream<_>
      xx.Skip(); xx.Peek()
    member x.SkipAndPeekFull () =
      let xx = x :> IArgumentStream<_>
      xx.SkipFull(); xx.PeekFull()
    member x.Read () =
      let va = current()
      inc()
      va
    member x.ReadFull () =
      let va = currentFull()
      incFull()
      va
    member x.Argv = argv
  static member Create(argv:string array, initState:'TUserState) =
    new ArgumentStream<'TUserState>(argv, initState)
  override x.ToString() =
    sprintf "Pos: %O, [|%s|], state: %O" pos (System.String.Join(";", argv)) state

module ArgumentStream =
  let create (argv:string array) (initState:'TUserState) =
    ArgumentStream<'TUserState>.Create(argv, initState) :> IArgumentStream<_>
  let clone (stream:IArgumentStream<_>) =
    let clone = ArgumentStream<'TUserState>.Create(stream.Argv, stream.UserState) :> IArgumentStream<_>
    clone.RestoreState ({ Pos = stream.Position; StateTag = stream.StateTag; UserState = stream.UserState} :> IArgumentStreamState<'TUserState>)
    clone

  type MappingStreamState<'u, 't, 'tinner> =
    { Inner : IArgumentStreamState<'tinner>
      State : 'u }
    with interface IArgumentStreamState<'t>
  let map (newState:'un) map (inner:IArgumentStream<'uo>) =
    let mutable newState = newState
    { new IArgumentStream<'un> with
        member x.CurrentState = 
          let innerState = inner.CurrentState
          { Inner = innerState; State = newState } :> IArgumentStreamState<'un>
        member x.Position = inner.Position
        member x.RestoreState (oldState:IArgumentStreamState<'un>) =
          match oldState with
          | :? MappingStreamState<'un, 'un, 'uo> as state ->
         // let state = { Pos = oldState.Pos; StateTag = oldState.StateTag; UserState = map oldState.UserState }
            inner.RestoreState state.Inner
            newState <- state.State
          | _ -> failwithf "Invalid state object"            
        member x.StateTag = inner.StateTag
        member x.UserState 
          with get () = newState
          and set v =
            if not (obj.ReferenceEquals(v, newState)) then
              newState <- v
              inner.UserState <- map inner.UserState v
              inner.UpdateStateTag()
        member x.UpdateStateTag () = inner.UpdateStateTag()
        member x.Seek newPos = inner.Seek newPos
        member x.Peek () = inner.Peek ()
        member x.PeekFull () = inner.PeekFull ()
        member x.IsEnd = inner.IsEnd
        member x.Skip () = inner.Skip()
        member x.SkipFull () = inner.SkipFull()
        member x.SkipAndPeek () = inner.SkipAndPeek()
        member x.SkipAndPeekFull () = inner.SkipAndPeekFull()
        member x.Read () = inner.Read()
        member x.ReadFull () = inner.ReadFull()
        member x.Argv = inner.Argv } 

type ArgumentParser<'TUserState, 'TResult> = IArgumentStream<'TUserState> -> Reply<'TResult>

module ArgParser =
  let preturn x : ArgumentParser<_,_> = fun stream -> Reply(x)
  let pzero : ArgumentParser<_,_> = fun stream -> Reply(Error, FParsec.Error.NoErrorMessages)

  let (>>=) (p: ArgumentParser<'u, 'a>) (f: 'a -> ArgumentParser<'u, 'b>) : ArgumentParser<_,_> =
    fun stream ->
        let reply1 = p stream
        if reply1.Status = Ok then
            let p2 = f reply1.Result
            let stateTag = stream.StateTag
            let mutable reply2 = p2 stream
            if stateTag = stream.StateTag && reply2.Status = Error then
                reply2.Error <- mergeErrors reply1.Error reply2.Error
            reply2
        else
            Reply(reply1.Status, reply1.Error)
  let (>>%) p x = p >>= fun _ -> preturn x          
  let (>>.) p1 p2 = p1 >>= fun _ -> p2
  let (.>>) p1 p2 = p1 >>= fun x -> p2 >>% x
  let (.>>.) p1 p2 = p1 >>= fun a -> p2 >>= fun b -> preturn (a, b)
  let between (popen: ArgumentParser<_,'u>) (pclose: ArgumentParser<'u,_>) (p: ArgumentParser<'u,_>) =
     popen >>. p .>> pclose
  let (|>>) p f = p >>= fun x -> preturn (f x)  


  let (<?>) (p: ArgumentParser<'u,'a>) label  : ArgumentParser<'u,'a> =
      let error = expected label
      fun stream ->
          let stateTag = stream.StateTag
          let mutable reply = p stream
          if stateTag = stream.StateTag then
              reply.Error <- error
          reply

  let choiceBest (ps : seq<ArgumentParser<'u, 'a>>) : ArgumentParser<_, _> =
    fun (stream:IArgumentStream<_>) ->
       let results =
         ps
         |> Seq.map (fun p -> async {
           let cpStream = ArgumentStream.clone stream
           let reply = p cpStream 
           return cpStream, reply
         })
         |> Async.Parallel
         |> Async.RunSynchronously
       let maxArgLength = (Seq.append stream.Argv [""] |> Seq.maxBy (fun (arg:string) -> arg.Length)).Length
       let mutable (bestStream, bestResult) =
          results
          |> Seq.maxBy (fun (stream, reply) ->
            if reply.Status <> Ok
            then -stream.Argv.Length - 1 + stream.Position.ArgIndex, -maxArgLength - 1 + stream.Position.InnerIndex
            else stream.Position.ArgIndex, stream.Position.InnerIndex)   
       let reply =
         if bestResult.Status <> Ok then
            let errors =
              results
              |> Seq.fold (fun (errors:ErrorMessageList) (_, reply) -> mergeErrors errors reply.Error) NoErrorMessages
            bestResult.Error <- errors
            bestResult
         else
            bestResult

       while stream.Position <> bestStream.Position do stream.Read() |> ignore
       stream.UserState <- bestStream.UserState
       reply

  let choice (ps : seq<ArgumentParser<'u, 'a>>) : ArgumentParser<_, _> =
    fun (stream:IArgumentStream<_>) ->
       use iter = ps.GetEnumerator()
       if iter.MoveNext() then
           let state = stream.CurrentState 
           let stateTag = stream.StateTag
           let mutable error = NoErrorMessages
           let mutable reply = iter.Current stream
           while reply.Status = Error && iter.MoveNext() do
               if stateTag <> stream.StateTag then
                  stream.RestoreState state
               error <- mergeErrors error reply.Error
               reply <- iter.Current stream
           if stateTag = stream.StateTag && reply.Status = Error then
               error <- mergeErrors error reply.Error
               reply.Error <- error
           reply
       else Reply()

  let (<|>) p1 p2 = choice [p1;p2]


  [<Sealed>]
  type Inline =
    [<NoDynamicInvocation>]
    static member inline
                       Many(stateFromFirstElement,
                            foldState,
                            resultFromState,
                            elementParser: ArgumentParser<_,_>,
                            ?firstElementParser: ArgumentParser<_,_>,
                            ?resultForEmptySequence) : ArgumentParser<_,_> =
      fun stream ->
        let mutable stateTag = stream.StateTag
        let firstElementParser = match firstElementParser with Some p -> p | _ -> elementParser
        let mutable reply = firstElementParser stream
        if reply.Status = Ok then
            let mutable xs = stateFromFirstElement reply.Result
            let mutable error = reply.Error
            stateTag <- stream.StateTag
            reply <- elementParser stream
            while reply.Status = Ok (*&& stateTag <> stream.StateTag*) do
                if stateTag = stream.StateTag then
                    failwithf "infiniteLoopException %O" stream
                xs    <- foldState xs reply.Result
                error <- reply.Error
                stateTag <- stream.StateTag
                reply <- elementParser stream
            if reply.Status = Error && stateTag = stream.StateTag then
                error <- mergeErrors error reply.Error
                Reply(Ok, resultFromState xs, error)
            else
                error <- if stateTag <> stream.StateTag then reply.Error
                         else mergeErrors error reply.Error
                Reply(reply.Status, error)
        else
            match resultForEmptySequence with
            | Some _ (* if we bind f here, fsc won't be able to inline it *)
              when reply.Status = Error && stateTag = stream.StateTag ->
                Reply(Ok, (match resultForEmptySequence with Some f -> f() | _ -> Unchecked.defaultof<_>), reply.Error)
            | _ ->
                Reply(reply.Status, reply.Error)

  let many p = Inline.Many((fun x -> [x]), (fun xs x -> x::xs), List.rev, p, resultForEmptySequence = fun () -> [])
  let many1 p = Inline.Many((fun x -> [x]), (fun xs x -> x::xs), List.rev, p)

  let pseq (ps : seq<ArgumentParser<_, _>>) : ArgumentParser<_, _> =
    Seq.fold (>>.) (preturn Map.empty) ps
  type internal UnorderedState<'u, 'a> =
    { InnerState : 'u 
      AppliedParsers : int list }
 
  let internal mapParserToUnorderedState i (p:ArgumentParser<'u, _>) : ArgumentParser<UnorderedState<'u, _>, _> =
    fun innerStream ->
       let oldStream = 
         ArgumentStream.map 
           (innerStream.UserState.InnerState)
           (fun unorderd s ->  { unorderd with InnerState = s })
           innerStream
       let reply = p oldStream
       if reply.Status = Ok then innerStream.UserState <- { innerStream.UserState with AppliedParsers = i :: innerStream.UserState.AppliedParsers }
       reply
  let punorderedseqWithMany allowEmpty allowMissing (ps : seq<bool * ArgumentParser<'u, 'a>>) : ArgumentParser<'u, _> =
    fun (stream:IArgumentStream<_>) ->
       let newStream =
         ArgumentStream.map 
           { InnerState = stream.UserState; AppliedParsers = [] }
           (fun _ s -> s.InnerState)
           stream   
       let allParsers =
          ps
          |> Seq.mapi (fun i (allowMultiple, p) -> allowMultiple, mapParserToUnorderedState i p)
          |> Seq.toList
       let mutable availableParsers = allParsers
       let mutable reply = Reply(Unchecked.defaultof<'a>)
       let mutable results = []
       while reply.Status = ReplyStatus.Ok && availableParsers.Length > 0 do
          reply <- choice (availableParsers |> Seq.map snd) newStream
          if (reply.Status = ReplyStatus.Ok) then
            results <- reply.Result :: results
          availableParsers <-
            allParsers
            |> List.mapi (fun i p -> i, p)
            |> List.filter(fun (i, (allowMultiple, _)) -> allowMultiple || not (List.exists (fun applied -> applied = i) newStream.UserState.AppliedParsers))
            |> List.map snd
       
       if reply.Status = Error && not allowMissing then
         Reply(reply.Status, reply.Error)
       else
         if not allowEmpty && results = [] then
           Reply(reply.Status, reply.Error)
         else
           Reply(results)

  let punorderedseq allowEmpty allowMissing (ps : seq<ArgumentParser<'u, 'a>>) : ArgumentParser<'u, _> =
    punorderedseqWithMany allowEmpty allowMissing (ps |> Seq.map (fun p -> false, p))
  let chooseParser itemType chooser =
    fun (stream:IArgumentStream<_>) ->
        match chooser (stream.Peek()) with
        | Some result ->
          stream.Skip()
          Reply(result)
        | None ->
          let e1 = expected itemType
          let e2 = unexpected (sprintf "%s" (stream.PeekFull() |> printSOption))
          let error = mergeErrors e1 e2
          Reply(ReplyStatus.Error, error)

  let chooseParserFull itemType chooser =
    fun (stream:IArgumentStream<_>) ->
        match chooser (stream.PeekFull()) with
        | Some result ->
          stream.SkipFull()
          Reply(result)
        | None ->
          let e1 = expected itemType
          let e2 = unexpected (sprintf "%s" (stream.PeekFull() |> printSOption))
          let error = mergeErrors e1 e2
          Reply(ReplyStatus.Error, error)

  let chooseParser' itemType chooser =
    let choose arg =
      match arg with
      | Some a -> chooser a
      | None -> None
    chooseParser itemType choose
  let chooseParserFull' itemType chooser =
    let choose arg =
      match arg with
      | Some a -> chooser a
      | None -> None
    chooseParserFull itemType choose


  let pcmd cmd =
      let chooseCmd arg =
        if arg = cmd then Some cmd else None
      chooseParserFull' (sprintf "Command '%s'" cmd) chooseCmd

  let parg argName = chooseParserFull (sprintf "Argument for  '%s'" argName) id

  let updateUserState (map':'a -> DocoptMap -> DocoptMap) : 'a -> ArgumentParser<DocoptMap, DocoptMap> =
      fun arg' ->
        fun stream' ->
          let res = map' arg' stream'.UserState
          stream'.UserState <- res
          Reply(res)

  let debug (map':'a -> IArgumentStream<'state> -> unit) : 'a -> ArgumentParser<'state, 'a> =
      fun arg' ->
        fun stream' ->
          map' arg' stream'
          Reply(arg')

  let updateMap key newItem map =
      match Map.tryFind key map, newItem with
      | None, _
      | Some DocoptResult.NoResult, _ -> Map.add key newItem map
      | _, DocoptResult.NoResult -> map
      | Some (DocoptResult.Argument arg1), DocoptResult.Argument arg2 ->
          Map.add key (DocoptResult.Arguments [arg1; arg2]) map
      | Some (DocoptResult.Argument arg1), DocoptResult.Arguments argList ->
          Map.add key (DocoptResult.Arguments (arg1 :: argList)) map
      | Some (DocoptResult.Arguments argList1), DocoptResult.Argument arg2 ->
          Map.add key (DocoptResult.Arguments (argList1 @ [arg2])) map
      | Some (DocoptResult.Arguments argList1),  DocoptResult.Arguments argList2 ->
          Map.add key (DocoptResult.Arguments (argList1 @ argList2)) map
      | Some (DocoptResult.Flag), DocoptResult.Flag ->
          Map.add key (DocoptResult.Flags 2) map
      | Some (DocoptResult.Flags n1), DocoptResult.Flag ->
          Map.add key (DocoptResult.Flags (n1 + 1)) map
      | Some (DocoptResult.Flag), DocoptResult.Flags n2 ->
          Map.add key (DocoptResult.Flags (n2 + 1)) map
      | Some (DocoptResult.Flags n1), DocoptResult.Flags n2 ->
          Map.add key (DocoptResult.Flags (n1 + n2)) map
      | Some v, _ -> failwithf "Cannot add value %O as %s -> %O already exists in the result map" newItem key v

  let saveInMap key f = 
    updateUserState (fun item map ->
      let newItem = f item
      updateMap key newItem map)

  let saveInMapM keys f = 
    updateUserState (fun item map ->
      let newItem = f item
      keys |> Seq.fold (fun map key -> updateMap key newItem map) map)


  let multipleSaveInMap f =
     updateUserState (fun item map ->
      f item |> Seq.fold (fun map (key, newItem) -> updateMap key newItem map) map)
  
  let mergeMap m1 m2 =
    Map.fold (fun s k v -> Map.add k v s) m1 m2
  let mergeMaps maps =
    Seq.fold mergeMap Map.empty maps


  let pLongFlag (flag:SafeOption) =
    if not flag.IsLong then failwithf "Cannot parse empty short flag %O" flag
    let keys =
      [ if flag.IsShort then yield flag.FullShort
        if flag.IsLong then yield flag.FullLong ]
    let single =
      let chooseCmd arg =
        match arg with
        | Some arg when arg = flag.FullLong -> Some arg
        | _ -> None
      chooseParserFull (sprintf "Flag '%s'" flag.FullLong) chooseCmd
    if flag.HasArgument then
      let chooseCmd arg =
        match arg with
        | Some (arg:string) when arg.StartsWith (flag.FullLong + "=") -> Some (arg.Substring (flag.FullLong.Length + 1))
        | _ -> None
      chooseParserFull (sprintf "Flag '%s='" flag.FullLong) chooseCmd <|> (single >>. parg flag.FullLong)
      >>= saveInMapM keys (DocoptResult.Argument)
    else
      single
      >>= saveInMapM keys (fun _ -> DocoptResult.Flag)


  let pShortFlag (flag : SafeOption) =
    if not flag.IsShort then failwithf "Cannot parse empty short flag %O" flag
    let keys =
      [ if flag.IsShort then yield flag.FullShort
        if flag.IsLong then yield flag.FullLong ]
    if flag.HasArgument then
      // When we have a argument we know we can consume the complete argument
      let chooseCmd (stream:IArgumentStream<_>) =
        match stream.Peek(), stream.PeekFull() with
        | Some (arg:string), Some (fullarg) when arg.StartsWith flag.FullShort ->
          let oldPos = stream.Position
          stream.SkipFull()
          let result =            
            match oldPos with
            | ShortArgumentPartialPos(_, i) when i + 1 < fullarg.Length ->
               // Parameter for short switch is in current argument
               Some (fullarg.Substring(i + 1))
            | _ -> None
          Reply(result)
        | _ ->
          let e1 = expected (sprintf "ShortFlag '%s'" flag.FullShort)
          let e2 = unexpected (sprintf "%s" (stream.PeekFull() |> printSOption))
          let error = mergeErrors e1 e2
          Reply(ReplyStatus.Error, error)
        //match arg with
        //| Some (arg:string) when arg.StartsWith flag.FullShort ->
        //  if arg = flag.FullShort then
        //    Some (None)
        //  else Some (Some (arg.Substring (flag.FullShort.Length)))
        //| _ -> None
       
      chooseCmd
      >>= (function
          | Some arg -> preturn arg
          | None -> parg flag.FullShort)
      >>= saveInMapM keys (DocoptResult.Argument)
    else
      let chooseCmd arg =
        match arg with
        | Some (arg:string) when arg = flag.FullShort -> Some arg
        | _ -> None
      
      chooseParser (sprintf "ShortFlag '%s'" flag.FullShort) chooseCmd
      >>= saveInMapM keys (fun _ -> DocoptResult.Flag)

  let pOption includeShort (o' : SafeOption) =
    let longArg =
      if o'.IsLong then pLongFlag o'
      else pzero
    if includeShort && o'.IsShort then
      //let short = pShortOptionsWithSave (SafeOptions [o'])
      longArg <|> pShortFlag o'
    else longArg

  let pOptions allowMissing (flags : SafeOptions) =
    let optionParsers = flags |> Seq.map (fun flag -> flag.AllowMultiple, pOption true flag) |> Seq.toList
    optionParsers
    |> punorderedseqWithMany false allowMissing
    >>= updateUserState (fun _ state -> state)

  let rec getParser (ast:UsageAst) : ArgumentParser<_, _> =
    let p = 
      match ast with
      | UsageAst.Eps -> preturn Map.empty
      | UsageAst.Ano (_, o') ->
        // Annotations are always optional
        pOptions true o' <|> preturn Map.empty
        //pzero <?> "Option annotation is not supported yet"
        //CharParsers.
      | UsageAst.Sop o' -> pOptions false o'
        //o'.
        //pzero <?> "Short options are not supported yet"
      | UsageAst.Lop o' -> pOption true o'
      | UsageAst.Sqb (UsageAst.Seq asts') when ast.ContainsOnlyOptions ->
        asts'
        |> Seq.map getParser
        |> Seq.toList
        |> punorderedseq false true
        >>= updateUserState (fun _ state -> state)
      | UsageAst.Sqb (UsageAst.Sop o') -> pOptions true o' <|> preturn Map.empty
      | UsageAst.Sqb ast' ->
        getParser ast' <|> preturn Map.empty
      | UsageAst.Arg name' ->
        parg name'
        >>= saveInMap (name') (DocoptResult.Argument)
      | UsageAst.XorEmpty -> preturn Map.empty
      | UsageAst.Xor (l', r') ->
        choiceBest [ getParser l'; getParser r' ]
      | UsageAst.Seq asts' when ast.ContainsOnlyOptions ->
        asts'
        |> Seq.map getParser
        |> Seq.toList
        |> punorderedseq false false
        >>= updateUserState (fun _ state -> state)
      | UsageAst.Req ast' ->
        getParser ast'
      | UsageAst.Seq asts' ->
        asts'
        |> Seq.map getParser
        |> pseq
      | UsageAst.Cmd cmd' -> 
        pcmd cmd'
        >>= saveInMap cmd' (fun _ -> DocoptResult.Flag)
      | UsageAst.Ell (UsageAst.Sqb ast') ->
        // Allow zero matches
        many (getParser ast')
        >>= updateUserState (fun _ state -> state)
      | UsageAst.Ell ast' ->
        // One or more
        many1 (getParser ast')
        >>= updateUserState (fun _ state -> state)
      | UsageAst.Sdh ->
        pcmd "-"
        >>= saveInMap "-" (fun _ -> DocoptResult.Flag)
    //(debug (fun _ stream ->
    //  printfn ">>>> STARTING ast %A, state: %A" ast stream) ())
    //>>.
    p      
    //>>= debug (fun result stream ->
    //  printfn ">>>> FINISHED ast %A, state: %A, result: %A" ast stream result)
type UsageParser(usageStrings':string array, sections:(string * SafeOptions) list) =
    //let opts' = sections.["options"]
    //let mutable isAno = false
    //let toIAst obj' = (# "" obj' : UsageAst #) // maybe #IAst instead of IAst
    let updateUserState (map':'a -> UsageAstCell -> UsageAstCell) : 'a -> Parser<UsageAstCell, UsageAstCell> =
      fun arg' ->
        fun stream' ->
          let res = map' arg' stream'.UserState in
          stream'.UserState <- res;
          Reply(res)
    let isLetterOrDigit c' = isLetter(c') || isDigit(c')
    let opp = OperatorPrecedenceParser<UsageAstCell, _, UsageAstCell>()
    let pupperArg =
      let start c' = isUpper c' || isDigit c' in
      let cont c' = start c' || c' = '-' in
      identifier (IdentifierOptions(isAsciiIdStart=start,
                                    isAsciiIdContinue=cont,
                                    label="UPPER-CASE identifier"))
    let plowerArg =
      satisfyL (( = ) '<') "<lower-case> identifier"
      >>. many1SatisfyL (( <> ) '>') "any character except '>'"
      .>> skipChar '>'
      |>> (fun name' -> String.Concat("<", name', ">"))
    let parg =
      let filterArg arg' (last':UsageAstCell) =
        if obj.ReferenceEquals(null, last')
        then UsageAstBuilder.Arg(arg')
        elif (match last'.Content with Some (UsageAstBuilder.Sop opts) -> opts.Last.HasArgument | _ -> false)
             || (match last'.Content with Some(UsageAstBuilder.Lop opt) -> opt.HasArgument | _ ->  false)
        then UsageAstBuilder.Eps
        else UsageAstBuilder.Arg(arg')
        |> UsageAstCell.FromBuilder
      in pupperArg <|> plowerArg
         >>= updateUserState filterArg
    let pano (title, so:SafeOptions) =
      skipString (sprintf "[%s]" title)
      >>= updateUserState (fun _ _ -> UsageAstBuilder.Ano(title, so) |> UsageAstBuilder.ToCell)
    let psdh = skipString "[-]"
               >>= updateUserState (fun _ _ -> UsageAstBuilder.Sqb (UsageAstBuilder.Sdh |> UsageAstBuilder.ToCell)|> UsageAstBuilder.ToCell)
    let psop = let filterSops (sops':string) (last':UsageAstCell) =
                 let sops = ResizeArray<SafeOption>() in
                 let mutable i = -1 in
                 while (i <- i + 1; i < sops'.Length) do
                   //sops.Add({SafeOption.Empty with Short = Some sops'.[i] })
                   match sections |> Seq.tryPick (fun (_, opts) -> opts.Find(sops'.[i])) with
                   //match opts'.Find(sops'.[i]) with
                   | None -> sops.Add({SafeOption.Empty with Short = Some sops'.[i] })
                   | Some opt ->
                      (if opt.HasArgument && i + 1 < sops'.Length
                       then i <- sops'.Length);
                       sops.Add(opt)
                 done;
                 if sops.Count = 0
                 then UsageAstBuilder.Eps
                 else match last'.Content with
                      | Some (UsageAstBuilder.Sop list) ->
                        last'.Content <- Some (UsageAstBuilder.Sop (list.AddRange(sops |> List.ofSeq)))
                        UsageAstBuilder.Eps
                      | _ -> UsageAstBuilder.Sop(SafeOptions(sops |> Seq.toList))
                |> UsageAstCell.FromBuilder
               in skipChar '-'
                  >>. many1SatisfyL ( isLetterOrDigit ) "Short option(s)"
                  >>= updateUserState filterSops
    let plop =
      let filterLopt (lopt':string, arg':string Option) _ =
        //UsageAstBuilder.Lop({SafeOption.Empty with Long = Some lopt'; ArgumentName = arg'})
        match sections |> Seq.tryPick (fun (_, opts) -> opts.Find(lopt')) with
        //match opts'.Find(lopt') with
        | None -> UsageAstBuilder.Lop({SafeOption.Empty with Long = Some lopt'; ArgumentName = arg'})
        | Some lopt -> UsageAstBuilder.Lop(lopt)
        |> UsageAstCell.FromBuilder
      in skipString "--"
         >>. manySatisfy (fun c' -> Char.IsLetterOrDigit(c') || c' = '-')
        .>>. opt (skipChar '=' >>. (plowerArg <|> pupperArg))
         >>= updateUserState filterLopt
    let psqb = between (skipChar '[' >>. spaces) (skipChar ']')
                       opp.ExpressionParser
               >>= updateUserState (fun ast' _ -> UsageAstBuilder.Sqb(ast')|> UsageAstCell.FromBuilder)
    let preq = between (skipChar '(' >>. spaces) (skipChar ')')
                       opp.ExpressionParser
               >>= updateUserState (fun ast' _ -> UsageAstBuilder.Req(ast')|> UsageAstCell.FromBuilder)
    let pcmd = many1Satisfy (fun c' -> isLetter(c') || isDigit(c') || c' = '-')
               >>= updateUserState (fun cmd' _ -> UsageAstBuilder.Cmd(cmd')|> UsageAstCell.FromBuilder)
    let panoParsers = sections |> List.map pano
    let term = choice (Seq.append panoParsers [|
                        psdh;
                        plop;
                        psop;
                        psqb;
                        preq;
                        parg;
                        pcmd|])
    let pxor = let afterStringParser =
                 spaces
                 .>> updateUserState (fun _ _ -> UsageAstBuilder.XorEmpty|> UsageAstCell.FromBuilder) ()
               in InfixOperator("|", afterStringParser, 10, Associativity.Left,
                                fun x' y' -> UsageAstBuilder.Xor(x', y') |> UsageAstCell.FromBuilder)
    let pell = let afterStringParser =
                 spaces .>> updateUserState (fun _ _ -> UsageAstBuilder.Ell(UsageAstBuilder.Eps|> UsageAstCell.FromBuilder)|> UsageAstCell.FromBuilder) ()
               let makeEll (ast':UsageAstCell) =
                 match ast'.Content with
                 | Some (UsageAstBuilder.Seq seq) -> 
                    let cell = seq |> List.last
                    cell.Content <-                     
                      match cell.Content with
                      | Some c -> 
                        Some (UsageAstBuilder.Ell (UsageAstCell.FromBuilder c))
                      | None -> Some <| UsageAstBuilder.Ell(UsageAstBuilder.Eps|> UsageAstCell.FromBuilder)           
                    ast'
                 | _       -> UsageAstBuilder.Ell(ast')|> UsageAstCell.FromBuilder
               in PostfixOperator("...", afterStringParser, 20, false, makeEll)
    let _ = 
      opp.TermParser <-
        sepEndBy1 term spaces1
        >>= updateUserState (fun ast' _ ->
                               match ast' |> List.filter (fun ast' -> ast'.Content.IsSome && ast'.Content.Value.UsageTag <> Tag.Eps) with
                               | []    -> UsageAstBuilder.Eps|> UsageAstCell.FromBuilder
                               | [ast] -> ast
                               | list  -> UsageAstBuilder.Seq(list)|> UsageAstCell.FromBuilder
                              )
    let _ = opp.AddOperator(pxor)
    let _ = opp.AddOperator(pell)
    let pusageLine = spaces >>. opp.ExpressionParser

    let parseAsync = function
    | ""   -> async { return UsageAst.Eps }
    | line -> async {
        let line = line.TrimStart() in
        let index = line.IndexOfAny([|' ';'\t'|]) in
        return if index = -1 then UsageAst.Eps
               else let line = line.Substring(index) in
                    match runParserOnString pusageLine { Content = None } "" line with
                    | Success(ast, _, _) -> ast.Build()
                    | Failure(err, _, _) -> raise (UsageException(err))
      }

    do if usageStrings'.Length = 0 || usageStrings' |> Seq.forall (String.IsNullOrWhiteSpace) then failwithf "Not given any usage-formats"
    let asts =
      usageStrings'
      |> Array.map parseAsync
      |> Async.Parallel
      |> Async.RunSynchronously

    let pAstParser =
      let (>>.) = ArgParser.(>>.)
      let (>>=) = ArgParser.(>>=)  
      asts
      |> Seq.map ArgParser.getParser
      |> ArgParser.choiceBest
      >>= ArgParser.updateUserState (fun _ state -> state)
      
    member __.ParseCommandLine (argv) =
      let state = ArgumentStream.create argv Map.empty
      let reply = pAstParser state
      let errors = ErrorMessageList.ToSortedArray(reply.Error)
      let argIdx = int64 state.Position.ArgIndex
      let parseError = ParserError(Position("argv", argIdx,0L,argIdx), state.UserState, reply.Error)
      let errorText =
        use sw = new System.IO.StringWriter()
        parseError.WriteTo(sw)
        sw.ToString()
        |> Helpers.improveErrorText 0L argIdx (if argIdx >= 0L && int argIdx < argv.Length then argv.[int argIdx] else "<>")
            
      match reply.Status = ReplyStatus.Ok, errors, state.IsEnd with
      | true, [||], true -> reply.Result
      | _, _ , true -> raise <| DocoptException (sprintf "errors %s: %s" (printReplyStatus reply.Status) errorText)
      | _, [||], false ->
          let unparsed = argv.[state.Position.ArgIndex..argv.Length - 1]
          raise <| DocoptException (sprintf "'[|%s|]' could not be parsed" (System.String.Join(";", unparsed :> _ seq)))
      | _ ->
          let unparsed = argv.[state.Position.ArgIndex..argv.Length - 1]
          raise <| DocoptException (sprintf "errors: %s, ('[|%s|]' could not be parsed)" errorText (System.String.Join(";", unparsed :> _ seq)))

    member __.Asts = asts
