module db

open Dapper
open Dapper.FSharp
open Dapper.FSharp.MySQL
open MySql.Data.MySqlClient
open DredgeFramework

let connString = "server=localhost;uid=root;pwd=;database=dredgepos;table cache = false"
let connection = new MySqlConnection(connString)

let Select<'a> asyncQuery =
    asyncQuery
        |> connection.SelectAsync<'a>
        |> RunSynchronously
        |> EnumerableToArray

let SelectJoin<'a, 'b> asyncQuery =
    asyncQuery
        |> connection.SelectAsync<'a, 'b>
        |> RunSynchronously
        |> EnumerableToArray

let Insert<'a> asyncQuery =
    asyncQuery
        |> connection.InsertAsync<'a>
        |> RunSynchronously

let InsertOutput<'a> asyncQuery =
    asyncQuery
        |> connection.InsertAsync<'a>
        |> RunSynchronously
        |> ignore

    let table = asyncQuery.Table
    connection.Query<'a>($"""Select * From {table} Where id = (select last_insert_id())""")
        |> EnumerableToArray

let Update<'a> asyncQuery =
    asyncQuery
        |> connection.UpdateAsync<'a>
        |> RunSynchronously

let Delete<'a> asyncQuery =
    asyncQuery
        |> connection.DeleteAsync
        |> RunSynchronously