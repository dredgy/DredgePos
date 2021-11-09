module PageController

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
       getRoomList currentVenue
         |> Array.map convertRoomListToLinks
         |> String.concat "\n"

   let variables = map [
       "title", "Floorplan"
       "roomMenu", roomMenu
       "decorator", Decorations.generateDecorator()
   ]
   let styles = ["tableMap.css"]
   let scripts = ["external/konva.min.js" ; "dredgepos.floorplan.js"]
   let currentClerk = recordToMap <| Session.getCurrentClerk ctx

   let arrays = map["clerk", currentClerk]

   htmlString <| Theme.loadTemplateWithVarsArraysScriptsAndStyles "tableMap" variables arrays scripts styles

let loadContactPage id =
    Session.clerkLogin 1408 |> ignore
    Theme.loadTemplate "index"

let getOpenTables() =
    let rows = Floorplan.openTables
    rows |> jsonEncode

let transferTables() =

    Theme.loadTemplate "index"

let mergeTables parent child =
    Floorplan.mergeTables parent child |> ignore
    "done"

let unmergeTables table =
    Floorplan.unmergeTable table |> ignore
    "done"