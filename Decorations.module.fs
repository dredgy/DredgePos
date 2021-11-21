module Decorations

open System
open System.IO
open System.Text.RegularExpressions
open DredgeFramework
open Dapper.FSharp
open DredgePos
open Types

let decorationsInRoom (roomId: int) = Entity.getAllByColumn "decoration_room" roomId

let getImageName (image: string, path: string) =
    let imageName =
        image
            |> StringReplace "-" " "
            |> StringReplace "_" " "
            |> ToTitleCase

    imageName, path

let isImageFile (fileName: string) = Regex.IsMatch(fileName |> ToLowerCase, @"^.+\.(jpg|jpeg|png|gif)$")

let getImageHTML (imageName: string, imageUrl: string) =
    let vars = map [
        "image_name", imageName
        "image_url", imageUrl
    ]
    Theme.loadTemplateWithVars "decoratorItem" vars

let GetFileNameWithoutExtension (path: string) =
    let name = Path.GetFileNameWithoutExtension path
    name, path |> Path.GetFileName

let getImageRowHtml (imagesInRow: string[]) =
    let vars = map ["decorations", String.Join("", imagesInRow)]
    Theme.loadTemplateWithVars "decoratorRow" vars

let generateDecorator () =
    "wwwroot/images/decorations"
        |> Directory.GetFiles
        |> Array.filter isImageFile
        |> Array.map GetFileNameWithoutExtension
        |> Array.map getImageName
        |> Array.map getImageHTML
        |> Array.chunkBySize 4
        |> Array.map getImageRowHtml
        |> JoinArray ""