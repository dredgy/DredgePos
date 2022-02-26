module DredgePos.Installer.Router

open DredgePos
open Saturn
open Giraffe

let router = router {
    pipe_through Ajax.Router.pipeline
    get "/" (warbler (fun _ -> htmlString (Controller.RunAllMigrations ())))
}