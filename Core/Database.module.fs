module db

open Dapper
open Dapper.FSharp
open Dapper.FSharp.PostgreSQL


open DredgeFramework

let connString = "Server=localhost;Port=5432;User Id=postgres;Password=root;Database=dredgepos;Include Error Detail=true"
//let connString = "server=localhost;uid=root;pwd=;database=dredgepos;table cache = false"
let connection = new Npgsql.NpgsqlConnection(connString)

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
        |> connection.InsertOutputAsync<'a, 'a>
        |> RunSynchronously
        |> EnumerableToArray

let Update<'a> asyncQuery =
    asyncQuery
        |> connection.UpdateOutputAsync<'a, 'a>
        |> RunSynchronously
        |> EnumerableToArray

let Delete<'a> asyncQuery =
    asyncQuery
        |> connection.DeleteAsync
        |> RunSynchronously