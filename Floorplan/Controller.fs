module DredgePos.Floorplan.Controller

open DredgeFramework
open DredgePos
open DredgePos.Global.Controller
open DredgePos.Entities
open DredgePos.Types
open Giraffe
open Microsoft.AspNetCore.Http
open Model
open System.IO

let makeRoomButton (room: room) =
    let vars = map [
        "roomId", room.id |> string
        "roomName", room.name
    ]

    Theme.loadTemplateWithVars "roomButton" vars

let getActiveTables venue = Model.getActiveTables venue |> ajaxSuccess |> json

let mergeTables (tables: floorplan_table[]) =
    let status =
        if mergeTables tables[0].table_number tables[1].table_number then
            let outputTables = map [
                "parent", tables[0];
                "child", tables[1];
                "merged", Model.getTable tables[0].table_number;
            ]
            ajaxSuccess outputTables
        else ajaxFail "Could Not Merge Tables"
    status |> json

let unmergeTable tableNumber =
    let unmerged = unmergeTable tableNumber
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
        activeTableNumbers = Model.getActiveTables (getCurrentVenue())
        rooms = Entity.GetAllInVenue<room>
        reservations = reservationList
    |}
    |> ajaxSuccess
    |> json

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
    transferTable origin destination
    let data = map ["origin", getTable origin ; "destination", getTable destination]
    ajaxSuccess data |> json

let AddDecoration (data: floorplan_decoration) =
    let image = "wwwroot/images/decorations/" + data.image
    let width, height = image |> GetImageSize
    let aspectRatio = decimal width /  decimal height

    let decoration : floorplan_decoration = {
        id = 0
        height = (200m / aspectRatio) |> int
        width = 200
        rotation = 0
        image = data.image
        pos_x = data.pos_x
        pos_y = data.pos_y
        room_id = data.room_id
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

let loadFloorplanView (ctx: HttpContext) =
   Authenticate.Model.RequireClerkAuthentication ctx
   let roomMenu = Entity.GetAllInVenue<room> |> Array.map View.roomButton
   let currentClerk = Authenticate.Model.getCurrentClerk ctx
   let styles = [|"dredgepos.floorplan.css"|] |> addDefaultStyles
   let scripts = [|"./external/konva.min.js" ; "dredgepos.floorplan.js"|] |> addDefaultScripts
   let metaTags = [|"viewport", "user-scalable = no, initial-scale=0.8,maximum-scale=0.8 ,shrink-to-fit=yes"|] |> addDefaultMetaTags

   View.index styles scripts metaTags currentClerk (Floorplan_Decorations.Controller.generateDecorator ()) roomMenu