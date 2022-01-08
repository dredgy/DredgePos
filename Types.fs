module DredgePos.Types

[<CLIMutable>]
type reservation = {
    id: int
    reservation_name: string
    reservation_time: int
    reservation_covers: int
    reservation_table_id: int
    reservation_created_at: int
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
    printer: int
    venue_id: int
}

[<CLIMutable>]
type sales_category = {
    id: int
    parent: int
    name: string
    print_group: int
    venue_id: int
}

[<CLIMutable>]
type floorplan_room = {
    id: int
    room_name: string
    background_image: string
    venue_id: int
}

[<CLIMutable>]
type floorplan_decoration = {
    id: int
    decoration_room: int
    decoration_pos_x: int
    decoration_pos_y: int
    decoration_rotation: int
    decoration_width: int
    decoration_height: int
    decoration_image: string
    venue_id: int
}

[<CLIMutable>]
type clerk = {id: int; clerk_name: string; clerk_login_code: int; clerk_usergroup: int}

[<CLIMutable>]
type session = {id: int; session_id: string; clerk_json: string; clerk_id: int; expires: int}

[<CLIMutable>]
type order_screen_page_group = {id: int; order: int; venue_id: int; label: string; grid_id: int}

[<CLIMutable>]
type grid = {id: int; grid_name: string; grid_rows: int; grid_cols: int; grid_data: string}

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
    item_code: string
    item_category: int
    item_name: string
    item_type: string
    price1: int
    price2: int
    price3: int
    price4: int
    price5: int
}
