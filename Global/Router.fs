module DredgePos.Global.Router

open Saturn
open Giraffe

let htmlViewWithContext func =
    (fun ctx ->
        func (snd ctx)
            |> htmlView
    )
    |> warbler