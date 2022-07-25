type OrderScreenData = {
    order_screen_pages: order_screen_page[]
    sales_categories: sales_category[]
    print_groups: print_group[]
    custom_item: item
}

type OrderScreen = {
    order_screen_pages: order_screen_page[]
    order_items: orderItem[]
    sales_categories: sales_category[]
    print_groups: print_group[]
    order_item_id_generator: Generator
    selected_item_ids: number[]
    order_item_ids_to_pulse: number[]
    qty_override: number
    print_group_override: print_group
    custom_item: item,
    selected_cover: number
    table: floorplan_table,
    last_added_item_ids: number[]
}

let OrderScreen : OrderScreen = {
    order_screen_pages: null,
    order_items: [],
    print_groups: [],
    sales_categories: [],
    order_item_id_generator: newestId(),
    selected_item_ids: [],
    order_item_ids_to_pulse: [],
    qty_override: 1,
    print_group_override: null,
    custom_item: null,
    selected_cover: 0,
    table: null,
    last_added_item_ids: [],
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
    OrderScreen.order_screen_pages = data.order_screen_pages
    OrderScreen.sales_categories = data.sales_categories
    OrderScreen.print_groups = data.print_groups
    OrderScreen.custom_item = data.custom_item

    updateOrderBoxTotals()
    $(document)
        .on('click', '.nextButton', goToNextPage)
        .on('click', '.prevButton', goToPrevPage)
        .on('click', '.loadPageGroup', loadPageGroup)
        .on('click', getElementsByAction('item'), itemButtonClicked)
        .on('click', getElementsByAction('grid'), gridButtonClicked)
        .on('click', '.closeGrid', hideGrids)
        .on('click', '.freetextButton', freetext)
        .on('click', '.openItemButton', customItem)
        .on('click', '.orderBoxTable tbody tr', itemRowClicked)
        .on('click', '.voidButton', voidButtonClicked)
        .on('dblclick', '.voidButton', voidLastItem)
        .on('click', '.numpadButton', overrideQty)
        .on('click', '.accumulateButton', () => toggleMode('accumulate'))
        .on('click', '.changeCoverNumberButton', changeCoverNumberPrompt)
        .on('click', '.showCoverSelectorButton', showCoverSelector)
        .on('click', '.coverSelectorButton', coverSelected)
        .on('change', '[name=print_override]', printGroupOverride)

    turnOnMode('accumulate')

    $('.loadPageGroup').first().trigger('click')

    let observer = new window.MutationObserver((mutations, observer) => updateOrderBoxTotals())

    observer.observe($('.orderBoxTable tbody').get()[0], {
        subtree: true,
        attributes: true,
        childList: true
    });

}

const getElementsByAction = (action: string) => `[data-primary-action=${action}], [data-secondary-action=${action}]`

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

const getOrderBox = () => $('.orderBoxTable tbody')
const goToNextPage = (e: JQuery.TriggeredEvent) => navigatePage(1, $(e.target))
const goToPrevPage = (e: JQuery.TriggeredEvent) => navigatePage(-1, $(e.target))

const setItemQty = (orderItem: orderItem, qty: number) => {
    const newItems = qty > 0
        ? OrderScreen.order_items.map(existingOrderItem => {
            if(existingOrderItem.id == orderItem.id){
                existingOrderItem.qty = qty
            }
            return existingOrderItem
        })
        : array_remove(OrderScreen.order_items, orderItem)

    if(qty < 1){
        OrderScreen.selected_item_ids = array_remove(OrderScreen.selected_item_ids, orderItem.id)
    }

    setOrderItems(newItems)
}

const incrementItemQty = (orderItem:orderItem) => setItemQty(orderItem, orderItem.qty+1)
const decrementItemQty = (orderItem:orderItem) => setItemQty(orderItem, orderItem.qty-1)

const deselectAllRows = () => {
    OrderScreen.selected_item_ids = []
    $('tr.selected').removeClass('selected')
}

const addOrderItemsToPulse = (id: number) => OrderScreen.order_item_ids_to_pulse = array_push(OrderScreen.order_item_ids_to_pulse, id)
const removeOrderItemsToPulse = (id: number) => OrderScreen.order_item_ids_to_pulse = array_remove(OrderScreen.order_item_ids_to_pulse, id)

const addItemToOrderBox = (newOrderItem:orderItem) => {
    const existingItems = OrderScreen.order_items
        .filter(existingOrderItem =>
          existingOrderItem.item.id == newOrderItem.item.id
          && existingOrderItem.print_group.id == newOrderItem.print_group.id
          && existingOrderItem.cover == newOrderItem.cover
        )

    if(existingItems.length > 0 && isInMode('accumulate')) {
        addOrderItemsToPulse(existingItems[0].id)
        incrementItemQty(existingItems[0])
        OrderScreen.last_added_item_ids = [existingItems[0].id]
    } else {
        addOrderItemsToPulse(newOrderItem.id)
        if(!OrderScreen.selected_item_ids.length) {
            const newItems = array_push(OrderScreen.order_items, newOrderItem)
            setOrderItems(newItems)
        } else {
            const newItems =
                OrderScreen.order_items
                    .collect(existingOrderItem => {
                        const firstSelectedItemId = getLastInstructionItem(OrderScreen.selected_item_ids.first()).id
                        return firstSelectedItemId == existingOrderItem.id
                            ? [existingOrderItem, newOrderItem]
                            : [existingOrderItem]
                    })
            setOrderItems(newItems)
        }
        OrderScreen.last_added_item_ids = [newOrderItem.id]
    }
    deselectAllRows()
}

