module Theme

open System
open System.IO
open System.Collections.Generic
open System.Text.RegularExpressions
open FSharp.Core
open DredgeFramework

let currentTheme = "restaurant"

let getHTMLForFile file =
    let stylePath = $"/styles/css/{file}"
    let scriptPath = $"/scripts/js/{file}"
    let fileExtension = file |> getFileExtension
    let scriptFileExists =  File.Exists ("wwwroot"+stylePath) || File.Exists("wwwroot"+scriptPath)
    match scriptFileExists with
        | true ->
            match fileExtension with
            | ".css" -> $"\t<link test rel=\"stylesheet\" href=\"{stylePath}\" />"
            | ".js" ->
                    let snippet = $"\t<script src=\"{scriptPath}\"></script>"
                    snippet
            | _ -> ""
        | false -> $"\t<!--Missing File: {file}-->"


let ParseScriptsAndStylesheets files html =
    let defaultScriptsAndStyles = ["dark.theme.css"; "external/jquery.js" ; "dredgepos.core.js"; "keyboards.js";]
    let scriptsAndStylesheets = defaultScriptsAndStyles @ files

    let scriptAndStylesheetHTML =
        scriptsAndStylesheets
            |> List.map getHTMLForFile
            |> String.concat("\n")

    html |> StringReplace "</head>" (scriptAndStylesheetHTML + "\n</head>")


let titlePrefix title = title + " | DredgePos"

let ParseVariables (varArray: Map<string, string>) (html:string) =
    Regex.Replace(html, "<!--\[var\:(.*?)\]-->",
        MatchEvaluator(
            fun matchedVar ->
                let varName = matchedVar.Groups.[1] |> string |> StringTrim

                if varArray.ContainsKey varName then
                    if varName |> ToLowerCase = "title" then titlePrefix varArray.[varName]
                    else varArray.[varName]
                else
                    "<!--[Undefined Variable: " + varName + "]-->"
    ))

let ParseArrays (arrayArray: Map<string, Map<string, string>>) (string:string) =
    Regex.Replace(string, "<!--\[arr\:(.*?)\|(.*?)\]-->",
        MatchEvaluator(
            fun matchedVar ->
                let arrayName = matchedVar.Groups.[1].ToString() |> StringTrim
                let keyName = matchedVar.Groups.[2].ToString()

                if arrayArray.ContainsKey arrayName && arrayArray.[arrayName].ContainsKey keyName then
                    arrayArray.[arrayName].[keyName]
                else
                   "<!--[Undefined Array: " + arrayName + "]-->"
        )
    )

let ParseSimpleLanguageVariables (string:string) =
    Regex.Replace(string, "<!--\[lang\:(.*?)\]-->",
        new MatchEvaluator(
            fun matchedVar ->
                let varName = matchedVar.Groups.[1].ToString()
                              |> StringTrim

                language.get varName
        ))

let ParseLanguageVariablesWithReplacements (string: string) =
    Regex.Replace(string, "<!--\[lang\:(.*?)\|(.*?)\]-->",
        new MatchEvaluator(
            fun matchedVar ->
                let varName = matchedVar.Groups.[1].ToString()
                let replacements = matchedVar.Groups.[2].ToString()
                                   |> StringSplit ","
                                   |> Array.toList

                language.getAndReplace varName replacements
        ))

let getTemplateFilePath templateName =
    "wwwroot/themes/"+ currentTheme + "/" + templateName + ".tpl.htm"

let templateExists templateName =
   templateName
   |> getTemplateFilePath
   |> File.Exists

let openTemplateFile templateName =
    if templateExists templateName then
        templateName |> getTemplateFilePath |> File.ReadAllText
    else
        "<!--[Missing Template: " + templateName + "]-->"

let rec loadTemplateWithVarsArraysScriptsAndStyles templateName vars arrays scripts styles =
        templateName
            |> openTemplateFile
            |> ParseVariables vars
            |> ParseArrays arrays
            |> ParseLanguageVariablesWithReplacements
            |> ParseSimpleLanguageVariables
            |> ParseTemplates vars arrays scripts styles
            |> ParseScriptsAndStylesheets (scripts @ styles)

and ParseTemplates vars arrays scripts styles (string: string) =
    Regex.Replace(string, "<!--\[template\:(.*?)\]-->",
        new MatchEvaluator( fun template ->
            let templateName = template.Groups.[1].ToString() |> StringTrim
            loadTemplateWithVarsArraysScriptsAndStyles templateName vars arrays scripts styles
        ))

let loadTemplate templateName =
    loadTemplateWithVarsArraysScriptsAndStyles templateName Map.empty<string, string> Map.empty<string, Map<string, string>> [] []

let loadTemplateWithVars templateName vars =
    loadTemplateWithVarsArraysScriptsAndStyles templateName vars Map.empty<string, Map<string, string>> [] []

let loadTemplateWithVarsAndArrays templateName vars arrs =
    loadTemplateWithVarsArraysScriptsAndStyles templateName vars arrs [] []

let loadTemplateWithVarsAndScripts templateName vars scripts =
    loadTemplateWithVarsArraysScriptsAndStyles templateName vars Map.empty<string, Map<string, string>> scripts []

let loadTemplateWithVarsAndStyles = loadTemplateWithVarsAndScripts

let loadTemplateWithVarsScriptsAndStyles templateName vars scripts styles =
    loadTemplateWithVarsArraysScriptsAndStyles templateName vars Map.empty<string, Map<string, string>> scripts styles