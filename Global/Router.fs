module DredgePos.Global.Router

open Saturn
open Giraffe

let htmlViewWithContext func =
    (fun ctx ->
        func (snd ctx)
            |> htmlView
    )
    |> warbler

let htmlViewWithContextAndId (id: int) func =
    (fun ctx ->
        func (snd ctx) id
            |> htmlView
    )
    |> warbler