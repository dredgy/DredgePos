module DredgePos.OrderScreen.View

open DredgeFramework
open DredgePos.Types
open DredgePos.Global.View
open DredgePos.Entities.Buttons.Model
open Thoth.Json.Net
open Giraffe.ViewEngine
open language

let coverSelector buttons = div [_class "coverSelector"] [
    yield! buttons
]

let pageContainer floorplanTable (clerk: clerk) printGroupButtons orderScreenPageGroupButtons pageGroups =
    div [_id "pageContainer" ; _table floorplanTable] [
        div [_id "leftColumn"] [
            h1 [_class "tableHeading"] [str (getAndReplace "active_table" [floorplanTable.table_number])]
            div [_class "tableInfo"] [
                posButton "changeCoverNumberButton" [] [str (getAndReplace "covers" [floorplanTable.default_covers])]
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
                span [_class "orderBoxTotal"] [str (getAndReplace "totalPrice" ["0.00"])]
                small [_class "orderBoxSelectedTotal"] [str (getAndReplace "selectedPrice" ["0.00"])]

            ]
        ]
        div [_id "rightColumn"] [
            div [_id "topHalf"] [
                div [_class "functionButtons"] [
                    div [_class "printGroupButtons toggleGroup"] [
                        input [_type "hidden"; _class "value"; _name "print_override"]
                        posButton "printGroupOverrideButton toggle default" [
                            (attr "data-value") (string 0)
                        ] [
                            ["default"] |> getAndReplace "print_with" |> str
                        ]
                        yield! printGroupButtons
                    ]
                    div [_class "functionColumn"] [
                        posButton "accumulateButton" [ActiveInMode "accumulate"] [str (get "accumulate_function")]
                        posButton "showCoverSelectorButton" [] [str (get "select_covers")]
                    ]
                    div [_class "functionColumn"] [
                        posButton "voidButton"  [ActiveInMode "void"] [str (get "void")]
                        posButton "openItemButton"  [] [str (get "custom_item_button")]
                        posButton "freetextButton"  [] [str (get "freetext_button")]
                        posButton "numpadButton"  [] [str (get "numpad_button")]
                    ]
                    div [_class "functionColumn"] [
                        posButton "" [] ["pay_function" |> get |> str]
                        posButton "" [] ["print_function" |> get |> str]
                    ]
                 ]
            ]
            div [_id "pageList"] [
                yield! orderScreenPageGroupButtons
            ]
            div [_id "pageGroupContainer"] [
                yield! pageGroups
            ]
            div [_class "pageNavigation"] [
                posButton "prevButton" [] ["prev_page" |> get |> str]
                posButton "nextButton" [] ["next_page" |> get |> str]
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

let pageGroupButton (pageGroup: order_screen_page_group) = posButton "loadPageGroup" [(attr "data-page-group-id") (string pageGroup.id)] [str pageGroup.label]
let printGroupButton (printGroup: print_group) = posButton "toggle printGroupOverrideButton" [(attr "data-value") (string printGroup.id)] [ [printGroup.name] |> getAndReplace "print_with" |> str ]

let itemButtonImage (button: button) =
    span [
        _class "buttonImg"
        _style $"background-image:url(\"/images/items/{button.image}\");"
    ] []

let itemButton (button: button) =
    let extraClasses =
        if button.image.Length > 0 then button.extra_classes + " hasImage"
        else button.extra_classes

    let primaryAttributes = getActionAttributes button.primary_action button.primary_action_value
    let secondaryAttributes = getActionAttributes button.secondary_action button.secondary_action_value

    posButton extraClasses [
        yield! primaryAttributes
        yield! secondaryAttributes
        _style button.extra_styles
        (attr "data-primary-action") button.primary_action
        (attr "data-secondary-action") button.secondary_action
    ] [
        if button.image.Length > 0 then itemButtonImage button
        span [_class "text "] [str button.text]
    ]

let _dataPageGroup = attr "data-page-group"
let _dataPageGroupId = attr "data-page-group-id"

let pageGroup (page_group: order_screen_page_group) gridNodes  =
    div [_class "pageGroup"; _dataPageGroupId (string page_group.id); ] [
        yield! gridNodes
    ]

let gridPage (grid: grid) buttonNodes =
    div [
        _class "gridPage"
        _style $"
        grid-template-columns: repeat({grid.cols}, 1fr);
        grid-template-rows: repeat({grid.rows}, 1fr);"
    ] [
        yield! buttonNodes
    ]


let index orderNumber styles scripts tags clerk printGroupButtons orderScreenPageGroupButtons pageGroupNodes coverSelectorButtons  =
    [|
        pageContainer (DredgePos.Floorplan.Model.getTable orderNumber) clerk printGroupButtons orderScreenPageGroupButtons pageGroupNodes
        posButtonTemplate
        gridContainer
        coverSelector coverSelectorButtons
    |]
    |> HtmlPage "Order" (GetScripts scripts) (GetStyles styles) (GetMetaTags tags)