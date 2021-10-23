﻿module Floorplan

open Reservations

let currentVenue = 1

open System
open System.Collections
open System.Collections.Generic
open System.IO
open System.Xml
open System.Xml.Linq
open System.Linq
open DredgeFramework
open Dapper
open Dapper.FSharp
open Dapper.FSharp.MySQL
open MySql.Data.MySqlClient
open Thoth.Json.Net

[<CLIMutable>]
type floorplan_table_shape = { table_number: int; shape: string; width: int; height: int; rotation: int}
[<CLIMutable>]
type floorplan_table_transform = { table_number: int; pos_x: int; pos_y: int; width: int; height: int; rotation: int}


[<CLIMutable>]
type floorplan_table = {
    table_number: int
    room_id: int
    venue_id: int
    pos_x: int
    pos_y: int
    shape: string
    width: int
    height: int
    default_covers: int
    rotation: int
    merged_children: string
    previous_state: string
    status: string
    table_id: int
}

[<CLIMutable>]
type floorplan_room = {
    room_id: int
    room_name: string
    background_image: string
    venue_id: int
}

let floorplan_table_decoder : Decoder<floorplan_table> =
        Decode.object
            (fun get ->
                {
                    table_number = get.Required.Field "table_number" Decode.int
                    room_id = get.Required.Field "room_id" Decode.int
                    venue_id = get.Required.Field "venue_id" Decode.int
                    pos_x = get.Required.Field "pos_x" Decode.int
                    pos_y = get.Required.Field "pos_y" Decode.int
                    shape = get.Required.Field "shape" Decode.string
                    width = get.Required.Field "width" Decode.int
                    height = get.Required.Field "height" Decode.int
                    default_covers = get.Required.Field "default_covers" Decode.int
                    rotation = get.Required.Field "rotation" Decode.int
                    merged_children = get.Required.Field "merged_children" Decode.string
                    previous_state = get.Required.Field "previous_state" Decode.string
                    status = get.Required.Field "status" Decode.string
                    table_id = get.Required.Field "table_id" Decode.int
                })



let activeTablePath = "tables/active/"

let getTableFile (tableNumber: int) =
    let tableNumberString = tableNumber |> string
    activeTablePath + "table" + tableNumberString + ".table"

let tableIsOpen (tableNumber: int) =
    let tableFile = getTableFile tableNumber
    File.Exists tableFile

let fileNameToTableNumber (fileName: string) = //Takes a file name for a floorplan table and returns the table number
    if fileName.Contains ".table" then
        let fileName = (fileName.Split ".").[0]
        (fileName.Split "/table").[1] |> int
    else 0

let openTables = //Get a list of all open tables.
    let tableList = Directory.GetFiles(activeTablePath)

    tableList
        |> Array.map fileNameToTableNumber
        |> Array.filter(fun tableNumber -> tableNumber > 0)

let getTableNumber table = table.table_number

let tablesInRoom (roomId: int) = //Get a list of all tables in a particular room.
    select {
        table "floorplan_tables"
        where (eq "room_id" roomId)
    }
    |> db.Select<floorplan_table>



let getActiveTables (venueId: int) =
    select{
        table "floorplan_tables"
        where (eq "venue_id" venueId)
    }
    |> db.Select
    |> Array.map getTableNumber
    |> Array.filter tableIsOpen

let openNewTable tableNumber = //Create a new table file pre-populated with skeleton data
    let tableFile = getTableFile tableNumber
    if not <| File.Exists tableFile then
        let newTableTemplate = $"""
             <table openedby="" number="{tableNumber}"></table>
        """
        File.Create tableFile |> ignore
        File.WriteAllText (tableFile, newTableTemplate)

