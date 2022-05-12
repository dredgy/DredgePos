module DredgePos.Global.View

open DredgeFramework
open DredgePos.Types
open Giraffe.ViewEngine

let Value = attr "data-value"
let _table (value: floorplan_table) = value |> jsonEncode |> (attr "data-table")

let VisibleInMode (value: string list) = value |> jsonEncode |> (attr "data-visible-in-mode")
let InvisibleInMode (value: string list) = value |> jsonEncode |> (attr "data-invisible-in-mode")
let ActiveInMode (value: string) = value |> (attr "data-active-in-mode")
let innerText = str
let lang key =  language.get key |> str

let template = tag "template"

let scriptToHTML (scriptFile: string) =
    let scriptPath = $"/scripts/{scriptFile}"
    match FileExists ("wwwroot" + scriptPath) with
        | true -> script [_src scriptPath] []
        | false -> comment $"[Missing script: {scriptFile}]"
let GetScripts (scripts: string[]) = scripts |> Array.map scriptToHTML

let styleToHTML (stylesheet:string) =
    let stylePath = $"/styles/{stylesheet}"
    match FileExists ("wwwroot" + stylePath) with
        | true -> link [_rel "stylesheet" ; _href stylePath]
        | false -> comment $"[Missing style: {stylesheet}]"

let GetStyles (scripts: string[]) = scripts |> Array.map styleToHTML

let tagToHtml (tag, content) = meta [_name tag; _content content]

let GetMetaTags (tags: (string * string)[]) = tags |> Array.map tagToHtml

let VirtualKeyboardRow numberOfButtons =
    let buttons = Array.init numberOfButtons (fun _ -> a [] [])
    div [_class "virtualKeyboardRow"] [
        yield! buttons
    ]

let VirtualKeyboard =
    div [_id "virtualKeyboard"] [
        div [_class "headingRow"] [
            h3 [_id "virtualKeyboardHeading"] []
            a [_class "posButton closeKeyboards"] [str "X"]
        ]
        input [_type "text"; _name "virtualKeyboardInput"; _id "virtualKeyboardInput"]
        div [_id "virtualKeyboardButtons"] [
            VirtualKeyboardRow 13
            VirtualKeyboardRow 14
            VirtualKeyboardRow 13
            VirtualKeyboardRow 11
            VirtualKeyboardRow 1
        ]
        span [_class "forceFocus"] []
    ]

let VirtualNumpadButton (text: string) =
    a [_href "#"; Value (text.ToLower()); _class "posButton virtualNumpadButton"] [str text]

let VirtualNumpadRow (buttons:string[]) =
    div [_class "virtualNumpadRow"] [
        yield! Array.map VirtualNumpadButton buttons
    ]

let VirtualNumpad =
    div [_id "virtualNumpad"] [
        div [_class "headingRow"] [
            h3 [_id "virtualNumpadHeading"] []
            a [_class "posButton closeKeyboards"] [str "X"]
        ]
        div [_id "virtualNumpadInput"] []
        div [_id "virtualNumpadButtons"] [
            VirtualNumpadRow [|"1"; "2"; "3"|]
            VirtualNumpadRow [|"4"; "5"; "6"|]
            VirtualNumpadRow [|"7"; "8"; "9"|]
            VirtualNumpadRow [|"0"; "."; "Clear"|]
            VirtualNumpadRow [|"Submit"|]
        ]
    ]

let alert =
    div [_id "alert"] [
        div [_id "alertHeading"] []
        div [_id "alertMessage"] []
        div [_id "alertButtons"] [
            a [_class "posButton"; _id "alertOk"] [lang "alert_ok"]
            a [_class "posButton"; _id "alertYes"] [lang "alert_yes"]
            a [_class "posButton"; _id "alertNo"] [lang "alert_no"]
        ]
    ]
let keyboards = [|
    VirtualKeyboard
    VirtualNumpad
    alert
|]

let posButton (extraClasses: string) attrs content =
    let allAttrs = [_class $"posButton {extraClasses}"] |> List.append attrs
    a allAttrs content

let HtmlPage pageTitle scripts styles tags content =
    html [] [
        head [] [
            title [] [innerText pageTitle]
            link [_rel "manifest" ; _href "/manifest.webmanifest"]
            yield! styles
            yield! scripts
            yield! tags
        ]
        body [] [
            yield! content
            yield! keyboards
        ]
    ]

let HTMLPageWithScripts pageTitle scripts content = HtmlPage pageTitle scripts [||] content
let HTMLPageWithStyles pageTitle styles content = HtmlPage pageTitle [||] styles content
let HTMLPageWithNoScriptsOrStyles pageTitle content = HtmlPage pageTitle [||] [||] content
