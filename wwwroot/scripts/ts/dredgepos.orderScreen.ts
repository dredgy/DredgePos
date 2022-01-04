type OrderScreenData = {
    order_screen_pages: order_screen_page[]
    sales_categories: sales_category[]
}


type OrderScreen = {
    order_screen_pages: order_screen_page[]
    last_added_item: orderItem
    order_items: orderItem[]
    sales_categories: sales_category[]
    order_item_id_generator: Generator
    selected_item_ids: number[]
}

let OrderScreen : OrderScreen = {
    order_screen_pages: null,
    last_added_item: null,
    order_items: [],
    sales_categories: [],
    order_item_id_generator: newestId(),
    selected_item_ids: []
}

const loadPageGroup = (e: Event) => {
    let button = $(e.target)
    $('.loadPageGroup').removeClass('active')
    button.addClass('active')
    let pageGroupId = button.data('page-group-id')
    $('.pageGroup').hide()
    let activeGrid = $(`.pageGroup[data-page-group-id=${pageGroupId}]`)
    let navButtons = $('.pageNavigation')
    navButtons.css('display', 'flex')

    activeGrid.find('.gridPage').length > 1
        ? navButtons.show()
        : navButtons.hide()

    activeGrid.css('display', 'inline-flex')
}

const setupOrderScreen = (data: OrderScreenData) => {
    OrderScreen.order_screen_pages = data.order_screen_pages
    OrderScreen.sales_categories = data.sales_categories
    let doc = $(document)
    doc.on('click', '.nextButton', goToNextPage)
    doc.on('click', '.prevButton', goToPrevPage)
    doc.on('click', '.loadPageGroup', loadPageGroup)
    doc.on('click', '[data-primary-action=item]', itemButtonClicked)
    doc.on('click', 'tr', itemRowClicked)
    doc.on('click', '.voidButton', voidButtonClicked)
    doc.on('dblclick', '.voidButton', voidLastItem)
    doc.on('click', '.accumulateButton', () => toggleMode('accumulate'))

    turnOnMode('accumulate')

    $('.loadPageGroup').first().trigger('click')
}

/**
 * @param direction 1 for forward, -1 for backwards.
 */
const navigatePage = (direction: number) => {
    let grid = $('.pageGroup:visible')
    grid.get()[0].scrollLeft += grid.width() * direction
}

const goToNextPage = () => navigatePage(1)
const goToPrevPage = () => navigatePage(-1)

const addNewItem = (item: item) => {
    let salesCategory = OrderScreen.sales_categories.where('id', item.item_category)
    const existingOrderItem = isInMode('accumulate') && item.item_type != 'instruction' ? OrderScreen.order_items.where('item', item) : null
    let orderItem : orderItem = existingOrderItem || {
        id: OrderScreen.order_item_id_generator.next().value,
        item: item,
        qty: 0,
        sales_category: salesCategory,
    }

    saveOrderItem(orderItem)
    renderOrderBox()
}

const renderOrderBox = () => {
    const orderBox = $('.orderBoxTable')
    const tbody = orderBox.children('tbody')
    const newTbody = $('<tbody />')
    OrderScreen.order_items.forEach(orderItem => {
        const newRow = createOrderRow(orderItem)
        newTbody.append(newRow)
        if(orderItem.id == OrderScreen.last_added_item?.id){
            pulseElement(newRow)
        }

        if(OrderScreen.selected_item_ids.includes(orderItem.id)){
            selectRow(newRow)
        }
    })

    tbody.replaceWith(newTbody)
    const element = orderBox.find('tbody tr').last().get()[0]
    element.scrollIntoView()
    OrderScreen.last_added_item = null
}

const createOrderRow = (orderItem: orderItem) => {
    const row = $('.orderBoxTable').EmptyRow()
    const price = money(orderItem.item.price1)
    row.data('order-item-id', orderItem.id)
    row.addClass(`${orderItem.item.item_type}Row`)
    row
        .setColumnValue(lang('qty_header'), orderItem.qty)
        .setColumnValue(lang('item_header'), orderItem.item.item_name)
        .setColumnValue(lang('price_header'), price)
        .setColumnValue(lang('total_price_header'), price)
        .setColumnValue(lang('printgroup_header'), OrderScreen.sales_categories.where('id', orderItem.item.item_category)?.name)
        .data('order-item-id', orderItem.id)

    return row
}

