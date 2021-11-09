module Browser

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open DredgeFramework


let cookieExists name (context: HttpContext) =
    context.Request.Cookies.ContainsKey(name)

let deleteCookie name (context: HttpContext) =
    if cookieExists name context then context.Response.Cookies.Delete(name)

let test = 0

let getCookie cookieName (context: HttpContext) =
    context.Request.Cookies.[cookieName] |? ""

let setCookie name value (expiry: DateTimeOffset) (context: HttpContext) =
    deleteCookie name context
    let options = CookieOptions()
    options.Expires <- expiry
    context.Response.Cookies.Append(name, value, options);

let redirect url (context: HttpContext) =
    context.Response.Redirect url

let addRoute path controller (endpoints: IEndpointRouteBuilder) =
    endpoints.MapGet(path, fun context ->
                    context.Response.WriteAsync(controller())) |> ignore
    endpoints

let addRouteWithParameter path controller param1 (endpoints: IEndpointRouteBuilder)  =
    endpoints.MapGet(path, fun context ->
                    let param1Name, param1Type = param1
                    let parameter1 = context.Request.RouteValues.[param1Name] |> string |> param1Type
                    context.Response.WriteAsync(controller parameter1)) |> ignore
    endpoints

let addRouteWithParameters path controller param1 param2 (endpoints: IEndpointRouteBuilder) =
    endpoints.MapGet(path, fun context ->
                    let param1Name, param1Type = param1
                    let param2Name, param2Type = param2
                    let parameter1 = context.Request.RouteValues.[param1Name] |> string |> param1Type
                    let parameter2 = context.Request.RouteValues.[param2Name] |> string |> param2Type
                    context.Response.WriteAsync(controller parameter1 parameter2)) |> ignore
    endpoints