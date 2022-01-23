﻿module AjaxController

open DredgeFramework
open DredgePos
open DredgePos.Types
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

let mergeTables (tables: floorplan_table[]) =
    let status =
        if mergeTables tables[0].table_number tables[1].table_number then
            let outputTables = map [
                "parent", tables[0];
                "child", tables[1];
                "merged", getTable tables[0].table_number;
            ]
            ajaxSuccess outputTables
        else ajaxFail "Could Not Merge Tables"
    status |> json

let unmergeTable tableNumber =
    let unmerged = Floorplan.unmergeTable tableNumber
    let unmergedTables =
        match unmerged  with
            | Some (parent, child) ->
                map ["parent", parent; "child", child] |> ajaxSuccess
            | None -> ajaxFail "Could not Unmerge Table"

    unmergedTables |> json


let getFloorplanData (id: int) =
    let tableList = Entity.GetAllInVenue<floorplan_table>
    let reservationList = getReservationList tableList
    {|
        tables = tableList
        decorations = Entity.GetAllInVenue<floorplan_decoration>
        activeTableNumbers = Floorplan.getActiveTables (getCurrentVenue())
        rooms = Entity.GetAllInVenue<floorplan_room>
        reservations = reservationList
    |}
    |> ajaxSuccess
    |> json

let getOrderScreenData (tableNumber: int) =
    {|
        order_screen_pages = Entity.GetAllInVenue<order_screen_page_group>
        sales_categories = Entity.GetAllInVenue<sales_category>
        print_groups = Entity.GetAllInVenue<print_group>
        custom_item = Entity.GetAllByColumn<item> "item_code" "OPEN000" |> first
        table = getTable tableNumber
    |}
    |> ajaxSuccess
    |> json

let getKeyboardLayout (language: string) =
    let layout = $"""wwwroot/languages/{language}/keyboardLayout.json"""
                 |> GetFileContents
    map [
            "status", "success"
            "data", layout
        ] |> json

let transformTable (table: floorplan_table) =
        Entity.Update table
        |> ajaxSuccess
        |> json

let createTable (tableData: floorplan_table) =
    if tableExists tableData.table_number = "False" then
        ajaxSuccess (addNewTable tableData)
    else ajaxFail (tableExists tableData.table_number)
    |> json

let deleteTable (table: floorplan_table) =
    Entity.DeleteById<floorplan_table> table.id
        |> ignore
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
        venue_id = data.venue_id
    }

    Entity.Create decoration
        |> ajaxSuccess
        |> json

let UpdateDecoration (data: floorplan_decoration) =
    Entity.Update data
        |> ignore
    ajaxSuccess "true" |> json

let DeleteDecoration (decorationToDelete: floorplan_decoration) =
    Entity.DeleteById<floorplan_decoration> decorationToDelete.id
    |> ajaxSuccess
    |> json

let newEmptyReservation (reservation: reservation) =
    let newReservation = {reservation with
                            created_at = CurrentTime()
                            time = CurrentTime()
                          }

    if reservation.floorplan_table_id > 0 then
        let table = {(getTableById reservation.floorplan_table_id) with
                        status = 2
                        default_covers = reservation.covers}
        updateTablePosition table |> ignore

    let createdReservation = Floorplan.createEmptyReservation newReservation
    ajaxSuccess createdReservation |> json

let updateReservation (reservation: reservation) = updateReservation reservation |> ajaxSuccess |> json

let unreserveTable (table: floorplan_table) =
    let newTable = {table with status = 0}
    updateTablePosition newTable |> ignore
    DeleteReservation newTable.id
    newTable |> ajaxSuccess |> json

let loadGrid (gridId: int) =
    let grid = Entity.GetById<grid> gridId
    let gridHtml = OrderScreen.loadGrid gridId
    if gridHtml = "Error" then ajaxFail gridHtml
    else ajaxSuccess {|grid=grid;gridHtml=gridHtml|}
    |> json