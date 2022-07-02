module DredgePos.OrderScreen.Model

open DredgeFramework
open DredgePos
open DredgePos.Types
open FSharp.Collections
open Thoth.Json.Net
open Theme


let getAllPageGridsInVenue () =
    Entity.GetAllInVenue<order_screen_page_group>
    |> Array.filter(fun pageGroup -> pageGroup.grid_id <> 0)
    |> Array.map(fun pageGroup -> (Entity.GetById<grid> pageGroup.grid_id), pageGroup)

let printGroupPosButton (printGroup: print_group) =
    PosButton (language.getAndReplace "print_with" [printGroup.name]) "printGroupOverrideButton toggle" $"""data-value="{printGroup.id}" """

