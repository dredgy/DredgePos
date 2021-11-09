module Reservations

open System
open DredgeFramework
open Dapper.FSharp
open DredgePos
open Types

let GetReservationById (id: int) =
    select {
        table "reservations"
        where (eq "id" id)
    }
    |> db.Select<reservation>
    |> first

let updateReservation (reservation: reservation) =
    update{
        table "reservations"
        set reservation
        where(eq "id" reservation.id)
    } |> db.Update |> ignore
    reservation

let DeleteReservation (tableId: int) =
    delete {
        table "reservations"
        where (eq "reservation_table_id" tableId)
    } |> db.Delete |> ignore