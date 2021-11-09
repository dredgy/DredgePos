module AjaxController

open DredgeFramework
open DredgePos
open Floorplan
open Microsoft.AspNetCore.Http
open Reservations
open language
open Giraffe
open Types

let loginWithLoginCode (context: HttpContext) (login_code: int) =
     if Session.clerkLogin login_code context then ajaxSuccess "success"
     else ajaxFail "fail"

let getLanguageVars = ajaxSuccess languageVars

let getActiveTables venue = Floorplan.getActiveTables venue |> ajaxSuccess |> json

let getRoomData roomId = Floorplan.getRoom roomId |> ajaxSuccess |> json

let mergeTables (tables: floorplan_table[]) =
    let status =
        if mergeTables tables.[0].table_number tables.[1].table_number then
            let outputTables = map [
                "parent", tables.[0];
                "child", tables.[1];
                "merged", getTable tables.[0].table_number;
            ]
            ajaxSuccess outputTables
        else ajaxFail "Could Not Merge Tables"
    status |> json

let unmergeTable tableNumber =
    let unmerged = Floorplan.unmergeTable tableNumber
    let unmergedTables =
        match unmerged  with
            | Some (parent, child) ->
                map["parent", parent; "child", child] |> ajaxSuccess
            | None -> ajaxFail "Could not Unmerge Table"

    unmergedTables |> json


let getFloorplanData venue =
    let tableList = Floorplan.tableList venue
    let reservationList = getReservationList tableList
    {|
        tables = tableList
        decorations = Decorations.decorationList venue
        activeTableNumbers = Floorplan.getActiveTables venue
        rooms = Floorplan.getRoomList venue
        reservations = reservationList
    |}
    |> ajaxSuccess
    |> json

let getKeyboardLayout (language: string) =
    let layout = $"""wwwroot/languages/{language}/keyboardLayout.json""" |> GetFileContents
    map [
            "status", "success"
            "data", layout
        ] |> json


let getRoomTablesAndDecorations roomId =
    let tables = Floorplan.tablesInRoom roomId
    let decorations = Decorations.decorationsInRoom roomId
    let data = {|
        tables = tables
        decorations = decorations
    |}

    data |> ajaxSuccess |> json

let getTableData tableNumber = json <| Floorplan.getTable tableNumber

let updateTableShape (table: floorplan_table) =
    Floorplan.updateTableShape table |> ignore
    getTableData table.table_number

let transformTable (table: floorplan_table) =
    Floorplan.updateTablePosition table |> ignore
    getTableData table.table_number

let createTable (tableData: floorplan_table) =
    let result =
        if tableExists tableData.table_number = "False" then
            ajaxSuccess (addNewTable tableData)
        else ajaxFail (tableExists tableData.table_number)

    result |> json

let deleteTable (table: floorplan_table) =
    Floorplan.deleteTable table.table_number
    table |> ajaxSuccess |> json

let transferTable (origin, destination) =
    Floorplan.transferTable origin destination
    let data = map ["origin", getTable origin ; "destination", getTable destination]
    ajaxSuccess data |> json

let AddDecoration (data: floorplan_decoration) =
    let image = "wwwroot/images/decorations/" + data.decoration_image
    let width, height = image |> GetImageSize
    let aspectRatio = decimal width /  decimal height

    let decoration : floorplan_decoration = {
        id = 0
        decoration_height = (200m / aspectRatio) |> int
        decoration_width = 200
        decoration_rotation = 0
        decoration_image = data.decoration_image
        decoration_pos_x = data.decoration_pos_x
        decoration_pos_y = data.decoration_pos_y
        decoration_room = data.decoration_room
    }

    Decorations.CreateDecoration decoration |> ajaxSuccess |> json

let UpdateDecoration data =
    Decorations.UpdateDecoration data |> ignore
    ajaxSuccess "true" |> json

let DeleteDecoration id = ajaxSuccess (Decorations.DeleteDecoration id) |> json

let newEmptyReservation (reservation: reservation) =
    let newReservation = {reservation with
                            reservation_created_at = CurrentTime()
                            reservation_time = CurrentTime()
                          }

    if reservation.reservation_table_id > 0 then
        let table = {(getTableById reservation.reservation_table_id) with
                        status = 2
                        default_covers = reservation.reservation_covers}
        updateTablePosition table |> ignore

    let createdReservation = Floorplan.createEmptyReservation newReservation
    ajaxSuccess createdReservation |> json

let updateReservation (reservation: reservation) = updateReservation reservation |> ajaxSuccess |> json

let unreserveTable (table: floorplan_table) =
    let newTable = {table with status = 0}
    updateTablePosition newTable |> ignore
    DeleteReservation newTable.id
    newTable |> ajaxSuccess |> json
