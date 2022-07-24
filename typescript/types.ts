type PosMode = "edit" | "void" | "transfer" | "default" | "tableSelected" | "decorationSelected" | "activeTableSelected" | "merge" | "reservedTableSelected" | "accumulate"
type PosModes =  PosMode[]

interface order {
    clerk: string
    split: boolean
    items: orderItem[]
}

interface orderItem {
    id: number
    qty: number
    print_group: print_group
    item: item
    cover: number
}

interface print_group {
    id: number,
    name: string,
    printer_id: number,
    venue_id: number,
}

interface ajaxResult {
    status: string
    data : any
}

interface ApplicationState {
    keyboard: keyboard
    mode: PosModes
    languageVars: Record<any, string>
}

interface floorplan_table {
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

interface floorplan_decoration {
    id: number
    room_id: number
    pos_x: number
    pos_y: number
    rotation: number
    width: number
    height: number
    image: string
    venue_id: number
}

interface room {
    id: number
    name: string
    background_image: string
    venue_id: number
}


interface reservation {
    id: number,
    name: string,
    time: number,
    covers: number,
    created_at: number,
    floorplan_table_id: number,
}

interface keyboard {
    capsLock: boolean
    shift: boolean
    layouts: VirtualKeyboard
    currentLayout: string
}

interface order_screen_page{id: number; order_screen_page_group_id: number; grid_id: number}
interface grid {id: number; name: string; rows: number; cols: number; data: string}

interface item {
    id: number
    code: string
    sales_category_id: number
    name: string
    item_type: string
    price1: number
}

type sales_category = {
    id: number
    parent: number
    name: string
    print_group_id: string
    venue_id: number
}

interface Array<T> {
    where(property: string, value: any): T
    first(): T
    last(): T
    unique(): this
    collect(func: (item: T) => T[]): T[]
}