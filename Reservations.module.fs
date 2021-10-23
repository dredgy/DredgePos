module Reservations

open System
open DredgeFramework
open Dapper.FSharp

type reservation = {
    reservation_id: int
    reservation_name: string
    reservation_time: int
    reservation_covers: int
    reservation_table_id: int
    reservation_created_at: int
}

let GetReservationById (id: int) =
    select {
        table "reservations"
        where (eq "reservation_id" id)
    }
    |> db.Select<reservation>
    |> first

let DeleteReservation (tableId: int) =
    delete {
        table "reservations"
        where (eq "table_id" tableId)
    } |> db.Delete |> ignore