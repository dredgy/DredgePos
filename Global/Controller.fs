module DredgePos.Global.Controller

open DredgeFramework

let addDefaultScripts scripts = scripts |> Array.append [|"./external/jquery.js" ; "dredgepos.core.js"; "keyboards.js";|]
let addDefaultStyles styles = styles |> Array.append  [|"dark.theme.css";|]
let addDefaultMetaTags (tags: (string*string)[]) = tags |> Array.append [|"apple-mobile-web-app-capable", "yes"|]