module Entity
open DredgePos
open Types
open Dapper.FSharp
open DredgeFramework
open Pluralize.NET.Core
open FSharp.Reflection
let getDatabaseTable<'x> =
        let typeName = typeof<'x>.Name
        Pluralizer().Pluralize typeName



let addToDatabase (record: 'x)=
    let tableName = getDatabaseTable<'x>
    insert {
        table tableName
        value record
    }
    |> db.InsertOutput
    |> first


let inline updateInDatabase (record: ^x) =
    let tableName = getDatabaseTable<'x>
    let id = ((^x) : (member id : int) (record))
    (* Run an update query *)
    update {
        table tableName
        set record
        where (eq "id" id)
    }
    |> db.Update |> ignore
    record

let getAll<'x> =
    let typeName = typeof<'x>.Name
    let tableName = Pluralizer().Pluralize typeName

    select {
        table tableName
    }
    |> db.Select<'x>

let getAllByColumn<'x> (column: string) (value: obj) =
    let typeName = typeof<'x>.Name
    let tableName = Pluralizer().Pluralize typeName

    select {
        table tableName
        where (eq column value)
    } |> db.Select<'x>

let getAllInVenue<'x> = getAllByColumn<'x> "venue_id" (getCurrentVenue ())
let getById<'x> (id: int) = getAllByColumn<'x> "id" id |> first

let deleteById<'x> id =
    let typeName = typeof<'x>.Name
    let tableName = Pluralizer().Pluralize typeName

    let entity = getById<'x>  id

    delete {
        table tableName
        where (eq "id" id)
    } |> db.Delete |> ignore

    entity