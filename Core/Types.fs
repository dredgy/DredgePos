module DredgePos.Types

[<CLIMutable>]
type reservation = {
    id: int
    name: string
    time: int
    covers: int
    floorplan_table_id: int
    created_at: int
}

[<CLIMutable>]
type venue = {
    id: int
    name: string
}

[<CLIMutable>]
type floorplan_table = {
    table_number: int
    room_id: int
    venue_id: int
    pos_x: int
    pos_y: int
    shape: string
    width: int
    height: int
    default_covers: int
    rotation: int
    merged_children: string
    previous_state: string
    status: int
    id: int
}

[<CLIMutable>]
type print_group = {
    id: int
    name: string
    printer_id: int
    venue_id: int
}

[<CLIMutable>]
type sales_category = {
    id: int
    parent: int
    name: string
    print_group_id: int
    venue_id: int
}

[<CLIMutable>]
type room = {
    id: int
    name: string
    background_image: string
    venue_id: int
}

[<CLIMutable>]
type floorplan_decoration = {
    id: int
    room_id: int
    pos_x: int
    pos_y: int
    rotation: int
    width: int
    height: int
    image: string
    venue_id: int
}

[<CLIMutable>]
type clerk = {
    id: int
    name: string
    login_code: int
    user_group_id: int
}

[<CLIMutable>]
type session = {id: int; session_id: string; clerk_json: string; clerk_id: int; expires: int}

[<CLIMutable>]
type order_screen_page_group = {id: int; order: int; venue_id: int; label: string; grid_id: int}

[<CLIMutable>]
type grid = {id: int; name: string; rows: int; cols: int; data: string}

[<CLIMutable>]
type button = {
    id: int
    text: string
    primary_action: string
    primary_action_value: string
    secondary_action: string
    secondary_action_value: string
    image: string
    extra_classes: string
    extra_styles: string
}

[<CLIMutable>]
type item = {
    id: int
    code: string
    sales_category_id: int
    name: string
    item_type: string
    price1: int
}

[<CLIMutable>]
type db_config = {
    db_name: string
    username: string
    password: string
    host: string
    port: int
}

[<CLIMutable>]
type config = {
    database: db_config
}

[<CLIMutable>]
type migration = {
    id: int
    name: string
    timestamp: int
}