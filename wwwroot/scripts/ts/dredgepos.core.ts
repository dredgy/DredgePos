/// <reference path="./typings/currency.d.ts" />

const Application: ApplicationState = {
    keyboard: null,
    mode: [],
    languageVars: {}
}


/** Parses a language variable. */
const lang = (key: string, replacements?: string[] | string) => {
    let finalValue = Application.languageVars[key] || ''

    if (!replacements) return finalValue
    if (typeof replacements === 'string') replacements = [replacements]

    replacements.forEach((replacement, index) => {
        const correctIndex = index + 1
        finalValue = finalValue.replace(`[${correctIndex}]`, replacement)
    })

    return finalValue
}

/** Check if a variable is defined */
const defined = (variable: any) => {
    return typeof variable !== 'undefined'
}

/** Call an Ajax function asynchronously */
const ajax = (endpoint: string, data: any, method = 'POST', successFunction: Function, errorFunction: Function, beforeFunction: any) => {
    data = (data == null) ? data : JSON.stringify(data)
    return $.ajax({
        url: endpoint,
        method: method,
        data: data,
        success: (response: ajaxResult) => {
            if (successFunction && response.status == 'success')
                successFunction(JSON.parse(response.data))
            else if (errorFunction && response.status != 'success') {
                errorFunction(JSON.parse(response.data))
            }
        },
        error: (error) => console.log(error.statusCode),
        beforeSend: beforeFunction
    })
}


/*
    For the flow of the app, synchronous is commonly preferred
    though trying to keep its usage as low as possible.
 */
const ajaxSync = (endpoint: string, data?: any, method = 'POST') => {
    const response = JSON.parse(
        $.ajax({
            url: endpoint,
            method: method,
            data: JSON.stringify(data),
            async: false,
        }).responseText)

    if (response.data) {
        response.data = JSON.parse(response.data)
        return response.data
    }

    return response
}

/* Redirect to a specific URL */
const redirect = (url: string): void => location.assign(url)

const resize = () => {
    $('#pageContainer').height(window.innerHeight + "px");
}

const setupCore = (languageVars: Record<string, string>) => {
    Application.languageVars = languageVars
    const doc = $(document)
    doc.on('click', '#alertNo, #alertOk', hideAlerts)
    window.addEventListener('resize', resize)
    resize()

    setElementVisibilityByMode()
}


const posAlert = (message: string, title = 'Message') => {
    const alertBox = $('#alert')
    alertBox.css('display', 'flex');
    alertBox.data('value', '');
    $('#alertHeading').text(title);
    $('#alertMessage').text(message);

    $('#alertOk').css('display', 'flex');
    $('#alertYes').css('display', 'none');
    $('#alertNo').css('display', 'none');
}

const confirmation = (message: string, data: any, title = 'Confirm', submitFunction = (data: any) => {hideAlerts()}) => {
    const alert = $('#alert')

    $(document).on('click', '#alert #alertYes', () => {
        hideAlerts()
        submitFunction(data)
        $(document).off('click', '#alert #alertYes')
    })

    alert.css('display', 'flex')
    $('#alertHeading').html(title)
    $('#alertMessage').html(message)

    $('#alertOk').css('display', 'none')
    $('#alertYes').css('display', 'flex')
    $('#alertNo').css('display', 'flex')
}


const hideAlerts = () => $('#alert').hide()

const turnOnMode = (mode: PosMode) => {
    Application.mode.push(mode)
    setElementVisibilityByMode()
}

const turnOffMode = (mode: PosMode) => {
    Application.mode = Application.mode.filter((value) => value != mode)
    setElementVisibilityByMode()
}

const toggleMode = (mode: PosMode) => {
    if (!isInMode(mode))
        turnOnMode(mode)
    else
        turnOffMode(mode)
}

const clearModes = () => {
    Application.mode = []
    setElementVisibilityByMode()
}

const isInMode = (mode: PosMode) => Application.mode.includes(mode)

const setElementVisibilityByMode = () => {
    const mode = Application.mode
    const elements = $('[data-visible-in-mode]')

    elements.each((index, elem) => {
        const element = $(elem)
        const visibleInModes: PosModes = element.data('visible-in-mode')

        const showElement = visibleInModes.every(visibleMode => {
            return mode.includes(visibleMode)
        });

        if (element.hasClass('useVisibility')) {
            if (showElement) {
                element.css('visibility', 'visible')
            } else element.css('visibility', 'hidden')
        } else element.toggle(showElement)
    })

    const invisibleElements = $('[data-invisible-in-mode]')
    invisibleElements.each((index, elem) => {
        const element = $(elem)
        const inVisibleInModes: PosModes = element.data('invisible-in-mode')
        const hideElement = inVisibleInModes.some(invisibleMode => {
            return mode.includes(invisibleMode)
        })
        element.toggle(!hideElement)
    })


    $('[data-active-in-mode]').each((index, elem) => {
        const button = $(elem)
        const activeInMode: PosMode = button.data('active-in-mode')

        mode.includes(activeInMode)
            ? button.addClass('active')
            : button.removeClass('active')

    })

}

const pulseElement = (element: JQuery) => element.addClass('pulse').on('animationend', () => element.removeClass('pulse'))

Array.prototype.where = function<x>(this: x[], property: string, value: any) {
    return this.filter( item => (item as any)[property] === value)[0] || null
}

const money = (amount: number) => currency(amount, {fromCents: true})
const moneyFromString = (amount: string) => currency(amount)

//Id generator.
function* newestId(){
    let id = 0
    while(true){
        id++
        yield id
    }
}


$(() => ajax('/ajax/languageVars', null, 'GET', setupCore, null, null))