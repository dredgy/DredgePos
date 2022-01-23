type OrderScreenData = {
    order_screen_pages: order_screen_page[]
    sales_categories: sales_category[]
    print_groups: print_group[]
    custom_item: item
}

type OrderScreen = {
    order_screen_pages: order_screen_page[]
    last_added_item: orderItem
    order_items: orderItem[]
    sales_categories: sales_category[]
    print_groups: print_group[]
    order_item_id_generator: Generator
    selected_item_ids: number[]
    qty_override: number
    print_group_override: print_group
    custom_item: item,
    selected_cover: number
    table: floorplan_table,
}

let OrderScreen : OrderScreen = {
    order_screen_pages: null,
    last_added_item: null,
    order_items: [],
    print_groups: [],
    sales_categories: [],
    order_item_id_generator: newestId(),
    selected_item_ids: [],
    qty_override: 1,
    print_group_override: null,
    custom_item: null,
    selected_cover: 0,
    table: null,
}

const loadPageGroup = (e: Event) => {
    const button = $(e.target)
    const container = $('#pageGroupContainer')

    $('.loadPageGroup').removeClass('active')
    button.addClass('active')

    let pageGroupId = button.data('page-group-id')

    container.find('.pageGroup').hide()

    let activeGrid = $(`.pageGroup[data-page-group-id=${pageGroupId}]`)
    let navButtons = container.next('.pageNavigation')
    navButtons.css('display', 'flex')

    activeGrid.find('.gridPage').length > 1
        ? navButtons.show()
        : navButtons.hide()

    activeGrid.css('display', 'inline-flex')
}

const setupOrderScreen = (data: OrderScreenData) => {

    $('.coverSelector, .gridContainer').hide()

    OrderScreen.order_screen_pages = data.order_screen_pages
    OrderScreen.sales_categories = data.sales_categories
    OrderScreen.print_groups = data.print_groups
    OrderScreen.custom_item = data.custom_item

    updateOrderBoxTotals()
    let doc = $(document)
    doc.on('click', '.nextButton', goToNextPage)
    doc.on('click', '.prevButton', goToPrevPage)
    doc.on('click', '.loadPageGroup', loadPageGroup)
    doc.on('click', '[data-primary-action=item]', itemButtonClicked)
    doc.on('click', '[data-primary-action=grid],[data-secondary-action=grid]', gridButtonClicked)
    doc.on('click', '.closeGrid', hideGrids)
    doc.on('click', '.freetextButton', freetext)
    doc.on('click', '.openItemButton', customItem)
    doc.on('click', '.orderBoxTable tbody tr', itemRowClicked)
    doc.on('click', '.voidButton', voidButtonClicked)
    doc.on('dblclick', '.voidButton', voidLastItem)
    doc.on('click', '.numpadButton', overrideQty)
    doc.on('click', '.accumulateButton', () => toggleMode('accumulate'))
    doc.on('click', '.changeCoverNumberButton', changeCoverNumberPrompt)
    doc.on('click', '.showCoverSelectorButton', showCoverSelector)
    doc.on('click', '.coverSelectorButton', coverSelected)
    doc.on('change', '[name=print_override]', printGroupOverride)

    turnOnMode('accumulate')

    $('.loadPageGroup').first().trigger('click')

    let observer = new window.MutationObserver((mutations, observer) => updateOrderBoxTotals())

    observer.observe($('.orderBoxTable tbody').get()[0], {
        subtree: true,
        attributes: true,
        childList: true
    });

}

/**
 * @param direction 1 for forward, -1 for backwards.
 * @param button
 */
const navigatePage = (direction: number, button: JQuery) => {
    const grid =
        button
            .parent()
            .parent()
            .find('.pageGroup:visible')
    grid.get()[0].scrollLeft += grid.width() * direction
}

const goToNextPage = (e: JQuery.TriggeredEvent) => navigatePage(1, $(e.target))
const goToPrevPage = (e: JQuery.TriggeredEvent) => navigatePage(-1, $(e.target))

const addItemToOrderBox = (orderItem:orderItem) => {
    const orderBox = $('.orderBoxTable tbody')
    let selectedRows = orderBox.find('tr.selected')
    let lastRow : JQuery = selectedRows.length ? getLastInstructionRow(selectedRows.first()) : orderBox.find('tr').last()
    const existingRow = orderBox
        .find('tr')
        .filterByData('item', orderItem.item)
        .filterByData('print_group', orderItem.print_group)
        .filterByData('cover', orderItem.cover)
        .last()


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
                getLastInstructionRow(selectedRow).after(newRow)
                newRow
                    .setColumnValue( lang('printgroup_header'), selectedRow.getColumnValue(lang('printgroup_header')) )


                if(parentRow.hasClass('selected')){
                    selectRow(newRow)
                } else {
                    newRow.pulse()
                }

                scrollToElement(newRow)
            }
        })
    }

    const lastRow = orderBox.find('tr').last()
    newRow
        .setColumnValue(lang('printgroup_header'), lastRow.getColumnValue(lang('printgroup_header')))
        .appendTo(orderBox)
        .pulse()
    scrollToElement(newRow)
}


