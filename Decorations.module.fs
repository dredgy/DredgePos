module Decorations

open System
open System.IO
open System.Text.RegularExpressions
open DredgeFramework
open Dapper
open Dapper.FSharp

[<CLIMutable>]
type floorplan_decoration = {
    decoration_id: int
    decoration_room: int
    decoration_pos_x: int
    decoration_pos_y: int
    decoration_rotation: int
    decoration_width: int
    decoration_height: int
    decoration_image: string
}

type decoration_creator = {
    decoration_room: int
    decoration_image: string
    basis: int
}


let decorationsInRoom (roomId: int) =
    select {
        table "floorplan_decorations"
        where (eq "decoration_room" roomId)
    }
    |> db.Select<floorplan_decoration>


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

let CreateDecoration (decoration: floorplan_decoration) =
    insert {
        table "floorplan_decorations"
        value decoration
    } |> db.Insert

let UpdateDecoration (decoration: floorplan_decoration) =
    let imageFile = GetFileName decoration.decoration_image
    let updatedDecoration  = {decoration with decoration_image = imageFile}

    update {
        table "floorplan_decorations"
        set updatedDecoration
        where (eq "decoration_id" decoration.decoration_id )
    } |> db.Update

let DeleteDecorationById (id: int) =
    delete {
        table "floorplan_decorations"
        where (eq "decoration_id" id)
    } |> db.Delete