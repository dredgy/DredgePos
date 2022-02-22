module DredgePos.Authenticate.Router

open Saturn
open Giraffe

let homepage = (warbler (fun _ -> htmlView Controller.loadAuthenticatePage  ))
let handlePostRoute<'a> handlerFunction post next ctx = json (handlerFunction ctx post) next ctx

let pipeline = pipeline{
    use_warbler
}

let router = router {
    pipe_through pipeline
    get "/" homepage
    get "" homepage
    post "/authenticateClerk" (bindJson<int> (handlePostRoute Controller.loginWithLoginCode) )
}