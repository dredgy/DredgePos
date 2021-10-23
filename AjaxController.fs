module AjaxController

open DredgeFramework
open Microsoft.AspNetCore.Http
open language
open Giraffe

let loginWithLoginCode (context: HttpContext) (login_code: int) =
     if Session.clerkLogin login_code context then ajaxSuccess "success"
     else ajaxFail "fail"

let getLanguageVars = ajaxSuccess languageVars

let getActiveTables venue = Floorplan.getActiveTables venue |> ajaxSuccess |> json

let getRoomData roomId = Floorplan.getRoom roomId |> ajaxSuccess |> json

let mergeTables (parent, child) =
    let status =
        if Floorplan.mergeTables parent child then "success"
        else "fail"

    map [
        "status", status
        "data" , parent |> Floorplan.getTable |> jsonEncode
    ]
    |> json

let unmergeTable tableNumber =
    let status =
        if Floorplan.unmergeTable tableNumber then "success"
        else "fail"

    map [
        "status", status
        "data" , "[true]"
    ]
    |> json

let getRoomTablesAndDecorations roomId =
    let tables = Floorplan.tablesInRoom roomId
    let decorations = Decorations.decorationsInRoom roomId
    let data = {|
        tables = tables
        decorations = decorations
    |}

    data |> ajaxSuccess |> json

let getTableData tableNumber = json <| Floorplan.getTable tableNumber

let updateTableShape (table: Floorplan.floorplan_table_shape) =
    Floorplan.updateTableShape table |> ignore
    getTableData table.table_number

let transformTable (table: Floorplan.floorplan_table_transform) =
    Floorplan.updateTablePosition table |> ignore
    getTableData table.table_number

let createTable (tableData) =
    let newTableCreated = Floorplan.addNewTable tableData
    let result =
        if newTableCreated then Floorplan.getTable tableData.table_number |> jsonEncode |> ajaxSuccess
        else Floorplan.tableExists tableData.table_number |> jsonEncode |> ajaxFail

    json result

let transferTable (origin, destination) =
    Floorplan.transferTable origin destination
    ajaxSuccess "true" |> json

let AddDecoration (data: Decorations.decoration_creator) =
    let image = "wwwroot/images/decorations/" + data.decoration_image
    let width, height = image |> GetImageSize
    let aspectRatio = decimal width /  decimal height

    let decoration : Decorations.floorplan_decoration = {
        decoration_id = 0
        decoration_height = (200m / aspectRatio) |> int
        decoration_width = 200
        decoration_rotation = 0
        decoration_image = data.decoration_image
        decoration_pos_x = data.basis/2
        decoration_pos_y = data.basis/2
        decoration_room = data.decoration_room
    }

    Decorations.CreateDecoration decoration |> ignore
    ajaxSuccess "true" |> json

let UpdateDecoration data =
    Decorations.UpdateDecoration data |> ignore
    ajaxSuccess "true" |> json

let DeleteDecoration id =
    Decorations.DeleteDecorationById id |> ignore
    ajaxSuccess "true" |> json

let newEmptyReservation tableNumber =
    Floorplan.createEmptyReservation tableNumber 2

    json <| ajaxSuccess "true"
