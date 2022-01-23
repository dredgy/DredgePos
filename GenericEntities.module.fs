module Entity
open Dapper.FSharp
open DredgeFramework
open Pluralize.NET.Core
open FSharp.Reflection

let GetDatabaseTable<'x> =
        let typeName = typeof<'x>.Name
        Pluralizer().Pluralize typeName

let Create (record: 'x)=
    let tableName = GetDatabaseTable<'x>
    insert {
        table tableName
        value record
        excludeColumn "id"
    }
    |> db.InsertOutput
    |> first


let inline Update (record: ^x) =
    let tableName = GetDatabaseTable<'x>
    let id = ((^x) : (member id : int) record)
    update {
        table tableName
        set record
        where (eq "id" id)
        excludeColumn "id"
    }
    |> db.Update

let GetAll<'x> =
    let tableName = GetDatabaseTable<'x>

    select {
        table tableName
    }
    |> db.Select<'x>

let GetAllByColumn<'x> (column: string) (value: obj) =
    let tableName = GetDatabaseTable<'x>

    select {
        table tableName
        where (eq column value)
    } |> db.Select<'x>

let GetAllInVenue<'x> = GetAllByColumn<'x> "venue_id" (getCurrentVenue ())
let GetById<'x> (id: int) = GetAllByColumn<'x> "id" id |> first

let inline GetRelated<'x, .. > (entity: ^y) =
    let columnName = typeof<'x>.Name + "_id"
    let primaryKeyValue = typeof<'y>.GetProperty(columnName).GetValue(entity) :?> int
    GetById<'x> primaryKeyValue

let inline GetAllRelated<'x, .. > (entity: ^y) =
    let id = typeof<'y>.GetProperty("id").GetValue(entity) :?> int
    let columnName = typeof<'y>.Name + "_id"
    GetAllByColumn<'x> columnName id

let DeleteById<'x> id =
    let typeName = typeof<'x>.Name
    let tableName = Pluralizer().Pluralize typeName
    let entity = GetById<'x>  id

    delete {
        table tableName
        where (eq "id" id)
    } |> db.Delete |> ignore

    entity

let inline Delete< ^x when  ^x: (member id: int) > (entity: ^x) =
    typeof<'x>.GetProperty("id").GetValue(entity) :?> int
    |> DeleteById<'x>
