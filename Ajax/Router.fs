module DredgePos.Ajax.Router

open Saturn
open Giraffe

let pipeline = pipeline {
    use_warbler
}

let router = router {
    getf "/getKeyboardLayout/%s" Controller.getKeyboardLayout
    get "/languageVars" (json Controller.getLanguageVars)
}

