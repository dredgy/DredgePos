module OrderScreen
open System.Security.Cryptography.Xml
open DredgeFramework
open DredgePos
open FSharp.Collections
open Thoth.Json.Net
open Types
open Theme

let htmlAttributes (attributes: Map<string, string>) =
    " " + (attributes
        |> Map.toArray
        |> Array.map (fun (attribute, value) -> attribute+"="+value)
        |> String.concat " ")


let getAllPageGrids () = Entity.getAllInVenue<order_screen_page_group>
                         |> Array.filter(fun pageGroup -> pageGroup.grid_id <> 0)
                         |> Array.map(fun pageGroup -> (Entity.getById<grid> pageGroup.grid_id), pageGroup)

let getImageButtonData (button: button) =
    let item = Entity.getAllByColumn<item> "item_code" button.primary_action_value
                |> first

    let extraData =
        map [
            "data-item-code", item.item_code
            "data-item-price", item.price1.ToString()
            "data-item-name", item.item_name
            "data-item-type", item.item_type
            "data-item-category", item.item_category.ToString()
        ] |> htmlAttributes

    {|
        extra_data = extraData
        text = item.item_name
    |}


let renderButton (buttonId: int) =
    let button = Entity.getById<button> buttonId

    let extra_styles =
        match button.extra_styles.Length with
            | 0 -> ""
            | _ -> $""" style="{button.extra_styles}" """

    let imageClass = if button.image.Length > 0 then "hasImage" else ""
    let spacerClass = if button.primary_action = "spacer" || button.secondary_action = "spacer"
                        then "invisible"
                        else ""

    let image = if button.image.Length > 0
                    then loadTemplateWithVars "orderScreen/button_image" (map ["image", button.image])
                    else ""

    let extraClasses = [|imageClass; spacerClass|] |> String.concat " "

    let action_data =
        match button.primary_action with
            | "item" -> getImageButtonData button
            | "spacer" ->  {|extra_data=""; text=""|}
            | _ -> {|extra_data=""; text=""|}

    let vars = map [
        "extra_classes",  button.extra_classes + " " + extraClasses
        "extra_styles", extra_styles
        "primary_action", button.primary_action
        "secondary_action", button.secondary_action
        "text", if button.text.Length >0 then button.text else action_data.text
        "image", image
        "extra_data", action_data.extra_data
    ]

    loadTemplateWithVars "orderScreen/grid_button" vars

let renderPage (buttonHTML: string) =
    let vars = map [
        "pageButtons", buttonHTML
    ]
    loadTemplateWithVars "orderScreen/page" vars

let renderPageGroup (pageGroup: order_screen_page_group) (pageHTML: string) =
    let vars = map [
        "pages", pageHTML
        "page_group_id", pageGroup.id.ToString()
    ]
    loadTemplateWithVars "orderScreen/page_group" vars

let getPagesHTML (gridInfo: grid * order_screen_page_group) =
    let grid, pageGroup = gridInfo

    let pages = grid.grid_data |> Decode.Auto.fromString<Map<string, int[]>>

    match pages with
    | Error _ -> "Error"
    | Ok pages ->
        pages
        |> Map.toArray
        |> Array.map snd
        |> Array.map(fun row -> row |> Array.map renderButton |> String.concat "\n")
        |> Array.map renderPage
        |> String.concat "\n"
        |> renderPageGroup pageGroup