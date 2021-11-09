namespace WebApplication


open DredgePos
open Reservations
open Saturn
open Giraffe
open Types

module Program =

    let handlePostRoute<'a> handlerFunction post next ctx   =
        json (handlerFunction ctx post) next ctx

    let browser = pipeline {
        use_warbler
    }

    let ajaxRouter = router {
        pipe_through browser
        post "/authenticateClerk" (bindJson<int> (handlePostRoute AjaxController.loginWithLoginCode) )
        post "/getTableData" (bindJson<int> AjaxController.getTableData)
        post "/transformTable" (bindJson<floorplan_table> AjaxController.transformTable)
        post "/createTable" (bindJson<floorplan_table> AjaxController.createTable)
        post "/addDecoration" (bindJson<floorplan_decoration> AjaxController.AddDecoration)
        post "/updateDecoration" (bindJson<floorplan_decoration> AjaxController.UpdateDecoration)
        post "/deleteDecoration" (bindJson<floorplan_decoration> AjaxController.DeleteDecoration)
        post "/deleteTable" (bindJson<floorplan_table> AjaxController.deleteTable)
        post "/mergeTables" (bindJson<floorplan_table[]> AjaxController.mergeTables)
        post "/newEmptyReservation" (bindJson<reservation> AjaxController.newEmptyReservation)
        post "/updateReservation" (bindJson<reservation> AjaxController.updateReservation)
        post "/getReservation" (bindJson<int> (fun reservation -> json <| GetReservationById reservation) )
        post "/unreserveTable" (bindJson<floorplan_table> AjaxController.unreserveTable )
        getf "/getRoomData/%i" AjaxController.getRoomData
        getf "/getKeyboardLayout/%s" AjaxController.getKeyboardLayout
        getf "/getTablesAndDecorations/%i" AjaxController.getRoomTablesAndDecorations
        get "/languageVars" (json <| AjaxController.getLanguageVars)
        get "/getOpenTables" (json <| Floorplan.getActiveTables Floorplan.currentVenue)
        getf "/getActiveTables/%i" AjaxController.getActiveTables
        getf "/getFloorplanData/%i" AjaxController.getFloorplanData
        getf "/tableIsOpen/%i" (fun tableNumber -> json <| Floorplan.tableNumberIsOpen tableNumber)
        getf "/transferTable/%i/%i" AjaxController.transferTable
        getf "/unmergeTable/%i" AjaxController.unmergeTable
        getf "/tableExists/%i" (fun tableNumber -> json <| Floorplan.tableExists tableNumber)
    }

    let pageRouter = router {
        pipe_through browser
        not_found_handler (setStatusCode 404 >=> text "404")
        get "/" (redirectTo false "/login")
        get "/login" (warbler (fun _ -> PageController.loadHomePage() ))
        get "/floorplan" (warbler (fun ctx -> PageController.loadFloorplan (snd ctx)))
        forward "/ajax" ajaxRouter
    }

    let app = application {
        use_static "wwwroot"
        use_router pageRouter

    }

    run app
