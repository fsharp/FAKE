﻿module FsCheck.Fake.TestParallelBuildOrder

open System
open System.Collections.Generic
open global.Xunit
open Fake
open FsCheck

let inline (==>) a b = global.Fake.AdditionalSyntax.(==>) a b

let (|Target|) (t : Target) =
    Target t.Name

let (|TargetSet|) (t : seq<Target>) =
    let list = t |> Seq.map (fun t -> t.Name) |> Seq.sort |> Seq.toList
    TargetSet list

// checks whether the given order is consistent with all dependencies
let validateBuildOrder (order : list<Target[]>) (rootTarget : string) =
    let rootTarget = getTarget rootTarget

    // store a "level" for all targets in order
    let targetLevelMap = 
        order |> List.mapi (fun i arr ->
                arr |> Array.map (fun t -> t.Name, i)
              )
              |> Seq.concat
              |> Map.ofSeq

    // check whether the assigned level is smaller or equal to
    // the given max level.
    let checkLevel (target : Target) (maxLevel : int) =
        match Map.tryFind target.Name targetLevelMap with
            | Some level ->
                if level > maxLevel then
                    failwithf "found target on unexpected level %d (should be at most %d)" level maxLevel

            | None ->
                failwithf "target %A was not assigned a level but occurs in the dependency tree" target.Name
    
    // recursively validate the target levels
    let rec validate fDeps (t : Target) (maxLevel : int) =
        checkLevel t maxLevel
        let realLevel = Map.find t.Name targetLevelMap

        let deps = fDeps t|> List.map getTarget
        for d in deps do
            validate fDeps d (realLevel - 1)

    // initially the max-level is unbounded
    validate (fun t -> t.Dependencies) rootTarget Int32.MaxValue
    validate (fun t -> t.SoftDependencies) rootTarget Int32.MaxValue

[<Fact>]
let ``Independent targets are parallel``() =
    TargetDict.Clear()

    Target "a" DoNothing
    Target "b" DoNothing
    Target "c" DoNothing
    
    Target "dep" DoNothing

    "a" ==> "dep" |> ignore
    "b" ==> "dep" |> ignore
    "c" ==> "dep" |> ignore

    let order = determineBuildOrder "dep"

    validateBuildOrder order "dep"

    match order with
        | [TargetSet ["a"; "b"; "c"]; [|Target "dep"|]] ->
            // as expected
            ()

        | _ ->
            failwithf "inconsitent order: %A" order


    ()

[<Fact>]
let ``Ordering maintains dependencies``() =
    let r = Random()

    for iter in 1..10 do
        TargetDict.Clear()
        Target "final" DoNothing

        let targetCount = r.Next 30 + 10
        let dependencyCount = r.Next(3 * targetCount)

        // define some targets and introduce a dependency
        // to some final target.
        for c in 0..targetCount-1 do
            Target (string c) DoNothing
            string c ==> "final" |> ignore

        // add a number of dependencies between two
        // random targets. By adding dependencies from
        // the lower index to the higher one we ensure that
        // the resulting graph remains acyclic
        for i in 0..dependencyCount-1 do
            let a = r.Next targetCount
            let b = r.Next targetCount

            if a <> b then
                // determine l(ow) and h(igh) and add the dependency
                let l,h = if a < b then a,b else b, a
                string l ==> string h |> ignore



        let order = determineBuildOrder "final"
        validateBuildOrder order "final"

[<Fact>]
let ``Diamonds are resolved correctly``() =
    TargetDict.Clear()
    Target "a" DoNothing
    Target "b" DoNothing
    Target "c" DoNothing
    Target "d" DoNothing

    // create a diamond graph
    "a" ==> "b" ==> "d" |> ignore
    "a" ==> "c" ==> "d" |> ignore

    let order = determineBuildOrder "d"
    validateBuildOrder order "d"

    match order with
        | [[|Target "a"|];TargetSet ["b"; "c"];[|Target "d"|]] ->
            // as expected
            ()

        | _ ->
            failwithf "unexpected order: %A" order


[<Fact>]
let ``Soft dependencies are respected when dependees are present``() = 
    TargetDict.Clear()
    Target "a" DoNothing
    Target "b" DoNothing
    Target "c" DoNothing
    Target "d" DoNothing
    Target "e" DoNothing
    Target "f" DoNothing
    
   
    "a" ==> "b" ==> "c" |> ignore
    // d does not depend on c, but if something else forces c to run, then d must come after c.
    "d" <=? "c" |> ignore

    // Running f will run  c, d, and f.  The soft dependency of d on c means that c must run first.
    "d" ==> "f" |> ignore
    "e" ==> "f" |> ignore
    "c" ==> "f" |> ignore

    let order = determineBuildOrder "f"

    validateBuildOrder order "f"

    match order with
        | [[|Target "a"|];TargetSet ["b";];[|Target "c"|];TargetSet ["d";"e"];[|Target "f"|]] ->
            // as expected
            ()

        | _ ->
            failwithf "unexpected order: %A" order
    ()



[<Fact>]
let ``Soft dependencies are ignored when dependees are not present``() = 
    TargetDict.Clear()
    Target "a" DoNothing
    Target "b" DoNothing
    Target "c" DoNothing
    Target "d" DoNothing
    Target "e" DoNothing
    
   
    "a" ==> "b" ==> "c" |> ignore
    // d does not depend on c, but if something else forces c to run, then d must come after c.
    "c" ?=> "d" |> ignore
    
    // Running e will not run c, due to soft dependency
    "d" ==> "e" |> ignore
    "b" ==> "e" |> ignore

    let order = determineBuildOrder "e"

    validateBuildOrder order "e"

    match order with
        | [[|Target "a"|];TargetSet ["b";"d"];[|Target "e"|]] ->
            // as expected
            ()

        | _ ->
            failwithf "unexpected order: %A" order
    ()