const addNewItem = (item: item, qty = 1) => {
    const salesCategory = OrderScreen.sales_categories.where('id', item.item_category)
    const printGroup = OrderScreen.print_group_override ?? OrderScreen.print_groups.where('id', salesCategory.print_group)
    const orderItem : orderItem = {
        id: OrderScreen.order_item_id_generator.next().value,
        item: item,
        qty: qty,
        print_group: printGroup,
        cover: OrderScreen.selected_cover,
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
    const itemCellText = $('<span/>').text(orderItem.item.item_name)
    row
        .addClass(`${orderItem.item.item_type}Row`)
        .setColumnValue(lang('qty_header'), orderItem.qty)
        .setColumnValue(lang('price_header'), price)
        .setColumnValue(lang('id_header'), orderItem.item.id)
        .setColumnValue(lang('total_price_header'), price.multiply(orderItem.qty))
        .setColumnValue(lang('printgroup_header'), orderItem.print_group?.name)
        .data('order-item-id', orderItem.id)
        .data('order-item-id', orderItem.id)
        .data('print_group', orderItem.print_group)
        .data('cover', orderItem.cover)
        .data('item', orderItem.item)
        .find('td.itemCell')
        .append(itemCellText)

    changeCoverOnRow(row, orderItem.cover)

    if(orderItem.item.item_type == 'instruction' && price.value <= 0){
        row
            .find('.totalPriceCell')
            .css('font-size', 0)
    }

    return row
}

const itemButtonClicked = (e: JQuery.TriggeredEvent) => {
    hideGrids()
    const existingItemRows = $('.itemRow')
    const button = $(e.target).closest('.posButton')
    const item : item = button.data('item')

    if(item.item_type == 'instruction' && existingItemRows.length < 1) return

    const qty = OrderScreen.qty_override || 1
    OrderScreen.qty_override = 1

    addNewItem(item, qty)

}

const gridButtonClicked = (e: JQuery.TriggeredEvent) => {
    const button = $(e.target).closest('.posButton')
    const grid : number = button.data('grid')
    ajax(`/orderScreen/getGridHtml/${grid}`, null, null,gridHtmlGenerated, null, null)
}


const hideGrids = () => $('.gridContainer').hide()


const gridHtmlGenerated = (gridData: {gridHtml:string, grid: grid}) => {
    const gridContainer = $('.gridContainer')
    const gridCellWidth = getGridCellWidth()
    const gridCellHeight = getGridCellHeight()
    const grid = gridData.grid
    const gridHtml = gridData.gridHtml

    gridContainer
        .show()
        .width(gridCellWidth * grid.grid_cols)
        .children('.gridContainerHeader')
        .children('span')
        .text(grid.grid_name)
        .parent()
        .parent()
        .find('.pageGroup')
        .html(gridHtml)
        .show()
        .parent()
        .height(gridCellHeight * grid.grid_rows)
        .closest('.gridContainer')
        .find('.pageNavigation')
        .toggle(gridContainer.find('.gridPage').length >  1)
        .height(gridCellHeight)
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

const updateOrderBoxTotals = () => {
    const allRows = $('.orderBoxTable tbody tr')
    const selectedRows = $('.orderBoxTable tbody tr.selected')
    const completeTotal = lang('totalPrice', getTotalOfRows(allRows))
    const selectedTotal = lang('selectedPrice', getTotalOfRows(selectedRows))

    $('.orderBoxTotal').text(completeTotal)
    $('.orderBoxSelectedTotal').text(selectedTotal)
}

const getTotalOfRows = (rows: JQuery) => {
    return money(rows
        .find('td.totalPriceCell')
        .get()
        .map(cell => Number(cell.innerText))
        .filter(number => !isNaN(number))
        .reduce( (total, number) => total + number , 0), false)
        .format()
}

const getQty = (row: JQuery) => Number(row.getColumnValue(lang('qty_header')))
const getUnitPrice = (row: JQuery) => moneyFromString(row.getColumnValue(lang('price_header')))
const calculateRowTotal = (row: JQuery) => {
    let price = getUnitPrice(row)
    let qty = getQty(row)
    row.setColumnValue(lang('total_price_header'), price.multiply(qty))
}

const decrementQty = (row: JQuery, qty=1) => {
    const existingQty = getQty(row)

    if(existingQty <= 1){
        const childRows = row.nextUntil('.itemRow')
        deleteRow(row)
        deleteRow(childRows)
        return
    }
    row.setColumnValue(lang('qty_header'), existingQty - qty)
    calculateRowTotal(row)
}

const scrollToElement = (JQueryElement: JQuery) => {
    const element = JQueryElement.get()[0]
    const container = JQueryElement.closest('.orderBox').get()[0]
    const containerTop = $(container).scrollTop()
    const containerBottom = containerTop + $(container).height();
    const elemTop = element.offsetTop
    const elemBottom = elemTop + $(element).height();
    if (elemTop < containerTop) {
        $(container).scrollTop(elemTop);
    } else if (elemBottom > containerBottom) {
        $(container).scrollTop(elemBottom - $(container).height());
    }
}

const overrideQty = () => showVirtualNumpad(lang('multiplier'), 4, false, true, true, qtyOverridden)

const qtyOverridden = (qtyString: string) => OrderScreen.qty_override = Number(qtyString)

const printGroupOverride = (e: JQuery.TriggeredEvent) => {
    const input = $(e.target)
    const printGroupId =  Number(input.val())
    const orderBox = $('.orderBoxTable tbody')
    const selectedRows = orderBox.find('tr.selected')
    const newPrintGroup = OrderScreen.print_groups.where('id', printGroupId)

    if(selectedRows.length && newPrintGroup){
        selectedRows.each((index, row) => {
            $(row).setColumnValue(lang('printgroup_header'), newPrintGroup.name)
            $(row).data('print_group', newPrintGroup)
        })

        OrderScreen.print_group_override = null
        resetToggle(input)

    } else {
        OrderScreen.print_group_override = newPrintGroup
    }
}

const freetext = () => showVirtualKeyboard('', 32,false, freetextSubmitted)

const freetextSubmitted = (text: string) => {
    if(text.trim().length < 1) return

    if($('.orderBoxTable tbody tr').length < 1){
        posAlert(lang('freetext_no_order'))
    }

    const item = Object.assign({}, OrderScreen.custom_item)
    item.item_type = 'instruction'
    item.item_name = text

    addNewItem(item)

}

const customItem = () => showVirtualKeyboard(lang('enter_item_name'), 32,false, customItemTextSubmitted)

const customItemTextSubmitted = (text: string) => {
    const submitFunction = (priceString: string) => {
        const price = currency(priceString, {fromCents: false})

        const item = Object.assign({}, OrderScreen.custom_item)
        item.item_type = 'item'
        item.item_name = text
        item.price1 = price.intValue

        addNewItem(item)
    }
    showVirtualNumpad(lang('enter_item_price'), 4, false, true, true, submitFunction)
}

const getGridCellHeight = () => $('#pageGroupContainer').height()/8
const getGridCellWidth = () => $('#pageGroupContainer').width()/6


const showCoverSelector = (event: JQuery.TriggeredEvent) => {
    const button = $(event.target)
    const gridHeight = getGridCellHeight()

    const coverSelector = $('.coverSelector')
    coverSelector
        .toggle(!coverSelector.is(':visible'))
        .width(button.width())
        .css({
            left: button.offset().left + 'px',
            top: button.offset().top + button.height() + 'px',
        })
        .find('.coverSelectorButton')
        .height(gridHeight)

}

const coverSelected = (event: JQuery.TriggeredEvent) => {
    $('.coverSelector').hide()
    const button = $(event.target)
    const cover = Number(button.data('cover'))
    const selectedRows = $('.orderBoxTable tbody').find('tr.itemRow.selected')

    selectedRows.each( (_, selectedRow) => changeCoverOnRow($(selectedRow), cover))
    OrderScreen.selected_cover = cover
}

const changeCoverOnRow = (row: JQuery, cover: number) => {
    row.data('cover', cover)
    const itemCell = row.find('.itemCell')
    const existingCoverSpan = itemCell.find('small')
    const coverSpan = existingCoverSpan.length > 0
                        ? existingCoverSpan
                        : $('<small/>').appendTo(itemCell)

    coverSpan.text(lang('selected_cover', cover.toString()))
    if(cover < 1 || !row.hasClass('itemRow')) {
        coverSpan.remove()
    }
}

const changeCoverNumberPrompt = () =>
    showVirtualNumpad(lang('how_many_covers'), 3, false, false, true, changeCoverNumberPromptSubmitted)


const changeCoverNumberPromptSubmitted = (value: string) => updateCoverNumbers(Number(value))

const updateCoverNumbers = (covers: number) => {
    let newTable = Object.assign({}, OrderScreen.table)
    newTable.default_covers = covers
    ajax('/orderScreen/updateCovers', newTable, 'post', coverNumbersUpdated, null, null)
}

const coverNumbersUpdated = (newTable: floorplan_table) => {
    const covers = newTable.default_covers
    OrderScreen.table = newTable
    $('.changeCoverNumberButton').text(lang('covers', covers.toString()))
    generateCoverSelector()
}

const generateCoverSelector = () => {
    const covers = OrderScreen.table.default_covers
    const coverSelector = $('.coverSelector')
    coverSelector.hide().children().remove()

    for(let cover=0; cover<=covers; cover++) {
        const buttonText = cover==0 ? lang('cover_zero') : lang('selected_cover', cover.toString())
        loadTemplate('#posButtonTemplate')
            .find('a')
            .first()
            .addClass('coverSelectorButton')
            .text(buttonText)
            .data('cover', cover)
            .appendTo(coverSelector)
    }
}

$(() => {
    OrderScreen.table = $('#pageContainer').data('table') || null
    ajax('/orderScreen/getOrderScreenData/1', null, 'get', setupOrderScreen, null, null)
})