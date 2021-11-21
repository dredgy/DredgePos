type PosMode = "edit" | "void" | "transfer" | "default" | "tableSelected" | "decorationSelected" | "activeTableSelected" | "merge" | "reservedTableSelected"
type PosModes =  PosMode[]


interface ajaxResult {
    status: string
    data : any
}

interface ApplicationState {
    keyboard: keyboard
    mode: PosModes
    languageVars: Record<any, string>
}

interface table {
    table_number: number,
    room_id: number
    venue_id: number
    pos_x: number
    pos_y: number
    shape: string
    width: number
    height: number
    default_covers: number
    rotation: number
    merged_children: string
    previous_state: string
    status: number
    id: number
}

interface decoration {
    id: number
    decoration_room: number
    decoration_pos_x: number
    decoration_pos_y: number
    decoration_rotation: number
    decoration_width: number
    decoration_height: number
    decoration_image: string
}

interface room {
    id: number
    room_name: string
    background_image: string
    venue_id: number
}


interface reservation {
    id: number,
    reservation_name: string,
    reservation_time: number,
    reservation_covers: number,
    reservation_created_at: number,
    reservation_table_id: number,
}

interface keyboard {
    capsLock: boolean
    shift: boolean
    layouts: VirtualKeyboard
    currentLayout: string
}

interface order_screen_page_group  {id: number; venue_id: number; label: string}
interface order_screen_page{id: number; order_screen_page_group_id: number; grid_id: number}
interface grid {id: number; grid_name: string; grid_rows: number; grid_cols: number; grid_data: string}