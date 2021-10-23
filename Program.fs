namespace WebApplication

open Clerk
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc.RazorPages
open Microsoft.Extensions.Hosting;
open Reservations
open Saturn
open Giraffe
open DredgeFramework

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
        post "/updateTableShape" (bindJson<Floorplan.floorplan_table_shape> AjaxController.updateTableShape)
        post "/transformTable" (bindJson<Floorplan.floorplan_table_transform> AjaxController.transformTable)
        post "/createTable" (bindJson<Floorplan.floorplan_table> AjaxController.createTable)
        post "/addDecoration" (bindJson<Decorations.decoration_creator> AjaxController.AddDecoration)
        post "/updateDecoration" (bindJson<Decorations.floorplan_decoration> AjaxController.UpdateDecoration)
        post "/deleteDecoration" (bindJson<int> AjaxController.DeleteDecoration)
        post "/newEmptyReservation" (bindJson<int> AjaxController.newEmptyReservation)
        post "/getReservation" (bindJson<int> (fun reservation -> json <| GetReservationById reservation) )
        post "/unreserveTable" (bindJson<int> (fun tableNumber -> json <| Floorplan.unReserveTable tableNumber) )
        getf "/getRoomData/%i" AjaxController.getRoomData
        getf "/getTablesAndDecorations/%i" AjaxController.getRoomTablesAndDecorations
        get "/languageVars" (json <| AjaxController.getLanguageVars)
        get "/getOpenTables" (json <| Floorplan.getActiveTables Floorplan.currentVenue)
        getf "/getActiveTables/%i" AjaxController.getActiveTables
        getf "/tableIsOpen/%i" (fun tableNumber -> json <| Floorplan.tableIsOpen tableNumber)
        getf "/transferTables/%i/%i" AjaxController.transferTable
        getf "/mergeTables/%i/%i" AjaxController.mergeTables
        getf "/unmergeTable/%i" AjaxController.unmergeTable
        getf "/tableExists/%i" (fun tableNumber -> json <| Floorplan.tableExists tableNumber)
        getf "/deleteTable/%i" (fun tableNumber -> json <| Floorplan.deleteTable tableNumber)
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
