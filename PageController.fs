module PageController

open System
open DredgePos.Types
open Microsoft.AspNetCore.Http
open Floorplan
open Giraffe
open DredgeFramework

let loadHomePage(): HttpHandler =
     let variables = map ["title", "Log In"]
     let scripts = ["dredgepos.authenticate.js"]
     let styles = ["dredgepos.authenticate.css"]

     Theme.loadTemplateWithVarsScriptsAndStyles "authenticate" variables scripts styles
        |> htmlString

let loadFloorplan (ctx: HttpContext) : HttpHandler =
   Session.RequireClerkAuthentication ctx

   let roomMenu =
       Entity.getAllInVenue<floorplan_room>
         |> Array.map makeRoomButton
         |> String.concat "\n"

   let variables = map [
       "title", "Floorplan"
       "roomMenu", roomMenu
       "decorator", Decorations.generateDecorator()
   ]
   let styles = ["dredgepos.floorplan.css"]
   let scripts = ["../external/konva.min.js" ; "dredgepos.floorplan.js"]
   let currentClerk = recordToMap <| Session.getCurrentClerk ctx

   let arrays = map ["clerk", currentClerk]

   htmlString <| Theme.loadTemplateWithVarsArraysScriptsAndStyles "floorplan" variables arrays scripts styles

let loadOrderScreen (ctx: HttpContext)  (tableNumber: int) : HttpHandler =
   Session.RequireClerkAuthentication ctx

   let covers = if tableNumber > 0 then (getTable tableNumber).default_covers else 0
   let coverString = language.getAndReplace "covers" [covers]

   let coverSelectorButton = if tableNumber > 0 then Theme.loadTemplateWithVars "orderScreen/cover_selector_button" (map ["covers", coverString]) else ""

   let orderNumber =
       if tableNumber > 0 then language.getAndReplace "active_table" [tableNumber]
       else language.get "new_order"

   let categoryList =
       Entity.getAllInVenue<order_screen_page_group>
        |> Array.filter (fun page_group -> page_group.id <> 0)
        |> Array.sortBy (fun {order=order} -> order)
        |> Array.map (fun category ->
            let categoryMap = recordToMap category
            let categoryArray = map ["page", categoryMap]
            Theme.loadTemplateWithArrays "orderScreen/page_group_button" categoryArray
            )
        |> String.concat "\n"

   let grids =
       OrderScreen.getAllPageGrids ()
       |> Array.map OrderScreen.getPagesHTML
       |> String.concat "\n"

   let variables = map [
       "title", "Order"
       "categoryList", categoryList
       "pageGroups", grids
       "orderNumber", orderNumber
       "coverSelectorButton", coverSelectorButton
       "covers", coverString
   ]

   let styles = ["dredgepos.orderScreen.css"]
   let scripts = ["dredgepos.tables.js";"../external/currency.min.js";"dredgepos.orderScreen.js"; ]
   let currentClerk = recordToMap <| Session.getCurrentClerk ctx
   let arrays = map ["clerk", currentClerk]

   htmlString <| Theme.loadTemplateWithVarsArraysScriptsAndStyles "orderScreen" variables arrays scripts styles

let getOpenTables() =
    let rows = openTables()
    rows |> jsonEncode

let mergeTables parent child =
    mergeTables parent child |> ignore
    "done"

let unmergeTables table =
    unmergeTable table |> ignore
    "done"