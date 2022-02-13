module DredgePos.Reservations.Controller

open DredgeFramework
open DredgePos
open DredgePos.Types
open Giraffe

let newEmptyReservation (reservation: reservation) =
    let newReservation = {reservation with
                            created_at = CurrentTime()
                            time = CurrentTime()
                          }

    if reservation.floorplan_table_id > 0 then
        let table = {(Entity.GetById<floorplan_table> reservation.floorplan_table_id) with
                        status = 2
                        default_covers = reservation.covers}
        Floorplan.Model.updateTablePosition table |> ignore

    let createdReservation = Floorplan.Model.createEmptyReservation newReservation
    ajaxSuccess createdReservation |> json

let updateReservation (reservation: reservation) = Model.updateReservation reservation |> ajaxSuccess |> json

let unreserveTable (table: floorplan_table) =
    let newTable = {table with status = 0}
    Floorplan.Model.updateTablePosition newTable |> ignore
    Model.DeleteReservation newTable.id
    newTable |> ajaxSuccess |> json