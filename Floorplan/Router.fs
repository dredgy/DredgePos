module DredgePos.Floorplan.Router

open DredgePos
open DredgePos.Global.Router
open DredgePos.Types
open Saturn
open Giraffe

let floorplan = (htmlViewWithContext Controller.loadFloorplanView)

let router = router {
    pipe_through Ajax.Router.pipeline
    get "" floorplan
    get "/" floorplan
    post "/mergeTables" (bindJson<floorplan_table[]> Controller.mergeTables)
    post "/transformTable" (bindJson<floorplan_table> Controller.transformTable)
    post "/createTable" (bindJson<floorplan_table> Controller.createTable)
    post "/addDecoration" (bindJson<floorplan_decoration> Controller.AddDecoration)
    post "/updateDecoration" (bindJson<floorplan_decoration> Controller.UpdateDecoration)
    post "/deleteDecoration" (bindJson<floorplan_decoration> Controller.DeleteDecoration)
    post "/deleteTable" (bindJson<floorplan_table> Controller.deleteTable)
    getf "/getFloorplanData/%i" Controller.getFloorplanData
    getf "/transferTable/%i/%i" Controller.transferTable
    getf "/unmergeTable/%i" Controller.unmergeTable
}