const getParentItem = (orderItemId: number) => {
    const itemIndex = OrderScreen.order_items.findIndex(orderItem => orderItem.id === orderItemId);
    if(OrderScreen.order_items[itemIndex].item.item_type == "item"){
        return OrderScreen.order_items[itemIndex]
    }
    return OrderScreen.order_items
            .filter((orderItem, index) => index < itemIndex && orderItem.item.item_type === 'item')
            .last()
}

const getInstructionItems = (orderItemId: number) => {
    const itemIndex = OrderScreen.order_items.findIndex(orderItem => orderItem.id === orderItemId);
    if(!OrderScreen.order_items[itemIndex+1] || OrderScreen.order_items[itemIndex+1].item.item_type == "item")
        return [OrderScreen.order_items[itemIndex]]
            
    const nextItem =
        OrderScreen.order_items
            .filter((orderItem, index) =>  index > itemIndex && orderItem.item.item_type === 'item')
            ?.first()
    if(!nextItem) {
        return OrderScreen.order_items.filter((orderItem, index) => index > itemIndex)
    }
    const nextItemIndex = OrderScreen.order_items.findIndex(orderItem => orderItem.id === nextItem.id);
    return OrderScreen.order_items.slice(itemIndex, nextItemIndex)

}

const getLastInstructionItem = (orderItemId: number) => getInstructionItems(orderItemId).last()

const addInstructionToOrderBox = (instruction: orderItem) => {
    //If no items are added, then you can't add an instruction row.
    if(!OrderScreen.order_items.length) return
        
    const addAfter = OrderScreen.selected_item_ids.length
        ? OrderScreen.selected_item_ids.map(selectedItemId => getLastInstructionItem(selectedItemId).id).unique()
        : OrderScreen.last_added_item_ids.map(itemId => getLastInstructionItem(itemId).id)

    console.log(addAfter)
    
    const newItems = OrderScreen.order_items.collect(existingItem => {
        const newInstruction = createNewOrderItem(instruction.item, instruction.qty, instruction.print_group)
        addOrderItemsToPulse(newInstruction.id)
        return addAfter.includes(existingItem.id)
            ? [existingItem, newInstruction]
            : [existingItem]
    })
    setOrderItems(newItems)
}

const createNewOrderItem = (item: item, qty: number, printGroup: print_group) : orderItem => {
    return {
        id: OrderScreen.order_item_id_generator.next().value,
        item: clone(item),
        qty: qty,
        print_group: printGroup,
        cover: OrderScreen.selected_cover,
    }
}


