interface OrderScreen{
    order_screen_pages: order_screen_page[]
}

let OrderScreen : OrderScreen = {
    order_screen_pages: null
}

const loadPageGroup = (e: Event) => {
    let button = $(e.target)
    $('.loadPageGroup').removeClass('active')
    button.addClass('active')
    let pageGroupId = button.data('page-group-id')
    $('.pageGroup').hide()
    let activeGrid = $(`.pageGroup[data-page-group-id=${pageGroupId}]`)

    let navButtons = $('.pageNavigation')

    activeGrid.find('.gridPage').length > 1
        ? navButtons.show()
        : navButtons.hide()

    activeGrid.css('display', 'inline-flex')
}

const setupOrderScreen = (data: OrderScreen) => {
    OrderScreen = data
    let doc = $(document)
    doc.on('click', '.nextButton', goToNextPage)
    doc.on('click', '.prevButton', goToPrevPage)
    doc.on('click', '.loadPageGroup', loadPageGroup)

    let initialPage = $('.loadPageGroup').first().trigger('click')
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


$(() => ajax('/orderScreen/getOrderScreenData/1', null, 'get', setupOrderScreen, null, null) )