module DredgePos.Authenticate.Controller

open Giraffe
open DredgeFramework
open Microsoft.AspNetCore.Http

let loadAuthenticatePage (): HttpHandler =
     let variables = map ["title", "Log In"]
     let scripts = ["dredgepos.authenticate.js"]
     let styles = ["dredgepos.authenticate.css"]

     Theme.loadTemplateWithVarsScriptsAndStyles "authenticate" variables scripts styles
        |> htmlString

let loginWithLoginCode (context: HttpContext) (login_code: int) =
     if Model.clerkLogin login_code context then ajaxSuccess "success"
     else ajaxFail "fail"