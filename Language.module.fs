module language

open System.IO
open System.Text.RegularExpressions
open Thoth.Json.Net
open FSharp.Data
let defaultLanguage = "english"
let languageFile =  "wwwroot/languages/" + defaultLanguage + "/main.json"
let languageData = languageFile |> File.ReadAllText


//Returns an array of all language variables as defined in the the language file.
let languageVars =
    languageData
        |> Decode.unsafeFromString (Decode.keyValuePairs Decode.string)
        |> Map.ofList


//Gets a value of a language variable
let get var =
    if languageVars.ContainsKey var then
        languageVars[var]
    else
        "Missing language variable: " + var

let getAndReplace languageVar replacements =
    let langString = get languageVar
    replacements
        |> List.mapi (fun index string
                       -> index + 1, string)
        |> List.fold (fun (result: string) (index, string)
                         -> result.Replace($"[{index}]", string)
                      ) langString