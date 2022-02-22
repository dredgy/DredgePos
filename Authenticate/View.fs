module DredgePos.Authenticate.View

open DredgePos.Global.View
open Giraffe.ViewEngine

let content = div [_id "authenticator"] []

let index scripts styles metaTags = HtmlPage "Floorplan" (GetScripts scripts) (GetStyles styles) (GetMetaTags metaTags) [|content|]


