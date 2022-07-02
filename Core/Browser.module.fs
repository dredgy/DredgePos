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

let getCookie cookieName (context: HttpContext) =
    context.Request.Cookies[cookieName] |? ""

let setCookie name value (expiry: DateTimeOffset) (context: HttpContext) =
    deleteCookie name context
    let options = CookieOptions()
    options.Expires <- expiry
    context.Response.Cookies.Append(name, value, options);

let redirect url (context: HttpContext) = context.Response.Redirect url