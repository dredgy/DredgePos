module DredgePos.Migrations.CreateDatabaseSchema

open DredgePos.Types
open Database
open Dapper.FSharp
open Dapper.FSharp.PostgreSQL

let CreateDatabase (db: db_config) connectionString =
    let connection = connect connectionString
    connection
        |> NonDbSpecificQuery $"""
            CREATE DATABASE {db.db_name};
        """

let addTables () =
    CreateTable "sessions" [
        "session_id", "varchar(200)"
        "clerk_json", "text"
        "clerk_id", "int"
        "expires", "int"
    ]

    CreateTable "grids" [
        "name", "varchar(60)"
        "rows", "int"
        "cols", "int"
        "data", "text"
    ]

    CreateTable "reservations" [
        "name", "varchar(100)"
        "time", "int"
        "covers", "int"
        "floorplan_table_id", "int"
        "created_at", "int"
    ]

    CreateTable "venues" [
        "name", "varchar(60)"
    ]

    CreateTable "floorplan_tables" [
        "table_number", "int"
        "room_id", "int"
        "venue_id", "int"
        "pos_x", "int"
        "pos_y", "int"
        "shape", "varchar(12)"
        "width", "int"
        "height", "int"
        "default_covers", "int"
        "rotation", "int"
        "merged_children", "text"
        "previous_state", "text"
        "status", "int"
    ]

    CreateTable "print_groups" [
        "name", "varchar(20)"
        "printer_id", "int"
        "venue_id", "int"
    ]

    CreateTable "sales_categories" [
        "parent", "int"
        "name", "varchar(20)"
        "print_group_id", "int"
        "venue_id", "int"
    ]

    CreateTable "rooms" [
        "name", "varchar(20)"
        "background_image", "varchar(100)"
        "venue_id", "int"
    ]

    CreateTable "floorplan_decorations" [
        "room_id", "int"
        "pos_x", "int"
        "pos_y", "int"
        "rotation", "int"
        "width", "int"
        "height", "int"
        "image", "varchar(100)"
        "venue_id", "int"
    ]

    CreateTable "clerks" [
        "name", "varchar(20)"
        "login_code", "int"
        "user_group_id", "int"
    ]

    CreateTable "order_screen_page_groups" [
        "order", "int"
        "venue_id", "int"
        "label", "varchar(40)"
        "grid_id", "int"
    ]

    CreateTable "buttons" [
        "text", "varchar(60)"
        "primary_action", "varchar(15)"
        "primary_action_value", "varchar(20)"
        "secondary_action", "varchar(15)"
        "secondary_action_value", "varchar(20)"
        "image", "varchar(60)"
        "extra_classes", "text"
        "extra_styles", "text"
    ]

    CreateTable "items" [
        "code", "varchar(40)"
        "sales_category_id", "int"
        "name", "varchar(60)"
        "item_type", "varchar(12)"
        "price1", "int"
    ]

    CreateTable "migrations" [
        "name", "varchar(100)"
        "timestamp", "int"
    ]

let run () =
    let db = getDatabaseSettings ()

    $"Server={db.host};Port={db.port};User Id={db.username};Password={db.password};Include Error Detail=true"
    |> CreateDatabase db
    |> ignore
    |> addTables