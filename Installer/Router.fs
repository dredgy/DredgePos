module DredgePos.Installer.Router

open DredgePos
open Saturn
open Giraffe

let installer = (warbler (fun _ -> htmlString (Controller.RunAllMigrations ())))

let router = router {
    pipe_through Ajax.Router.pipeline
    get "/" installer
    get "" installer
}