module DredgePos.Ajax.Controller

open DredgeFramework
open language
open Giraffe

let getLanguageVars = ajaxSuccess languageVars

let getKeyboardLayout (language: string) =
    let layout = $"""wwwroot/languages/{language}/keyboardLayout.json"""
                 |> GetFileContents
    map [
            "status", "success"
            "data", layout
        ] |> json