let transferTable origin destination = //Transfers a table from one to another
    let originFile = getTableFile origin
    let destinationFile = getTableFile destination

    if tableIsOpen origin then
        (* If the destination is not an already open table,
        then we simply have to rename the origin to destination *)
        if not <| tableIsOpen destination then
            let content = File.ReadAllText originFile
            let newContent = content.Replace($"number=\"{origin|>string}\">", $"number=\"{destination|>string}\">")
            File.WriteAllText(originFile, newContent)
            File.Move(originFile, destinationFile)
        else
            let originXML = XDocument.Load originFile
            let destinationXML = XDocument.Load destinationFile
            destinationXML.Root.Add(originXML.Root.Elements())
            destinationXML.Save destinationFile
            File.Delete originFile

let saveOrderToTable orderXML tableNumber =
    let tableFile = getTableFile tableNumber
    let tableXML = tableFile
                   |> File.ReadAllText
                   |> StringReplace "</table>" (orderXML + "</table>")

    File.WriteAllText(tableFile, tableXML)

let getTable (tableNumber : int) =
    select {
        table "floorplan_tables"
        where (eq "table_number" tableNumber + eq "venue_id" currentVenue)
    }
      |> db.Select<floorplan_table>
      |> first

let getRoom (roomId: int) =
    select {
        table "floorplan_rooms"
        where (eq "room_id" roomId)
    } |> db.Select<floorplan_room> |> first

let getRoomList (venueId: int) =
    select {
        table "floorplan_rooms"
        where (eq "venue_id" venueId)
    }  |> db.Select<floorplan_room>

let updateFloorplanTable (tableNumber:int) (column: string) value =
    let sql = "Update floorplan_tables Set @column = @value Where table_number = @tableNumber"
    let parameters = [("column", box column); ("value", box value); ("tableNumber", box tableNumber)]
    db.connection.Execute(sql, parameters) |> ignore

    getTable tableNumber

let updateTableShape (floorplanTable: floorplan_table_shape)  =
    update {
        table "floorplan_tables"
        set floorplanTable
        where (eq "table_number" floorplanTable.table_number + eq "venue_id" currentVenue)
    } |> db.Update

let updateTablePosition (floorplanTable: floorplan_table_transform) =
    update {
        table "floorplan_tables"
        set floorplanTable
        where (eq "table_number" floorplanTable.table_number + eq "venue_id" currentVenue)
    } |> db.Update

let createEmptyReservation tableNumber covers =
    let table = getTable tableNumber
    let status = if table.status = "" then "reserved" else table.status

    //let res = newReservation "" 0 covers

    update{
        table "floorplan_tables"
        set {| status = status  |}
        where(eq "table_number" tableNumber)
    } |> db.Update |> ignore



let getChildTables tableNumber =
    let table = getTable tableNumber
    let json = table.merged_children

    json |> Decode.unsafeFromString(Decode.array floorplan_table_decoder)

let matchTable (tableNumberToMatch: int) (floorplanTableToCheck: floorplan_table) =
    tableNumberToMatch = floorplanTableToCheck.table_number

let findChildTable (childTable: int) (parentTable: floorplan_table) =
    let json = parentTable.merged_children
    let childTables = json |> Decode.fromString(Decode.array floorplan_table_decoder)

    let matchedTables =
        match childTables with
            | Ok table -> table |> Array.map(matchTable childTable)
            | Error _ -> [|false|]
        |> removeFalseValues
        |> length

    match matchedTables with
        | 0 -> 0
        | _ -> parentTable.table_number

let tableExists (tableNumber: int) =
    let numberOfResults =
       select{
           table "floorplan_tables"
           where (eq "table_number" tableNumber + eq "venue_id" currentVenue)
       } |> db.Select<floorplan_table> |> length

    match numberOfResults with
       | 0 ->
            let allTables =
                select {
                    table "floorplan_tables"
                }   |> db.Select<floorplan_table>
                    |> Array.map(findChildTable tableNumber)
                    |> Array.filter(fun tableNumber -> tableNumber <> 0)


            match allTables.Length with
                | 0 -> false |> string //Table does not exist
                | _ ->
                    let parentTableData = getTable allTables.[0]
                    let parentRoom = getRoom parentTableData.room_id
                    let parentRoomName = parentRoom.room_name
                    language.getAndReplace "error_table_exists_merged" [parentRoomName; parentTableData.table_number.ToString()]


       | _  ->
                let tableData = getTable tableNumber
                let room = getRoom tableData.room_id
                language.getAndReplace "error_table_exists" [room.room_name]


