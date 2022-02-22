module DredgePos.Entities.Floorplan_Decorations.Controller

open System.IO

let generateDecorator () =
    "wwwroot/images/decorations"
        |> Directory.GetFiles
        |> Array.filter Model.isImageFile
        |> Array.map Model.GetFileNameWithoutExtension
        |> Array.map Model.getImageName
        |> Array.map View.decoratorItem
        |> Array.chunkBySize 4
        |> Array.map View.decoratorRow