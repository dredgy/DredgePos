module DredgePos.Authenticate.Controller

open DredgeFramework
open Microsoft.AspNetCore.Http
open DredgePos.Global.Controller

let loadAuthenticatePage =
    let scripts = [|"dredgepos.authenticate.js"|] |> addDefaultScripts
    let styles = [|"dredgepos.authenticate.css"|] |> addDefaultStyles
    let metaTags = [|"viewport", "user-scalable = no, initial-scale=0.8,maximum-scale=0.8 ,shrink-to-fit=yes"|] |> addDefaultMetaTags

    View.index scripts styles metaTags

let loginWithLoginCode (context: HttpContext) (login_code: int) =
     if Model.clerkLogin login_code context then ajaxSuccess "success"
     else ajaxFail "fail"