module Clerk

open System
open Dapper.FSharp
open DredgeFramework
open Renci.SshNet
open Thoth.Json.Net

let mutable loginCookie = ""

type clerk = {clerk_id: int; clerk_name: string; clerk_login_code: int; clerk_usergroup: int}
let clerk_decoder : Decoder<clerk> =
        Decode.object
            (fun get ->
                {
                    clerk_id = get.Required.Field "clerk_id" Decode.int
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