let addNewTable newTable =
    if tableExists newTable.table_number = "False" then
        insert{
            table "floorplan_tables"
            value newTable
        } |> db.Insert |> ignore
        true
    else false

let deleteTable tableNumber =
    delete {
        table "floorplan_tables"
        where (eq "table_number" tableNumber + eq "venue_id" currentVenue)
    } |> db.Delete |> ignore

let mergeTables parent child = //Merge two tables together
    if parent = child then false else
        let parentTable = getTable parent
        let childTable = getTable child

        let xDiff = (parentTable.pos_x - childTable.pos_x) |> Math.Abs
        let yDiff = (parentTable.pos_y - childTable.pos_y) |> Math.Abs

        let newHeight =
            if xDiff < yDiff then parentTable.height + childTable.height
            else parentTable.height

        let newWidth =
            if xDiff >= yDiff then parentTable.width + childTable.width
            else parentTable.width

        let newPosX =
            if parentTable.pos_x <= childTable.pos_x then parentTable.pos_x + xDiff/2
            else parentTable.pos_x - xDiff/2

        let newPosY =
            if parentTable.pos_y <= childTable.pos_y then parentTable.pos_y + yDiff/2
            else parentTable.pos_y - yDiff/2

        let newChildTable = childTable

        let existingChildrenJson = parentTable.merged_children |> StringTrim
        let existingChildren =
            existingChildrenJson
                |> Decode.fromString(Decode.list floorplan_table_decoder)

        let tableList =
            match existingChildren with
                | Error _ -> [newChildTable]
                | Ok tables -> tables @ [newChildTable]

        let newChildrenJson = tableList |> jsonEncode
        let parentPreviousState = parentTable |> jsonEncode

        update {
            table "floorplan_tables"
            set {|
                   merged_children = newChildrenJson
                   previous_state = parentPreviousState
                   height = newHeight
                   width = newWidth
                   pos_x = newPosX
                   pos_y = newPosY
                   default_covers = parentTable.default_covers + childTable.default_covers
                |}
            where (eq "table_number" parent + eq "venue_id" currentVenue)
        } |> db.Update |> ignore

        deleteTable child

        true


let updateUnmergedTables parentTable childTable =
    update {
       table "floorplan_tables"
       set parentTable
       where(eq "table_number" parentTable.table_number + eq "venue_id" currentVenue)
    } |> db.Update |> ignore

    addNewTable childTable |> ignore
    true

let processUnmerge originalTable unmergedChild =
        let previousState = originalTable.previous_state  |> Decode.Auto.fromString<floorplan_table>

        match previousState with
            | Ok table -> updateUnmergedTables table unmergedChild
            | Error _ -> false

let unmergeTable tableNumber = //Separates a merged table into itself and the last table merged into it.
    let currentTable = getTable tableNumber
    let mergedChildren = currentTable.merged_children |> Decode.fromString(Decode.list floorplan_table_decoder)

    match mergedChildren with
        | Ok listOfChildTables ->
            let unmergedChild = listOfChildTables |> List.last
            processUnmerge currentTable unmergedChild
        | Error _ -> false

let convertRoomListToLinks (room: floorplan_room) =
    let vars = map [
        "roomId", room.room_id |> string
        "roomName", room.room_name
    ]

    Theme.loadTemplateWithVars "roomButton" vars

let newReservation name time covers =
    let reservation = {
        reservation_id = 0
        reservation_name = name
        reservation_time = time
        reservation_covers = covers
        reservation_table_id = 0
        reservation_created_at = CurrentTime()
    }

    insert {
        table "reservations"
        value reservation
    } |> db.Insert

let unReserveTable tableNumber =
    let table = getTable tableNumber
    DeleteReservation table.table_id
    if table.status = "reserved" then
        update {
            table "floorplan_tables"
            set {| status = "" ; reservation_id = 0 |}
        } |> db.Update |> ignore

