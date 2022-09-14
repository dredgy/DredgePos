namespace DredgePos

open DredgePos
open Saturn
open Giraffe

module Program =
    let router = router {
        pipe_through Ajax.Router.pipeline
        not_found_handler (setStatusCode 404 >=> text "404")
        get "/" (redirectTo true "/login")
        forward "/ajax" Ajax.Router.router
        forward "/floorplan" DredgePos.Floorplan.Router.router
        forward "/order" DredgePos.OrderScreen.Router.router
        forward "/login" DredgePos.Authenticate.Router.router
        forward "/reservations" DredgePos.Reservations.Router.router
        forward "/install" DredgePos.Installer.Router.router
    }

    let app =
        let ipAddress = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList[2];
        printf $"DredgePOS is now running at http://{ipAddress}:5001\n"
        application {
            use_mime_types [(".woff", "application/font-woff")]
            use_static "wwwroot"
            use_router router
            url "http://0.0.0.0:5001"
        }

    run app
