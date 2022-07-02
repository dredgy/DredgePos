module DredgePos.Migrations.PopulateTestData

open DredgeFramework
open DredgePos.Types
open System.IO

let spaceButton () = (Entity.GetFirstByColumn<button> "primary_action" "spacer").id

let CreatePageFromDirectory index (dir: string) =
    let dirName = DirectoryInfo(dir).Name

    let printGroup =
        match dirName.ToLower() with
            | "beer" | "wine" -> (Entity.GetFirstByColumn<print_group> "name" "Beverage").id
            | _ -> (Entity.GetFirstByColumn<print_group> "name" "Food").id

    let parentName =
        match dirName.ToLower() with
            | "beer" | "wine" -> "Beverage"
            | _ -> "Food"
    let parentCategory = Entity.GetFirstByColumn<sales_category> "name" parentName

    if dirName.ToLower() <> "dips" && dirName.ToLower() <> "Steak Temperatures" then
        let NewGrid = Entity.Create {
            id=0
            name=dirName
            rows=8
            cols=6
            data=""
        }

        Entity.Create {
            id=0
            order=index
            venue_id=1
            label=dirName
            grid_id=NewGrid.id
        } |> ignore
    else ()

    Entity.Create {
        id=0
        parent=parentCategory.id
        name=dirName
        print_group_id=printGroup
        venue_id=1
     } |> ignore

    dir

let CreateDefaultPrintGroups (path: string) =
    Entity.Create {
        id=0
        name="Food"
        printer_id=1
        venue_id=1
    } |> ignore

    Entity.Create {
        id=0
        name="Beverage"
        printer_id=1
        venue_id=1
    } |> ignore


    path

let CreateDefaultVenue (path: string) =
    let venue: venue = {
        id=0
        name="Megalomania"
    }
    Entity.Create venue
    |>ignore
    path

let CreateDefaultClerk (path: string) =
    let venue: clerk = {
        id=0
        name="Josh"
        login_code=1408
        user_group_id=1
    }
    Entity.Create venue
    |>ignore
    path

let CreateDefaultSalesCategories (path: string) =
    Entity.Create {
        id=0
        parent=0
        name="Food"
        print_group_id=(Entity.GetFirstByColumn<print_group> "name" "Food").id
        venue_id=1
    } |> ignore

    Entity.Create {
        id=0
        parent=0
        name="Beverage"
        print_group_id=(Entity.GetFirstByColumn<print_group> "name" "Beverage").id
        venue_id=1
    } |> ignore

    path

let CreateDefaultButtons (path: string) =
    Entity.Create {
        id = 0
        text = ""
        primary_action = "spacer"
        secondary_action = ""
        primary_action_value = ""
        secondary_action_value = ""
        image = ""
        extra_classes = "invisible"
        extra_styles = ""
    }
    |> ignore
    path

let CreateDefaultItems (path: string) =
    Entity.Create {
        id = 0
        name = "Custom Item"
        code = "OPEN000"
        sales_category_id = (Entity.GetFirstByColumn<sales_category> "name" "Food").id
        item_type = "item"
        price1 = 0
    }
    |> ignore
    path

let CreateRooms () =
    "wwwroot/images/rooms"
    |> Directory.GetFiles
    |> Array.filter (fun file -> Path.GetExtension file = ".png" || Path.GetExtension file = ".jpg")
    |> Array.iter (fun image ->
        let roomName = Path.GetFileNameWithoutExtension image
        Entity.Create {
            id=0
            name=roomName
            background_image= Path.GetFileName image
            venue_id=1
        } |> ignore
    )

