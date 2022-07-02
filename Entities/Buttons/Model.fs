module DredgePos.Entities.Buttons.Model

open DredgePos.Types
open DredgeFramework

let attr = Giraffe.ViewEngine.HtmlElements.attr

let getItemActionAttributes (itemCode: string) =
    let item = Entity.GetFirstByColumn<item> "code" (StringTrim itemCode)
    [(attr "data-item") <| jsonEncode item]

let getGridActionAttributes (gridId: int) = [(attr "data-grid") <| jsonEncode gridId]

let getActionAttributes (action: string) (actionValue: string) =

    match action with
        | "item" -> getItemActionAttributes actionValue
        | "grid" -> actionValue |> int |> getGridActionAttributes
        | _ -> []