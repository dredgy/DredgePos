module DredgePos.Reservations.Model

open DredgeFramework
open Dapper.FSharp
open DredgePos
open Types

let updateReservation (reservation: reservation) =
    update{
        table "reservations"
        set reservation
        where(eq "id" reservation.id)
    } |> Database.Update |> ignore
    reservation

let DeleteReservation (tableId: int) =
    delete {
        table "reservations"
        where (eq "floorplan_table_id" tableId)
    } |> Database.Delete |> ignore