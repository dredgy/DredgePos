module DredgePos.OrderScreen.View

open DredgePos.Types
open DredgePos.Global.View
open DredgePos.Global.Controller
open Giraffe.ViewEngine
open language

let coverSelector = div [_class "coverSelector"] []

let pageContainer floorplanTable (clerk: clerk) orderScreenPageGroups =
    div [_id "pageContainer" ; _table floorplanTable] [
        div [_id "leftColumn"] [
            h1 [_class "tableHeading"] [str (getAndReplace "active_table" [floorplanTable.table_number])]
            div [_class "tableInfo"] [
                coverSelector
                posButton "" [] [str (getAndReplace "logged_in_as" [clerk.name])]
            ]
            div [_class "orderBox"] [
                table [_class "orderBoxTable"] [
                    thead [] [
                        tr [] [
                            th [_class "orderBoxCell qtyCell"] [str (get "qty_header")]
                            th [_class "orderBoxCell itemIdCell"] [str (get "id_header")]
                            th [_class "orderBoxCell itemCell"] [str (get "item_header")]
                            th [_class "orderBoxCell unitPriceCell"] [str (get "price_header")]
                            th [_class "orderBoxCell totalPriceCell"] [str (get "total_price_header")]
                            th [_class "orderBoxCell printGroupCell"] [str (get "printgroup_header")]
                        ]
                    ]
                    tbody [] []
                ]
            ]
            div [_class "orderBoxInfo"] [
                span [_class "voidModeWarning"; VisibleInMode ["void"]] [str (get "void_mode")]
            ]
            div [_class "orderBoxFooter"] [
                span [_class "totalPrice"] [str (getAndReplace "totalPrice" ["0.00"])]
                small [_class "selectedPrice"] [str (getAndReplace "selectedPrice" ["0.00"])]

            ]
        ]
        div [_id "rightColumn"] [
            div [_id "topHalf"] [
                div [_class "functionButtons"] [
                    div [_class "printGroupButtons toggleGroup"] [
                        input [_type "hidden"; _class "value"]
                        (* Sales category override buttons *)
                    ]
                    div [_class "functionColumn"] [
                        posButton "accumulateButton" [ActiveInMode "accumulate"] [str (get "accumulate_function")]
                        posButton "showCoverSelectorButton" [] [str (get "select_covers")]
                    ]
                    div [_class "functionColumn"] [
                        posButton "voidButton"  [ActiveInMode "void"] [str (get "void")]
                        posButton "open_item_button"  [] [str (get "custom_item_button")]
                        posButton "freetextButton"  [] [str (get "freetext_button")]
                        posButton "numpadButton"  [] [str (get "numpad_button")]
                    ]
                    div [_class "functionColumn"] [
                        posButton "" [] ["pay_function" |> get |> str]
                        posButton "" [] ["print_function" |> get |> str]
                    ]
                 ]
                div [_id "pageList"] [
                    yield! orderScreenPageGroups
                ]
                div [_id "pageGroupContainer"] [
                    (* Page Groups *)
                ]
                div [_class "pagNavigation"] [
                    posButton "prevButton" [] ["prev_page" |> get |> str]
                    posButton "nextButton" [] ["next_page" |> get |> str]
                ]
            ]
        ]
    ]
    (* Grid Container, Cover Selector *)

let posButtonTemplate =
    template [_id "posButtonTemplate"] [
        posButton "" [] []
    ]

let gridContainer =
    div [_class "gridContainer"] [
        div [_class "gridContainerHeader"] [
            span [] []
            div [_class "posButton closeGrid"] [str "×"]
        ]
        div [_class "gridContainerGrid"] [
            div [_class "pageGroup"] []
        ]
        div [_class "pageNavigation"] [
            posButton "prevButton" [] ["prev_page" |> get |> str]
            posButton "nextButton" [] ["next_page" |> get |> str]
        ]
    ]

let pageGroupButton (pageGroup: order_screen_page_group) = posButton "loadPageGroup" [] [str pageGroup.label]

let index orderNumber styles scripts tags clerk (orderScreenPageGroups: order_screen_page_group[])  =

    let orderScreenPageGroupButtons =
        orderScreenPageGroups
        |> Array.map pageGroupButton

    [|
        pageContainer (DredgePos.Floorplan.Model.getTable orderNumber) clerk orderScreenPageGroupButtons
        posButtonTemplate
        gridContainer
        coverSelector
    |]
    |> HtmlPage "Order" (GetScripts scripts) (GetStyles styles) (GetMetaTags tags)