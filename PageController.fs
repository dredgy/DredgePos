module PageController

open System
open DredgePos.Types
open Microsoft.AspNetCore.Http
open Floorplan
open Giraffe
open DredgeFramework

let loadHomePage(): HttpHandler =
     let variables = map["title", "Log In"]
     let scripts = ["dredgepos.authenticate.js"]
     let styles = ["dredgepos.authenticate.css"]

     htmlString <| Theme.loadTemplateWithVarsScriptsAndStyles "authenticate" variables scripts styles

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

   let arrays = map["clerk", currentClerk]

   htmlString <| Theme.loadTemplateWithVarsArraysScriptsAndStyles "floorplan" variables arrays scripts styles

let loadOrderScreen (ctx: HttpContext) : HttpHandler =
   Session.RequireClerkAuthentication ctx

   let categoryList =
       Entity.getAll<order_screen_page_group>
        |> Array.filter (fun category -> category.id <> 0)
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
   ]

   let styles = ["dredgepos.orderScreen.css"]
   let scripts = ["dredgepos.orderScreen.js"]
   let currentClerk = recordToMap <| Session.getCurrentClerk ctx
   let arrays = map["clerk", currentClerk]

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