module Clerk

open Dapper.FSharp
open DredgeFramework
open DredgePos
open Thoth.Json.Net
open Types

let mutable loginCookie = ""

let clerk_decoder : Decoder<clerk> =
        Decode.object
            (fun get ->
                {
                    id = get.Required.Field "clerk_id" Decode.int
                    clerk_name = get.Required.Field "clerk_name" Decode.string
                    clerk_login_code = get.Required.Field "clerk_login_code" Decode.int
                    clerk_usergroup = get.Required.Field "clerk_usergroup" Decode.int
                })


type user = {clerk_name:string}

let getClerkByLoginCode (loginCode: int) =
    let clerk =
        select {
            table "clerks"
            where (eq "clerk_login_code" loginCode)
            take 1
        }
        |> db.Select<clerk>
        |> EnumerableToArray

    if (clerk |> length) > 0 then
        Some (first clerk)
    else
        None