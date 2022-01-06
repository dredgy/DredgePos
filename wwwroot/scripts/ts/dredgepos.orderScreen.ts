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
    selected_item_ids: [],
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
    doc.on('click', '.orderBoxTable tbody tr', itemRowClicked)
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

const addItemToOrderBox = (orderItem:orderItem) => {
    const orderBox = $('.orderBoxTable tbody')
    let selectedRows = orderBox.find('tr.selected')
    let lastRow : JQuery = selectedRows.length ? selectedRows.first() : orderBox.find('tr').last()
    const existingRow = orderBox.find(`.itemCell:contains("${orderItem.item.item_name}")`).closest('tr').last()

    //If accumulating, just increase the quantity of the existing row.
    if(existingRow.length > 0 && isInMode('accumulate')){
        incrementRowQty(existingRow, orderItem.qty)
        scrollToElement(existingRow)
        existingRow.pulse()
    } else {
        const newRow = createOrderRow(orderItem)
        lastRow.length > 0
            ? lastRow.after(newRow)
            : orderBox.append(newRow)
        scrollToElement(newRow)
        newRow.pulse()
    }

    deselectRow(orderBox.find('tr'))
}


const addInstructionToOrderBox = (instruction: orderItem) => {
    const orderBox = $('.orderBoxTable tbody')
    let selectedRows = orderBox.find('tr.selected')
    const newRow = createOrderRow(instruction)

    //If no items are added, then you can't add an instruction row.
    if(!orderBox.find('tr.itemRow').length) return

    if(selectedRows.length > 0){
        selectedRows.each( (_, row) => {
            const selectedRow = $(row)
            const parentRow = getParentRow(selectedRow)

            if(parentRow.is(selectedRow) || !parentRow.hasClass('selected')) {
                const newRow = createOrderRow(instruction)
                getLastInstructionRow(selectedRow).after(newRow.pulse())
            }
        })
        return
    }

    orderBox.append(newRow.pulse())
}


const addNewItem = (item: item, qty = 1) => {

    const salesCategory = OrderScreen.sales_categories.where('id', item.item_category)

    const orderItem : orderItem = {
        id: OrderScreen.order_item_id_generator.next().value,
        item: item,
        qty: qty,
        sales_category: salesCategory,
    }

    switch(item.item_type){
        case 'instruction':
            addInstructionToOrderBox(orderItem)
            break
        case 'item':
        default:
            addItemToOrderBox(orderItem)
            break
    }
}

const getLastInstructionRow = (row: JQuery) => {
        let stopCounting = false
        let finalRow = row
        row.nextAll().each(function (index, activeRow){
            if(!stopCounting){
                if($(activeRow).hasClass('instructionRow')){
                    finalRow = $(activeRow)
                } else {
                    stopCounting = true
                }
            }
        })

        return $(finalRow)
}

const getParentRow = (row: JQuery) => {
    return row.hasClass('instructionRow')
        ? row.prevAll('.itemRow').first()
        : row
}

const incrementRowQty = (row: JQuery, qty: number) => {
    const existingQty = Number(row.getColumnValue(lang('qty_header')))
    const newQty = qty + existingQty
    row.setColumnValue(lang('qty_header'), newQty)
    calculateRowTotal(row)
}

const renderOrderBox = () => {
    const orderBox = $('.orderBoxTable')
    const tbody = orderBox.children('tbody')
    const newTbody = $('<tbody />')
    OrderScreen.order_items.forEach(orderItem => {
        const newRow = createOrderRow(orderItem)
        newTbody.append(newRow)
        newRow.pulse()
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

    if(row.hasClass('itemRow') && instructionRows.length){
        deselectRow(instructionRows)
    }
}

const deleteRow = (row: JQuery) => row.find('*:not(.hidden)').slideUp('fast', () => row.remove())

const voidInstructionRow = (row: JQuery) => {
    if(!row.prevAll('.itemRow').first().hasClass('selected'))
        deleteRow(row)
}

const voidItemRow = (row : JQuery) => decrementQty(row)

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
    const orderBox = $('.orderBoxTable tbody')
    const allRows = orderBox.find('tr')
    if(allRows.length < 1) return
    voidRows(allRows.last())
}

const getQty = (row: JQuery) => Number(row.getColumnValue(lang('qty_header')))
const getUnitPrice = (row: JQuery) => moneyFromString(row.getColumnValue(lang('price_header')))
const calculateRowTotal = (row: JQuery) => {
    let price = getUnitPrice(row)
    let qty = getQty(row)
    row.setColumnValue(lang('total_price_header'), price.multiply(qty))
}

const decrementQty = (row: JQuery) => {
    const qty = getQty(row)
    if(qty <= 1){
        const childRows = row.nextUntil('.itemRow')
        deleteRow(row)
        deleteRow(childRows)
        return
    }

    row.setColumnValue(lang('qty_header'), qty - 1)
    calculateRowTotal(row)
}

const scrollToElement = (element: JQuery) => element.get()[0].scrollIntoView()


$(() => ajax('/orderScreen/getOrderScreenData/1', null, 'get', setupOrderScreen, null, null) )