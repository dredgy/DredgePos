module Reservations

open System
open DredgeFramework
open Dapper.FSharp
[<CLIMutable>]
type reservation = {
    id: int
    reservation_name: string
    reservation_time: int
    reservation_covers: int
    reservation_table_id: int
    reservation_created_at: int
}

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