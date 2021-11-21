module Floorplan

open DredgePos
open Reservations

open System
open System.IO
open System.Xml.Linq
open DredgeFramework
open Dapper
open Dapper.FSharp
open Thoth.Json.Net
open Types

let activeTablePath = "tables/active/"

let getTableFile (tableNumber: int) =
    let tableNumberString = tableNumber |> string
    activeTablePath + "table" + tableNumberString + ".table"

let tableNumberIsOpen (tableNumber: int) =
    let tableFile = getTableFile tableNumber
    File.Exists tableFile

let tableIsOpen (table: floorplan_table) = tableNumberIsOpen table.table_number

let fileNameToTableNumber (fileName: string) = //Takes a file name for a floorplan table and returns the table number
    if fileName.Contains ".table" then
        let fileName = (fileName.Split ".").[0]
        (fileName.Split "/table").[1] |> int
    else 0

let openTables () = //Get a list of all open tables.
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
    |> Array.filter tableIsOpen
    |> Array.map (fun table -> table.table_number)

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

    if tableNumberIsOpen origin then
        (* If the destination is not an already open table,
        then we simply have to rename the origin to destination *)
        if not <| tableNumberIsOpen destination then
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
    let query = select {
        table "floorplan_tables"
        where (eq "table_number" tableNumber + eq "venue_id" (getCurrentVenue()))
    }

    let result = query |> db.Select<floorplan_table>
    result |> first

let getTableById (id : int) =
    select {
        table "floorplan_tables"
        where (eq "id" id)
    }
      |> db.Select<floorplan_table>
      |> first

let getRoom (roomId: int) =
    select {
        table "floorplan_rooms"
        where (eq "id" roomId)
    } |> db.Select<floorplan_room> |> first

let updateFloorplanTable (tableNumber:int) (column: string) value =
    //TODO: Make update query venue specific
    let sql = "Update floorplan_tables Set @column = @value Where table_number = @tableNumber"
    let parameters = [("column", box column); ("value", box value); ("tableNumber", box tableNumber)]
    db.connection.Execute(sql, parameters) |> ignore

    getTable tableNumber

let updateTableShape (floorplanTable: floorplan_table)  =
    update {
        table "floorplan_tables"
        set floorplanTable
        where (eq "table_number" floorplanTable.table_number + eq "venue_id" (getCurrentVenue()))
    } |> db.Update


let updateTablePosition (floorplanTable: floorplan_table) = Entity.updateInDatabase floorplanTable

let createEmptyReservation (reservation: reservation) =
    update {
        table "floorplan_tables"
        set {| status = 2  |}
        where(eq "id" reservation.reservation_table_id)
    } |> db.Update |> ignore

    Entity.addToDatabase reservation

let getChildTables tableNumber =
    let table = getTable tableNumber
    let json = table.merged_children

    let result = json |> Decode.Auto.fromString<floorplan_table[]>
    match result with
        | Ok tables -> tables
        | Error _ -> [||]


let matchTable (tableNumberToMatch: int) (floorplanTableToCheck: floorplan_table) =
    tableNumberToMatch = floorplanTableToCheck.table_number

let findChildTable (childTable: int) (parentTable: floorplan_table) =
    let json = parentTable.merged_children
    let childTables = json |> Decode.Auto.fromString<floorplan_table[]>

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
           where (eq "table_number" tableNumber + eq "venue_id" (getCurrentVenue()))
       } |> db.Select<floorplan_table> |> length

    match numberOfResults with
       | 0 ->
            let allTables =
                Entity.getAllInVenue<floorplan_table>
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


let addNewTableWithoutOutput (newTable: floorplan_table) =
    insert{
        table "floorplan_tables"
        value newTable
    }
    |> db.Insert

let addNewTable (newTable: floorplan_table) = Entity.addToDatabase newTable

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
            existingChildrenJson |> Decode.Auto.fromString<floorplan_table[]>

        let tableList =
            match existingChildren with
                | Error _ -> [|newChildTable|]
                | Ok tables -> [tables ; [|newChildTable|]] |> Array.concat

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
            where (eq "table_number" parent + eq "venue_id" (getCurrentVenue()))
        } |> db.Update |> ignore

        Entity.deleteById<floorplan_table> newChildTable.id
            |> ignore

        true


let updateUnmergedTables parentTable childTable =
    update {
       table "floorplan_tables"
       set parentTable
       where(eq "table_number" parentTable.table_number + eq "venue_id" (getCurrentVenue()))
    } |> db.Update |> ignore

    addNewTableWithoutOutput childTable |> ignore
    true

let processUnmerge originalTable unmergedChild =
        let previousState = originalTable.previous_state  |> Decode.Auto.fromString<floorplan_table>

        match previousState with
            | Ok table -> updateUnmergedTables table unmergedChild
            | Error _ -> false

let unmergeTable tableNumber = //Separates a merged table into itself and the last table merged into it.
    let currentTable = getTable tableNumber
    let mergedChildren = currentTable.merged_children |> Decode.Auto.fromString<floorplan_table[]>

    match mergedChildren with
        | Ok listOfChildTables ->
            let unmergedChild = listOfChildTables |> last
            processUnmerge currentTable unmergedChild |> ignore
            Some (getTable currentTable.table_number, unmergedChild)
        | Error _ -> None

let makeRoomButton (room: floorplan_room) =
    let vars = map [
        "roomId", room.id |> string
        "roomName", room.room_name
    ]

    Theme.loadTemplateWithVars "roomButton" vars

let getReservationList (tableList: floorplan_table[]) =
    let tableIds =
        tableList
            |> Array.map(fun table -> table.id)
            |> JoinArray ","

    db.connection.Query<reservation>($"""Select * From reservations Where reservation_table_id In ({tableIds})""")
        |> EnumerableToArray

let newReservation name time covers =
    let reservation = {
        id = 0
        reservation_name = name
        reservation_time = time
        reservation_covers = covers
        reservation_table_id = 0
        reservation_created_at = CurrentTime()
    }

    Entity.addToDatabase reservation


let tableList () = Entity.getAllInVenue<floorplan_table>
