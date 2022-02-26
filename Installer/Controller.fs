module DredgePos.Installer.Controller

open System.Reflection
open DredgePos.Types
open FSharp.Reflection

let RunMigration (fsModule: System.Type) =
    fsModule
        .GetMethod("run")
        .Invoke(null, [||])
        |> ignore
    Entity.Create {name=fsModule.FullName; timestamp=DredgeFramework.CurrentTime(); id=0} |> ignore
    fsModule.FullName + " ran Successfully"

let RunAllMigrations () =
    let completedMigrations =
        try
            Entity.GetAll<migration>
        with
            | _ -> [||]
        |> Array.map (fun migration -> migration.name)

    Assembly
        .GetExecutingAssembly()
        .GetTypes()
        |> Array.filter FSharpType.IsModule
        |> Array.filter (fun fsModule -> fsModule.Namespace = "DredgePos.Migrations")
        |> Array.filter (fun fsModule -> not (completedMigrations |> Array.contains fsModule.FullName))
        |> Array.sortBy (fun fsModule -> fsModule.Name)
        |> Array.map RunMigration
        |> (fun arr -> if arr.Length > 0 then arr else [|"No Migrations Were Run"|])
        |> String.concat "<br/><hr/>"