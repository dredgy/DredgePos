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
}

[<CLIMutable>]
type clerk = {id: int; clerk_name: string; clerk_login_code: int; clerk_usergroup: int}

[<CLIMutable>]
type session = {id: int; session_id: string; clerk_json: string; clerk_id: int; expires: int}