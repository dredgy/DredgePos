module Decorations

open System
open System.IO
open System.Text.RegularExpressions
open DredgeFramework
open Dapper.FSharp
open DredgePos
open Types

let decorationList venue =
    select {
        table "floorplan_decorations"
        innerJoin "floorplan_rooms" "id" "decoration_room"
    }
    |> db.SelectJoin<floorplan_decoration, floorplan_room>
    |> Array.filter (fun (_, room) -> room.venue_id = venue )
    |> Array.map fst

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
    }
    |> db.InsertOutput
    |> first


let UpdateDecoration (decoration: floorplan_decoration) =
    update {
        table "floorplan_decorations"
        set decoration
        where (eq "id" decoration.id )
    } |> db.Update

let DeleteDecoration (decoration: floorplan_decoration) =
    delete {
        table "floorplan_decorations"
        where (eq "id" decoration.id)
    } |> db.Delete |> ignore
    decoration