const addNewItem = (item: item, qty = 1) => {
    const salesCategory = OrderScreen.sales_categories.where('id', item.sales_category_id)
    const printGroup = OrderScreen.print_group_override ?? OrderScreen.print_groups.where('id', salesCategory.print_group_id)
    const orderItem = createNewOrderItem(item, qty, printGroup)

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

const renderOrderBox = () => {
    const orderBox = getOrderBox()
    const newTbody = $('<tbody />')
    OrderScreen.order_items.forEach(orderItem => {
        const newRow = createOrderRow(orderItem)
        newTbody.append(newRow)

        if(OrderScreen.selected_item_ids.includes(orderItem.id)){
            newRow.addClass('selected')
        }

        if(OrderScreen.order_item_ids_to_pulse.includes(orderItem.id)){
            newRow.pulse()
        }
    })

    orderBox.replaceWith(newTbody)
    const element = newTbody.find('tr').last().get()[0]
    if(element) {
        element.scrollIntoView()
    }
        updateOrderBoxTotals()
}


const setOrderItems = (orderItems: orderItem[]) => {
    OrderScreen.order_items = orderItems
    renderOrderBox()
}

const createOrderRow = (orderItem: orderItem) => {
    const row = $('.orderBoxTable').EmptyRow()
    const price = money(orderItem.item.price1)
    const itemCellText = $('<span/>').text(orderItem.item.name)
    row
        .addClass(`${orderItem.item.item_type}Row`)
        .setColumnValue(lang('qty_header'), orderItem.qty)
        .setColumnValue(lang('price_header'), price)
        .setColumnValue(lang('id_header'), orderItem.item.id)
        .setColumnValue(lang('total_price_header'), price.multiply(orderItem.qty))
        .setColumnValue(lang('printgroup_header'), orderItem.print_group?.name)
        .data('order-item-id', orderItem.id)
        .data('order-item', orderItem)
        .data('print_group', orderItem.print_group)
        .data('cover', orderItem.cover)
        .data('item', orderItem.item)
        .find('td.itemCell')
        .append(itemCellText)

    changeCoverOnRow(row, orderItem.cover)

    if(orderItem.item.item_type == 'instruction' && price.value <= 0){
        row
            .find('.totalPriceCell,.unitPriceCell')
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
    ajax(`/order/getGridHtml/${grid}`, null, null,gridHtmlGenerated, null, null)
}

const hideGrids = () => $('.gridContainer').hide()


const gridHtmlGenerated = (gridData: {gridHtml:string, grid: grid}) => {
    const gridContainer = $('.gridContainer')
    const cellDimensions = getGridCellDimensions()
    const grid = gridData.grid
    const gridHtml = gridData.gridHtml

    gridContainer
        .show()
        .width(cellDimensions.width * grid.cols)
        .children('.gridContainerHeader')
        .children('span')
        .text(grid.name)
        .parent()
        .parent()
        .find('.pageGroup')
        .html(gridHtml)
        .show()
        .parent()
        .height(cellDimensions.height * grid.rows)
        .closest('.gridContainer')
        .find('.pageNavigation')
        .toggle(gridContainer.find('.gridPage').length >  1)
        .height(cellDimensions.height)
}

const itemRowClicked = (e: JQuery.TriggeredEvent) => {
    const row = $(e.target).closest('tr')
    const orderItem: orderItem = row.data('order-item')

    if(isInMode('void')){
        voidOrderItems([orderItem.id])
        turnOffMode('void')
        return
    }

    if(!row.hasClass('selected')) selectRow(row)
    else deselectRow(row)

}

const selectRow = (row: JQuery) => {
    const orderItem: orderItem = row.addClass('selected').data('order-item')
    OrderScreen.selected_item_ids = array_push(OrderScreen.selected_item_ids, orderItem.id)
}

const deselectRow = (row: JQuery) => {
    row.removeClass('selected')
    const instructionRows = row.nextUntil('.itemRow')
    const orderItemToDeselect: orderItem = row.data('order-item')

    OrderScreen.selected_item_ids = OrderScreen.selected_item_ids.filter(orderItemId => orderItemId != orderItemToDeselect.id)

    if(row.hasClass('itemRow') && instructionRows.length){
        deselectRow(instructionRows)
    }
}



const deleteRow = (row: JQuery) => row.find('*:not(.hidden)').slideUp('fast', () => row.remove())

const voidInstructionRow = (row: JQuery) => {
    if(!row.prevAll('.itemRow').first().hasClass('selected'))
        deleteRow(row)
}

const voidOrderItems = (orderItemIds: number[]) => {
    orderItemIds.forEach(orderItemId => {
        decrementItemQty(OrderScreen.order_items.find(item => item.id == orderItemId))
    })
}

const voidButtonClicked = () => {
    if(isInMode('void')){
        turnOffMode('void')
    } else if(OrderScreen.selected_item_ids.length){
        voidOrderItems(OrderScreen.selected_item_ids)
    } else {
        turnOnMode('void')
    }
}

const voidLastItem = () => {
    if (OrderScreen.order_items.length)
        decrementItemQty(OrderScreen.order_items.last())
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
    item.name = text

    addNewItem(item)
}

const customItem = () => showVirtualKeyboard(lang('enter_item_name'), 32,false, customItemTextSubmitted)

const customItemTextSubmitted = (text: string) => {
    const submitFunction = (priceString: string) => {
        const price = currency(priceString, {fromCents: false})

        const item = Object.assign({}, OrderScreen.custom_item)
        item.item_type = 'item'
        item.name = text
        item.price1 = price.intValue

        addNewItem(item)
    }
    showVirtualNumpad(lang('enter_item_price'), 4, false, true, true, submitFunction)
}

const getGridCellDimensions = () => {
    const container = $('#pageGroupContainer')
    return {
        height: container.height()/8,
        width: container.width()/6
    }
}

const showCoverSelector = (event: JQuery.TriggeredEvent) => {
    const button = $(event.target)
    const gridHeight = getGridCellDimensions().height
    const coverSelector = $('.coverSelector')

    const buttonPositionLeftPercent = getPercentageOfPageContainerWidth(button.offset().left)
    const buttonWidthPercent = getPercentageOfPageContainerWidth(button.width())

    coverSelector
        .toggle(!coverSelector.is(':visible'))
        .css({
            width: buttonWidthPercent,
            left: buttonPositionLeftPercent,
            top: (button.offset().top + button.height()) + 'px'
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
    ajax('/order/updateCovers', newTable, 'post', coverNumbersUpdated, null, null)
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
    $('.coverSelector, .gridContainer').hide()
    if(OrderScreen.table)
        ajax(`/order/getOrderScreenData/${OrderScreen.table.table_number}`, null, 'get', setupOrderScreen, null, null)
})