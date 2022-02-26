module Database

open Dapper
open Dapper.FSharp.PostgreSQL
open DredgeFramework
open DredgePos.Types
open Npgsql


let connect connectionString = new NpgsqlConnection(connectionString)

let getDatabaseSettings () = (getConfig ()).database

let getConnectionString () =
    let db = getDatabaseSettings ()
    $"Server={db.host};Port={db.port};User Id={db.username};Password={db.password};Database={db.db_name};Include Error Detail=true"

let connectToDatabase () = connect (getConnectionString ())

let closeAndReturn (connection: NpgsqlConnection) (result: 'a) =
    connection.Dispose()
    result

let Select<'a> asyncQuery =
    let connection = connectToDatabase ()
    asyncQuery
        |> connection.SelectAsync<'a>
        |> RunSynchronously
        |> EnumerableToArray
        |> closeAndReturn connection

let SelectJoin<'a, 'b> asyncQuery =
    let connection = connectToDatabase ()
    asyncQuery
        |> connection.SelectAsync<'a, 'b>
        |> RunSynchronously
        |> EnumerableToArray
        |> closeAndReturn connection


let Insert<'a> asyncQuery =
    let connection = connectToDatabase ()
    asyncQuery
        |> connection.InsertAsync<'a>
        |> RunSynchronously
        |> closeAndReturn connection


let InsertOutput<'a> asyncQuery =
    let connection = connectToDatabase ()
    asyncQuery
        |> connection.InsertOutputAsync<'a, 'a>
        |> RunSynchronously
        |> EnumerableToArray
        |> closeAndReturn connection

let Update<'a> asyncQuery =
    let connection = connectToDatabase ()
    asyncQuery
        |> connection.UpdateOutputAsync<'a, 'a>
        |> RunSynchronously
        |> EnumerableToArray
        |> closeAndReturn connection

let Delete<'a> asyncQuery =
    let connection = connectToDatabase ()
    asyncQuery
        |> connection.DeleteAsync
        |> RunSynchronously
        |> closeAndReturn connection

let NonDbSpecificQuery (sql: string) (connection: NpgsqlConnection) =
    sql
    |> fun str -> System.IO.File.WriteAllText("sql.log", str); str
    |> connection.Execute
    |> closeAndReturn connection

let rawQuery (sql: string) = connectToDatabase () |> NonDbSpecificQuery sql

let CreateTable (tableName: string) (columnList: (string * string) list) =
    let columns =
        columnList
        |> List.filter (fun (columnName, _) -> columnName <> "id")
        |> List.map (fun (columnName, columnType) -> $""" "{columnName}" {columnType} not null""")
        |> String.concat ",\n\t\t\t"

    $"""
    create table if not exists {tableName}
        (
            id serial
            constraint {tableName}_pk
            primary key,
            {columns}
        );
    """
    |> fun str -> System.IO.File.WriteAllText("sql.log", str); str
    |> rawQuery
    |> ignore