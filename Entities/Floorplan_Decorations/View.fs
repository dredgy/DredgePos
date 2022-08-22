module DredgePos.Entities.Floorplan_Decorations.View

open Giraffe.ViewEngine
open DredgePos.Global.View

let decoratorItem (imageName, imageUrl) =
    let image = attr "data-image"
    div [_class "decoratorItem"; image imageUrl] [
        a [_style $"background-image:url('/images/decorations/{imageUrl}')"] []
        a [] [str imageName]
    ]

let decoratorRow decoratorItems = div [_class "decoratorRow"] [yield! decoratorItems]

let decorator (decorationRows: XmlNode[]) =
    div [_id "decorator"; _class "modal"] [
        div [_id "decoratorHeader"] [
            h2 [] [lang "choose_decoration"]
            a [_class "posButton hideModals"] [str "×"]
        ]
        div [_id "decoratorContent"] [
            yield! decorationRows
        ]
    ]