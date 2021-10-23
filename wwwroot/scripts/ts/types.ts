type PosMode = "edit" | "void" | "transfer" | "default"

interface ajaxResult {
    status: string
    data : any
}

interface ApplicationState {
    keyboard: keyboard
    mode: PosMode
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
    status: string
    table_id: number
}

interface room {
    room_id: number
    room_name: string
    background_image: string
    venue_id: number
}


interface keyboard {
    capsLock: boolean
    shift: boolean
    layout: string
}