let populateEntreeGrid () =
    let SalesCategory = Entity.GetFirstByColumn<sales_category> "name" "Entrees"
    let DipSalesCategory = Entity.GetFirstByColumn<sales_category> "name" "Dips"
    let Entrees = Entity.GetAllByColumn<item> "sales_category_id" SalesCategory.id
    let Dips = Entity.GetAllByColumn<item> "sales_category_id" DipSalesCategory.id
    let space = spaceButton()
    let GridData =
        [|
            space; space; space; space; space; space;
            space; space; space; space; space; space;
            space; space; space; space; space; space;
            space; space; space; space; space; space;
            space; space; space; space; space; space;
            space; space; space; space; space; space;
            space; space; space; space; space; space;
            space; space; space; space; space; space;
        |]
        |> Array.mapi (fun index current ->
         let isFirstColumn = (index % 6) = 0
         if not isFirstColumn then current else
             let entree = Entrees |> Array.tryItem (index/6)
             match entree with
             | None -> space
             | Some x -> x.id
        )
        |> Array.mapi (fun index current ->
         let isSecondRow = index > 6 && index < 12
         if not isSecondRow then current else
             let entree = Dips |> Array.tryItem (index-7)
             match entree with
             | None -> space
             | Some x -> x.id
        )

    let grid =
        Entity.GetFirstByColumn<order_screen_page_group> "label" "Entrees"
        |> Entity.GetRelated<grid, order_screen_page_group>

    let newGrid = {grid with data=(jsonEncode {|page1=GridData|})}
    Entity.Update newGrid |> ignore

    ()

let populateMainGrid (category: string) () =
    let SalesCategory = Entity.GetFirstByColumn<sales_category> "name" category
    let Mains = Entity.GetAllByColumn<item> "sales_category_id" SalesCategory.id
    let space = spaceButton()
    let getId index =
        match Mains |> Array.tryItem index with
            | None -> space
            | Some x -> x.id
    let GridData =
        [|
            getId 0; space; getId 1; space; getId 2; space;
            space; space; space; space; space; space;
            getId 3; space; getId 4; space; getId 5; space;
            space; space; space; space; space; space;
            space; space; space; space; space; space;
            space; space; space; space; space; space;
            space; space; space; space; space; space;
            space; space; space; space; space; space;
        |]

    let grid =
        Entity.GetFirstByColumn<order_screen_page_group> "label" category
        |> Entity.GetRelated<grid, order_screen_page_group>

    let newGrid = {grid with data=(jsonEncode {|page1=GridData|})}
    Entity.Update newGrid |> ignore

let populateDessertGrid () =
    let space = spaceButton()
    let SalesCategory = Entity.GetFirstByColumn<sales_category> "name" "Dessert"
    let Desserts = Entity.GetAllByColumn<item> "sales_category_id" SalesCategory.id
    let getId index =
        match Desserts |> Array.tryItem index with
            | None -> space
            | Some x -> x.id
    let GridData =
        [|
            getId 0; space; getId 1; space; space ; space;
            space; space; space; space; space; space;
            space; getId 2; space; getId 4; space; space;
            space; space; space; space; space; space;
            space; space; space; space; space; space;
            space; space; space; space; space; space;
            space; space; space; space; space; space;
            space; space; space; space; space; space;
        |]

    let grid =
        Entity.GetFirstByColumn<order_screen_page_group> "label" "Dessert"
        |> Entity.GetRelated<grid, order_screen_page_group>

    let newGrid = {grid with data=(jsonEncode {|page1=GridData|})}
    Entity.Update newGrid |> ignore

let populateBeerGrid () =
    let space = spaceButton()
    let SalesCategory = Entity.GetFirstByColumn<sales_category> "name" "Beer"
    let Beers = Entity.GetAllByColumn<item> "sales_category_id" SalesCategory.id
    let grid =
        Entity.GetFirstByColumn<order_screen_page_group> "label" "Beer"
        |> Entity.GetRelated<grid, order_screen_page_group>

    let mutable buttonMap = Map.empty<string, int[]>
    Beers
    |> Array.chunkBySize 24
    |> Array.map (fun beerPage ->
        let getId index =
            match beerPage |> Array.tryItem index with
                | None -> space
                | Some x -> x.id
        [|
            getId 0; getId 1; getId 2; getId 3; getId 4 ; getId 5;
            space; space; space; space; space; space;
            getId 6; getId 7; getId 8; getId 9; getId 10 ; getId 11;
            space; space; space; space; space; space;
            getId 12; getId 13; getId 14; getId 15; getId 16 ; getId 17;
            space; space; space; space; space; space;
            getId 18; getId 19; getId 20; getId 21; getId 22 ; getId 23;
            space; space; space; space; space; space;
        |]
    )
    |> Array.iteri (fun index buttonIds ->
        buttonMap <- buttonMap |> Map.add $"page{index+1}" buttonIds
    )

    let GridData = buttonMap |> jsonEncode

    let newGrid = {grid with data=GridData}
    Entity.Update newGrid |> ignore

