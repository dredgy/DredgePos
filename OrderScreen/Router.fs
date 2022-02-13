module DredgePos.OrderScreen.Router

open DredgePos
open DredgePos.Types
open Saturn
open Giraffe

let router = router {
    pipe_through Ajax.Router.pipeline
    getf "/getOrderScreenData/%i" Controller.getOrderScreenData
    getf "/getGridHtml/%i" Controller.loadGrid
    post "/updateCovers" (bindJson<floorplan_table> (fun table -> Entity.Update table |> Array.head |> DredgeFramework.ajaxSuccess |> json))
    get "/" (warbler (fun ctx -> Controller.loadOrderScreen (snd ctx) 0))
    getf "/%i" (fun number -> (warbler (fun ctx -> Controller.loadOrderScreen (snd ctx) number)))
}