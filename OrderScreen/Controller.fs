﻿module DredgePos.OrderScreen.Controller

open DredgePos
open DredgeFramework
open DredgePos.Types
open DredgePos.Global.Controller
open Saturn.CSRF
open Thoth.Json.Net
open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Collections

let getOrderScreenData (tableNumber: int) =
    {|
        order_screen_pages = Entity.GetAllInVenue<order_screen_page_group>
        sales_categories = Entity.GetAllInVenue<sales_category>
        print_groups = Entity.GetAllInVenue<print_group>
        custom_item = Entity.GetFirstByColumn<item> "code" "OPEN000"
        table = Floorplan.Model.getTable tableNumber
    |}
    |> ajaxSuccess
    |> json

let renderGrid (grid: grid) =
    let gridData = grid.data |> Decode.Auto.fromString<Map<string, int[]>>
    match gridData with
    | Error message -> failwith message
    | Ok data ->
        data
        |> Map.toArray
        |> Array.map snd
        |> Array.map(
            fun buttonIds ->
                buttonIds
                    |> Array.map Entity.GetById<button>
                    |> Array.map View.itemButton
                    |> View.gridPage grid
        )

let loadGrid (gridId: int) =
    let grid = Entity.GetById<grid> gridId
    let gridNodes = (renderGrid grid) |> List.ofArray
    let gridHtml = Giraffe.ViewEngine.RenderView.AsString.htmlNodes gridNodes

    if gridHtml = "Error" then ajaxFail gridHtml
    else ajaxSuccess {|grid=grid;gridHtml=gridHtml|}
    |> json

let loadOrderScreenView (ctx: HttpContext)  (tableNumber: int) =
    Authenticate.Model.RequireClerkAuthentication ctx
    let tableOption = DredgePos.Floorplan.Model.getTableSafely tableNumber
    let attr = Giraffe.ViewEngine.HtmlElements.attr

    match tableOption with
        | None ->
            Browser.redirect "/" ctx
            View.posButtonTemplate
        | Some table ->
            let currentClerk = Authenticate.Model.getCurrentClerk ctx
            let styles = [|"dredgepos.orderScreen.css"|] |> addDefaultStyles
            let scripts = [|"dredgepos.tables.js";"./external/currency.min.js";"dredgepos.orderScreen.js"; |] |> addDefaultScripts
            let metaTags = [|"viewport", "user-scalable = no, initial-scale=0.8,maximum-scale=0.8 ,shrink-to-fit=yes"|] |> addDefaultMetaTags

            let printGroupButtons =
                Entity.GetAllInVenue<print_group>
                    |> Array.map View.printGroupButton

            let orderScreenPageGroupButtons =
               Entity.GetAllInVenue<order_screen_page_group>
                |> Array.filter (fun page_group -> page_group.id <> 0)
                |> Array.sortBy (fun {order=order} -> order)
                |> Array.map View.pageGroupButton

            let grids = Model.getAllPageGridsInVenue ()
            let pageGroupNodes =
                grids
                |> Array.map(fun (grid, page_group) ->
                    renderGrid grid
                    |> View.pageGroup page_group
                )

            let coverSelectorButtons =
                Array.init (table.default_covers + 1) id
                    |> Array.map(fun coverNumber ->
                        let text = if coverNumber > 0 then language.getAndReplace "selected_cover" [coverNumber]
                                   else language.get "cover_zero"
                        Global.View.PosButton "coverSelectorButton" (map ["data-cover", coverNumber]) text
                    )

            View.index tableNumber styles scripts metaTags currentClerk printGroupButtons orderScreenPageGroupButtons pageGroupNodes coverSelectorButtons