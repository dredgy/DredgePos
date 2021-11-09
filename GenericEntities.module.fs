module Entity
open DredgePos
open Types
open Dapper.FSharp
open DredgeFramework

let getDatabaseTable (record: 'a) = record.GetType().ToString().ToLower() + "s"

let addToDatabase (record: 'x)=
    let tableName = getDatabaseTable record
    insert {
        table tableName
        value record
    }
    |> db.InsertOutput
    |> first

let updateInDatabase (record: 'x) =
    let tableName = getDatabaseTable record
    (* Run an update query *)
    update {
        table tableName
        set record
    }
    |> db.Update |> ignore
    record