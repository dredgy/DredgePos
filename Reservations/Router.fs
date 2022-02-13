module DredgePos.Reservations.Router

open DredgePos
open DredgePos.Types
open Saturn
open Giraffe

let router = router {
    pipe_through Ajax.Router.pipeline
    post "/newEmptyReservation" (bindJson<reservation> Controller.newEmptyReservation)
    post "/updateReservation" (bindJson<reservation> Controller.updateReservation)
    post "/unreserveTable" (bindJson<floorplan_table> Controller.unreserveTable )
}