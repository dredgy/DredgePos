module DredgePos.Authenticate.Model

open System
open DredgeFramework
open Dapper.FSharp
open DredgePos
open Thoth.Json.Net
open Types

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

let deleteSession sessionId context =
    delete {
        table "sessions"
        where (eq "session_id" sessionId)
    } |> db.Delete |> ignore
    Browser.deleteCookie "dredgepos_clerk_logged_in" context

let deleteSessionByClerkId clerk_id context =
    delete {
        table "sessions"
        where (eq "clerk_id" clerk_id)
    } |> db.Delete |> ignore

    Browser.deleteCookie "dredgepos_clerk_logged_in" context

let createNewSession (clerk: clerk) context =
    if (getClerkByLoginCode clerk.clerk_login_code).IsSome then
        deleteSessionByClerkId clerk.id context
        let newSessionId = (Guid.NewGuid().ToString "N") + (Guid.NewGuid().ToString "N")

        let newSession = {
                            id = 0
                            session_id = newSessionId
                            clerk_json = clerk |> jsonEncode
                            clerk_id = clerk.id
                            expires = int <| DateTimeOffset.Now.AddHours(24.0).ToUnixTimeSeconds()
                         }
        insert {
            table "sessions"
            value newSession
        }
        |> db.Insert
        |> ignore

        Browser.setCookie "dredgepos_clerk_logged_in" newSessionId (DateTimeOffset.UtcNow.AddHours(24.0)) context


let sessionExists (sessionId: string) context =
    let sessions =
        select {
            table "sessions"
            where (eq "session_id" sessionId)
        } |> db.Select<session>

    match sessions |> length with
    | 0 -> false
    | 1 -> true
    | _ ->
        (* Two sessions have same id :(
           About the same odds as winning the lottery every
           day until the heat death of the universe, but still
           better account for it *)
        deleteSession sessionId context
        false

let checkAuthentication clerk =
    let existingClerk = getClerkByLoginCode clerk.clerk_login_code
    existingClerk.IsSome
    && existingClerk.Value.id = clerk.id
    && existingClerk.Value.clerk_name = clerk.clerk_name
    && existingClerk.Value.clerk_login_code = clerk.clerk_login_code

let getLoginCookie context = Browser.getCookie "dredgepos_clerk_logged_in" context

let getSession (sessionId: string) =
    let sessions =
        select {
            table "sessions"
            where (eq "session_id" sessionId)
        } |> db.Select<session>

    match sessions |> length with
    | 0 -> {session_id = ""; clerk_json = ""; clerk_id= 0; expires= 0; id=0}
    | _ -> sessions |> first

let getCurrentClerk context =
    let cookie = getLoginCookie context
    let emptyClerk = {id=0; clerk_login_code=0; clerk_usergroup=0; clerk_name=""}
    match cookie with
    | "" ->
        Browser.redirect "/login" context
        emptyClerk
    | _ ->
        let session = getSession cookie
        let clerkResult = session.clerk_json |> Decode.Auto.fromString<clerk>
        match clerkResult with
        | Error _ ->
            Browser.redirect "/login" context
            emptyClerk
        | Ok clerk -> clerk


let authenticated context =
    let loginCookie = getLoginCookie context
    match loginCookie with
    | "" -> false
    | _ ->
        let currentSession = getSession loginCookie

        match currentSession.clerk_id with
        | 0 -> false
        | _ ->
             let decode = currentSession.clerk_json |>  Decode.Auto.fromString<clerk>
             match decode with
                | Ok clerk -> checkAuthentication clerk
                | Error _ -> false

let RequireClerkAuthentication context =
    if not (authenticated context) then
        Browser.redirect("/login") context

let clerkLogin loginCode context =
    let clerk = getClerkByLoginCode loginCode
    if clerk.IsSome then
        createNewSession clerk.Value context
        true
    else false