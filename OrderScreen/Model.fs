module DredgePos.OrderScreen.Model

open DredgeFramework
open DredgePos
open DredgePos.Types
open FSharp.Collections
open Thoth.Json.Net
open Theme


let getAllPageGrids () = Entity.GetAllInVenue<order_screen_page_group>
                         |> Array.filter(fun pageGroup -> pageGroup.grid_id <> 0)
                         |> Array.map(fun pageGroup -> (Entity.GetById<grid> pageGroup.grid_id), pageGroup)

let getImageButtonData (button: button) =
    let itemCode =
        if button.primary_action = "item" then button.primary_action_value
        else button.secondary_action_value

    let item = Entity.GetAllByColumn<item> "item_code" itemCode
                |> first

    let extraData =
        map [
            "data-item", jsonEncode item
        ] |> htmlAttributes

    {|
        extra_data = extraData
        text = item.name
    |}

let getGridButtonData (button: button) =
    let gridId =
        if button.primary_action = "grid" then button.primary_action_value
        else button.secondary_action_value
        |> int

    let grid = Entity.GetById<grid> gridId
    {|
        extra_data = map ["data-grid", jsonEncode gridId] |> htmlAttributes
        text = grid.name
    |}

let getActionData (button: button) (action: string) =
    let actionValue =
        if action = "primary" then button.primary_action
        else button.secondary_action

    match actionValue with
        | "item" -> getImageButtonData button
        | "grid" -> getGridButtonData button
        | "spacer" ->  {|extra_data=""; text=""|}
        | _ -> {|extra_data=""; text=""|}

let renderButton (buttonId: int) =
    let button = Entity.GetById<button> buttonId

    let extra_styles =
        match button.extra_styles.Length with
            | 0 -> ""
            | _ -> $""" style="{button.extra_styles}" """

    let imageClass = if button.image.Length > 0 then "hasImage" else ""
    let spacerClass = if button.primary_action = "spacer" || button.secondary_action = "spacer"
                        then "invisible"
                        else ""

    let image = if button.image.Length > 0 then loadTemplateWithVars "orderScreen/button_image" (map ["image", button.image]) else ""

    let extraClasses = [|imageClass; spacerClass|] |> String.concat " "

    let primary_action_data = getActionData button "primary"
    let secondary_action_data = getActionData button "secondary"

    let action_extra_data = primary_action_data.extra_data + " " + secondary_action_data.extra_data
    let button_text =
        if button.text.Length > 0 then button.text
        else
            if primary_action_data.text.Length > 0 then primary_action_data.text
            else secondary_action_data.text

    let vars = map [
        "extra_classes",  button.extra_classes + " " + extraClasses
        "extra_styles", extra_styles
        "primary_action", button.primary_action
        "secondary_action", button.secondary_action
        "text", button_text
        "image", image
        "extra_data", action_extra_data
    ]

    loadTemplateWithVars "orderScreen/grid_button" vars

let renderPage (grid: grid)  (buttonHTML: string) =
    let vars = map ["pageButtons", buttonHTML; "rows", string grid.rows; "cols", string grid.cols]
    loadTemplateWithVars "orderScreen/page" vars

let renderPageGroup (pageGroup: order_screen_page_group) (pageHTML: string) =
    let vars = map [
        "pages", pageHTML
        "page_group_id", string pageGroup.id
    ]
    loadTemplateWithVars "orderScreen/page_group" vars

let printGroupPosButton (printGroup: print_group) =
    PosButton (language.getAndReplace "print_with" [printGroup.name]) "printGroupOverrideButton toggle" $"""data-value="{printGroup.id}" """

let generateSalesCategoryOverrideButtons () =
    Entity.GetAllInVenue<print_group>
        |> Array.map printGroupPosButton
        |> Array.append [|PosButton (language.getAndReplace "print_with" ["default"]) "printGroupOverrideButton toggle default active" """data-value="0" """|]
        |> joinWithNewLine


let renderGrid (grid: grid) =
    let gridData = grid.data |> Decode.Auto.fromString<Map<string, int[]>>

    match gridData with
        | Error _ -> "Error"
        | Ok pages ->
            pages
                |> Map.toArray
                |> Array.map snd
                |> Array.map(fun row -> row |> Array.map renderButton |> String.concat "\n")
                |> Array.map (renderPage grid)
                |> joinWithNewLine

let loadGrid gridId = renderGrid (Entity.GetById<grid> gridId)


let getPagesHTML (gridInfo: grid * order_screen_page_group) =
    let grid, pageGroup = gridInfo

    renderGrid grid
    |> renderPageGroup pageGroup
