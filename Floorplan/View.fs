module DredgePos.Floorplan.View

open DredgePos.Global
open DredgePos.Global.View
open DredgePos.Entities
open DredgePos.Types
open Giraffe.ViewEngine
open DredgeFramework


let VisibleInMode (value: string list) = value |> jsonEncode |> (attr "data-visible-in-mode")
let InvisibleInMode (value: string list) = value |> jsonEncode |> (attr "data-invisible-in-mode")
let ActiveInMode (value: string) = value |> (attr "data-active-in-mode")


let pageContainer (clerk: clerk) roomMenu =
    let loggedInText = str (language.getAndReplace "logged_in_as" [clerk.clerk_name])

    div [_id "pageContainer"] [
        div [_id "floorplanLeftColumn"] [
            div [_class "topCell"] [
                a [_class "posHeader"] [loggedInText]
            ]
            div [_class "middleCell"] []
            div [_class "bottomCell"] []
        ]

        div [_id "floorplanCenterColumn"] [
            div [_class "topCell"] [
                yield! roomMenu
            ]
            div [_class "middleCell"] [
                div [_id "floorplanCanvas"] []
                div [_id "floorplanCanvas"] []
            ]

            div [_class "bottomCell"] [
                div [_class "editControls" ; VisibleInMode ["tableSelected"]] [
                    div [_class "posHeader currentTable"] [
                        b [_class "selectedTableNumber"] []
                        a [_class "reservationStatus"; VisibleInMode ["reservedTableSelected"]] []
                        small [_class "selectedTableCovers"] []
                    ]

                    a [_class "posButton placeOrderButton"] [lang "order_table"]
                    a [_class "posButton reserveTableButton";  InvisibleInMode ["reservedTableSelected"; "activeTableSelected"]] [lang "reserve_table"]
                    a [_class "posButton unreserveTableButton";  VisibleInMode ["reservedTableSelected"]] [lang "unreserve_table"]
                    a [_class "posButton payTableButton";  VisibleInMode ["activeTableSelected"]] [lang "pay_table"]
                    a [_class "posButton viewTableButton";  VisibleInMode ["activeTableSelected"]] [lang "view_table"]
                ]
            ]
        ]

        div [_id "floorplanRightColumn"] [
            div [_class "topCell"] [
                a [_class "posButton logOut"] [str "×"]
            ]

            div [_class "middleCell"] [
                a [_class "posButton editModeButton"] [lang "edit_floorplan"]
                div [_class "floorplanControls useVisibility"; VisibleInMode ["edit"]] [
                    a [_class "posButton addTableButton"] [lang "add_table"]
                    a [_class "posButton addDecoration"] [lang "add_decoration"]
                    a [_class "posButton deleteDecoration useVisibility"; VisibleInMode ["decorationSelected"; "edit"] ] [lang "delete_decoration"]
                    a [_class "posButton deleteTableButton useVisibility"; VisibleInMode ["tableSelected"; "edit"]] [lang "delete_table"]
                    a [_class "posButton changeShapeButton useVisibility"; VisibleInMode ["tableSelected"; "edit"]] [lang "change_shape"]
                ]

                div [_class "mergeControls useVisibility"; VisibleInMode ["tableSelected"]] [
                    a [_class "posButton mergeButton"; ActiveInMode "merge"] [lang "merge_table"]
                    a [_class "posButton unmergeButton"; VisibleInMode ["tableSelected"]] [lang "unmerge_table"]
                    a [_class "posButton transferTableButton" ; ActiveInMode "transfer" ; VisibleInMode ["activeTableSelected"]] [lang "transfer_table"]
                ]
            ]

            div [_class "bottomCell"] []
        ]
    ]

let roomButton (room: floorplan_room) = a [_class "posButton roomButton"; Value (string room.id)] [str room.room_name ]

let index styles scripts tags clerk decoratorRows roomMenu =
    [|
        pageContainer clerk roomMenu
        decoratorRows |> Floorplan_Decorations.View.decorator
    |]
    |> HtmlPage "Floorplan" (GetScripts scripts) (GetStyles styles) (GetMetaTags tags)

