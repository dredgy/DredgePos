module DredgePos.OrderScreen.Controller

open DredgePos
open DredgeFramework
open DredgePos.Types
open DredgePos.Global.Controller
open Giraffe
open Microsoft.AspNetCore.Http

let getOrderScreenData (tableNumber: int) =
    {|
        order_screen_pages = Entity.GetAllInVenue<order_screen_page_group>
        sales_categories = Entity.GetAllInVenue<sales_category>
        print_groups = Entity.GetAllInVenue<print_group>
        custom_item = Entity.GetAllByColumn<item> "code" "OPEN000" |> first
        table = Floorplan.Model.getTable tableNumber
    |}
    |> ajaxSuccess
    |> json

let loadGrid (gridId: int) =
    let grid = Entity.GetById<grid> gridId
    let gridHtml = Model.loadGrid gridId
    if gridHtml = "Error" then ajaxFail gridHtml
    else ajaxSuccess {|grid=grid;gridHtml=gridHtml|}
    |> json

let loadOrderScreenView (ctx: HttpContext)  (tableNumber: int) =
    Authenticate.Model.RequireClerkAuthentication ctx
    let currentClerk = Authenticate.Model.getCurrentClerk ctx
    let styles = [|"dredgepos.orderScreen.css"|] |> addDefaultStyles
    let scripts = [|"dredgepos.tables.js";"./external/currency.min.js";"dredgepos.orderScreen.js"; |] |> addDefaultScripts
    let metaTags = [|"viewport", "user-scalable = no, initial-scale=0.8,maximum-scale=0.8 ,shrink-to-fit=yes"|] |> addDefaultMetaTags

    let orderScreenPageGroups =
       Entity.GetAllInVenue<order_screen_page_group>
        |> Array.filter (fun page_group -> page_group.id <> 0)
        |> Array.sortBy (fun {order=order} -> order)

    View.index tableNumber styles scripts metaTags currentClerk orderScreenPageGroups

let loadOrderScreen (ctx: HttpContext)  (tableNumber: int) : HttpHandler =
   Authenticate.Model.RequireClerkAuthentication ctx

   let table = Floorplan.Model.getTable tableNumber

   let covers = if tableNumber > 0 then table.default_covers else 0
   let coverString = language.getAndReplace "covers" [covers]

   let changeCoverNumberButton = if tableNumber > 0 then Theme.loadTemplateWithVars "orderScreen/change_cover_number_button" (map ["covers", coverString]) else ""

   let orderNumber =
       if tableNumber > 0 then language.getAndReplace "active_table" [tableNumber]
       else language.get "new_order"

   let containerAttributes =
        if tableNumber > 0 then
            map ["data-table", jsonEncode table]
                |> Theme.htmlAttributes
        else ""

   let categoryList =
       Entity.GetAllInVenue<order_screen_page_group>
        |> Array.filter (fun page_group -> page_group.id <> 0)
        |> Array.sortBy (fun {order=order} -> order)
        |> Array.map (fun category ->
            let categoryMap = recordToMap category
            let categoryArray = map ["page", categoryMap]
            Theme.loadTemplateWithArrays "orderScreen/page_group_button" categoryArray
            )
        |> joinWithNewLine

   let grids =
       Model.getAllPageGrids ()
       |> Array.map Model.getPagesHTML
       |> joinWithNewLine

   let coverSelectorButtons =
        Array.init (covers+1) id
            |> Array.map(fun coverNumber ->
                let text = if coverNumber > 0 then language.getAndReplace "selected_cover" [coverNumber]
                           else language.get "cover_zero"
                Theme.PosButton text "coverSelectorButton" $"""data-cover="{coverNumber}" """)
            |> String.concat "\n"

   let variables = map [
       "title", "Order"
       "containerAttributes", containerAttributes
       "categoryList", categoryList
       "pageGroups", grids
       "orderNumber", orderNumber
       "changeCoverNumberButton", changeCoverNumberButton
       "covers", coverString
       "salesCategoryOverrideButtons", Model.generateSalesCategoryOverrideButtons ()
       "coverSelectorButtons", coverSelectorButtons
   ]

   let styles = ["dredgepos.orderScreen.css"]
   let scripts = ["dredgepos.tables.js";"./external/currency.min.js";"dredgepos.orderScreen.js"; ]
   let currentClerk = recordToMap <| Authenticate.Model.getCurrentClerk ctx
   let arrays = map ["clerk", currentClerk]

   Theme.loadTemplateWithVarsArraysScriptsAndStyles "orderScreen" variables arrays scripts styles
    |> htmlString
