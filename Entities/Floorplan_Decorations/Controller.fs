module DredgePos.Entities.Floorplan_Decorations.Controller

open DredgeFramework
open System
open System.IO

let getImageHTML (imageName: string, imageUrl: string) =
    let vars = map [
        "image_name", imageName
        "image_url", imageUrl
    ]
    Theme.loadTemplateWithVars "decoratorItem" vars

let getImageRowHtml (imagesInRow: string[]) =
    let vars = map ["decorations", String.Join("", imagesInRow)]
    Theme.loadTemplateWithVars "decoratorRow" vars

let generateDecorator () =
    "wwwroot/images/decorations"
        |> Directory.GetFiles
        |> Array.filter Model.isImageFile
        |> Array.map Model.GetFileNameWithoutExtension
        |> Array.map Model.getImageName
        |> Array.map getImageHTML
        |> Array.chunkBySize 4
        |> Array.map getImageRowHtml
        |> JoinArray ""