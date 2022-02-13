module DredgePos.Entities.Floorplan_Decorations.Model

open System
open System.IO
open System.Text.RegularExpressions
open DredgeFramework

let decorationsInRoom (roomId: int) = Entity.GetAllByColumn "decoration_room" roomId

let getImageName (image: string, path: string) =
    let imageName =
        image
            |> StringReplace "-" " "
            |> StringReplace "_" " "
            |> ToTitleCase

    imageName, path

let isImageFile (fileName: string) = Regex.IsMatch(fileName |> ToLowerCase, @"^.+\.(jpg|jpeg|png|gif)$")

let GetFileNameWithoutExtension (path: string) =
    let name = Path.GetFileNameWithoutExtension path
    name, path |> Path.GetFileName