let populateSteakTemperaturesGrid () =
    let space = spaceButton()
    let SalesCategory = Entity.GetFirstByColumn<sales_category> "name" "Steak Temperatures"
    let Temps = Entity.GetAllByColumn<item> "sales_category_id" SalesCategory.id
    let grid =
        Entity.GetFirstByColumn<order_screen_page_group> "label" "Steak Temperatures"
        |> Entity.GetRelated<grid, order_screen_page_group>

    let getId index =
        match Temps |> Array.tryItem index with
            | None -> space
            | Some x -> x.id
    let GridData =
        [|
            getId 0; space; getId 1; space; getId 2; space;
            space; space; space; space; space; space;
            getId 3; space; getId 4; space; getId 5; space;
            space; space; space; space; space; space;
        |]

    let newGrid = {grid with data=(jsonEncode {|page1=GridData|}); rows=4; cols=6}
    Entity.Update newGrid |> ignore

    let steakButtons = Entity.GetAllByColumn<button> "text" "Venison Wellington"
    steakButtons |> Array.iter (fun button ->
        Entity.Update {button with secondary_action="grid"; secondary_action_value=newGrid.id.ToString()} |> ignore
    )

let PopulateGrids () =
    populateEntreeGrid ()
    |> populateMainGrid "Mains"
    |> populateMainGrid "Wine"
    |> populateDessertGrid
    |> populateBeerGrid
    |> populateSteakTemperaturesGrid

let CreateItemFromFileName (index: int) (dirName: string) (file: string) =
    let extension = Path.GetExtension file
    let fileName = Path.GetFileNameWithoutExtension file
    let itemType =
        match dirName.ToLower() with
            | "dips" -> "instruction"
            | "steak temperatures" -> "instruction"
            | _ -> "item"

    let categories = (Entity.GetAllByColumn<sales_category> "name" dirName)
    let categoryID =
        if categories.Length > 0 then categories[0].id
        else 1

    let newItem = Entity.Create {
        id = 0
        code = $"{dirName}0{index+1}" |> StringReplace " " ""
        sales_category_id=categoryID
        name=fileName
        item_type=itemType
        price1=10
    }

    let classes =
        match dirName.ToLower() with
            | "beer" | "dessert" -> "doubleHeight"
            | "mains" | "wine" | "steak temperatures" -> "doubleHeight doubleWidth"
            | "entrees" -> "doubleWidth"
            | _ -> "normal"

    Entity.Create {
        id=0
        text=fileName
        primary_action="item"
        primary_action_value=newItem.code
        secondary_action="None"
        secondary_action_value=""
        image= $"{dirName}/{fileName}{extension}"
        extra_classes=classes
        extra_styles=""
    } |> ignore


let CreateItemsAndButtons (dir: string) =
    let dirName = DirectoryInfo(dir).Name
    dir
    |> Directory.GetFiles
    |> Array.filter (fun file -> Path.GetExtension file = ".png" || Path.GetExtension file = ".jpg")
    |> Array.iteri (fun index -> CreateItemFromFileName index dirName)

let run () =
    "wwwroot/images/items"
        |> CreateDefaultVenue
        |> CreateDefaultClerk
        |> CreateDefaultPrintGroups
        |> CreateDefaultSalesCategories
        |> CreateDefaultItems
        |> CreateDefaultButtons
        |> Directory.GetDirectories
        |> Array.mapi CreatePageFromDirectory
        |> Array.iter CreateItemsAndButtons
        |> CreateRooms
        |> PopulateGrids