const saveOrderItem = (orderItem: orderItem) => {
    const selectedRows = $('.orderBoxTable tbody tr.selected').get()
    const currentQty = orderItem.qty
    orderItem.qty = currentQty + 1
    if( isInMode('accumulate') && orderItem.qty > 1) {
        OrderScreen.order_items = OrderScreen.order_items.map(
            existingOrderItem => {
                if (existingOrderItem == orderItem) return orderItem
                else return existingOrderItem
            })
    } else if(orderItem.item.item_type == 'instruction' && selectedRows.length > 0){
        const selectedOrderItemIds : number[] = selectedRows.map(row => {
            const orderItem = OrderScreen.order_items.where('id', $(row).data('order-item-id'))
            if (orderItem.item && orderItem.item.item_type != 'instruction') {
                return orderItem.id
            } else {
                return null
            }
        }).filter(number => number)

        selectedOrderItemIds.forEach(id => {
            let item = OrderScreen.order_items.where('id', id)
            let index = OrderScreen.order_items.indexOf(item) + 1
            OrderScreen.order_items.splice(index, 0, orderItem)
        })

    } else {
        OrderScreen.order_items.push(orderItem)
    }

    OrderScreen.last_added_item = orderItem

    return orderItem
}


const itemButtonClicked = (e: JQuery.TriggeredEvent) => {
    const existingItemRows = $('.itemRow')
    const button = $(e.target).closest('.posButton')
    const item : item = button.data('item')

    if(item.item_type == 'instruction' && existingItemRows.length < 1) return

    addNewItem(item)

}


const itemRowClicked = (e: JQuery.TriggeredEvent) => {
    const row = $(e.target).closest('tr')

    if(isInMode('void')){
        voidRows(row)
        turnOffMode('void')
        return
    }

    if(!row.hasClass('selected')) selectRow(row)
    else deselectRow(row)

}

const selectRow = (row: JQuery) => {
    row.addClass('selected')
    const id = row.data('order-item-id')
    if(!OrderScreen.selected_item_ids.includes(id))
        OrderScreen.selected_item_ids.push(id)

    const instructionRows = row.nextUntil('.itemRow')

    if(row.hasClass('itemRow') && instructionRows.length){
        instructionRows.each((index, row) => {
            selectRow($(row))
        })
    }
}

const deselectRow = (row: JQuery) => {
    row.removeClass('selected')
    const instructionRows = row.nextUntil('.itemRow')
    OrderScreen.selected_item_ids = OrderScreen.selected_item_ids.filter(id => id != row.data('order-item-id'))

    if(row.hasClass('itemRow') && instructionRows.length){
        deselectRow(instructionRows)
    }
}

const deleteRow = (row: JQuery) => row.find('*:not(.hidden)').slideUp('fast', () => {
    OrderScreen.order_items = OrderScreen.order_items.filter(orderItem => orderItem.id != row.data('order-item-id'))
    row.remove()
})

const voidInstructionRow = (row: JQuery) => {
    const parentRow = row.prevAll('.itemRow').first()
    const parentOrderItem = OrderScreen.order_items.where('id', parentRow.data('order-item-id'))
    if(!parentRow.hasClass('selected') || (parentOrderItem && parentOrderItem?.qty == 0) || !parentOrderItem)
        decrementQty(OrderScreen.order_items.where('id', row.data('order-item-id')))
}

const voidItemRow = (row : JQuery) => {
    const newQty = Number(row.getColumnValue(lang('qty_header'))) - 1
    const orderItem = OrderScreen.order_items.where('id', row.data('order-item-id'))
    const instructionRows = row.nextUntil('.itemRow')

    if(newQty < 1)
        voidRows(instructionRows)

    decrementQty(orderItem)
}

const voidRow = (row: JQuery) => {
    if(row.hasClass('itemRow')) voidItemRow(row)
    else voidInstructionRow(row)
}

const voidRows = (rows: JQuery) => rows.each((index, row) => voidRow($(row)))

const voidButtonClicked = () => {
    const selectedRows = $('.orderBox tr.selected')
    if(isInMode('void')){
        turnOffMode('void')
    } else if(selectedRows.length){
        voidRows(selectedRows)
    } else {
        turnOnMode('void')
    }
}

const voidLastItem = () => {
    if(OrderScreen.order_items.length < 1) return
    let orderItem = OrderScreen.order_items[OrderScreen.order_items.length-1]
    let row = getOrderItemRow(orderItem)
    voidRows(row)
}

const decrementQty = (orderItem: orderItem) => {
    const row = getOrderItemRow(orderItem)
    if(orderItem.qty <= 1){
        OrderScreen.order_items = OrderScreen.order_items.filter(item => item != orderItem)
        deleteRow(row)
    } else {
        orderItem.qty--
        row.setColumnValue(lang('qty_header'), orderItem.qty)
    }
}

const getOrderItemRow = (orderItem: orderItem) => $('tr').filterByData('order-item-id', orderItem.id)

$(() => ajax('/orderScreen/getOrderScreenData/1', null, 'get', setupOrderScreen, null, null) )