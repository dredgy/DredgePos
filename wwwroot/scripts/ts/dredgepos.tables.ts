interface JQuery {
    getColumnValue(columnHeading: string) : string
    setColumnValue(columnHeading: string, value: any) : JQuery
    getColumnIndex(columnHeading: string) : number
    EmptyRow() : JQuery<HTMLTableRowElement>
    filterByData(prop: string, value: any) : JQuery
}

$.fn.EmptyRow = function(this: JQuery) {
    const headingRow = this.find('th').first().closest('tr')
    const headingCells = headingRow.find('th')
    const newRow = $('<tr/>')
    headingCells.each( (index, cell) => {
        const newCell  = $('<td/>')
        const attributes = Array.from(cell.attributes)
        newCell.data('column', cell.innerText.trim())
        newCell.data('column-index', index)
        attributes.forEach(attribute => newCell.attr(attribute.name, attribute.value))
        newRow.append(newCell)
    })

    return newRow as JQuery<HTMLTableRowElement>
}

$.fn.getColumnIndex = function(this: JQuery, columnHeading: string){
    return this
        .find('td')
        .filterByData('column', columnHeading)
        .data('column-index')
}

$.fn.getColumnValue = function(this: JQuery, columnHeading: string){
    return this.find('td').filterByData('column', columnHeading).text()
}

$.fn.setColumnValue = function(this: JQuery, columnHeading: string, value: any){
    this.find('td').filterByData('column', columnHeading).text(value)
    return this
}

$.fn.filterByData = function(prop: string, val: any) {
    return this.filter(
        function() {
            return $(this).data(prop)==val
        }
    )
}