let Application = {
    keyboard: null,
    mode: [],
    languageVars: {}
};
/** Parses a language variable. */
let lang = (key, replacements) => {
    let finalValue = Application.languageVars[key] || '';
    if (!replacements)
        return finalValue;
    if (typeof replacements === 'string')
        replacements = [replacements];
    replacements.forEach((replacement, index) => {
        let correctIndex = index + 1;
        finalValue = finalValue.replace(`[${correctIndex}]`, replacement);
    });
    return finalValue;
};
/** Check if a variable is defined */
let defined = (variable) => {
    return typeof variable !== 'undefined';
};
/** Call an Ajax function asynchronously */
let ajax = (endpoint, data, method = 'POST', successFunction, errorFunction, beforeFunction) => {
    data = (data == null) ? data : JSON.stringify(data);
    return $.ajax({
        url: endpoint,
        method: method,
        data: data,
        success: (response) => {
            if (successFunction && response.status == 'success')
                successFunction(JSON.parse(response.data));
            else if (errorFunction && response.status != 'success') {
                errorFunction(JSON.parse(response.data));
            }
        },
        error: (error) => console.log(error.statusCode),
        beforeSend: beforeFunction
    });
};
/*
    For the flow of the app, synchronous is commonly preferred
    though trying to keep its usage as low as possible.
 */
let ajaxSync = (endpoint, data, method = 'POST') => {
    let response = JSON.parse($.ajax({
        url: endpoint,
        method: method,
        data: JSON.stringify(data),
        async: false,
    }).responseText);
    if (response.data) {
        response.data = JSON.parse(response.data);
        return response.data;
    }
    return response;
};
/* Redirect to a specific URL */
let redirect = (url) => location.assign(url);
const resize = () => {
    $('#pageContainer').height(window.innerHeight + "px");
};
let setupCore = (languageVars) => {
    Application.languageVars = languageVars;
    const doc = $(document);
    doc.on('click', '#alertNo, #alertOk', hideAlerts);
    window.addEventListener('resize', resize);
    resize();
    setElementVisibilityByMode();
};
// @ts-ignore
let posAlert = (message, title = 'Message') => {
    let alertBox = $('#alert');
    alertBox.css('display', 'flex');
    alertBox.data('value', '');
    $('#alertHeading').text(title);
    $('#alertMessage').text(message);
    $('#alertOk').css('display', 'flex');
    $('#alertYes').css('display', 'none');
    $('#alertNo').css('display', 'none');
};
let confirmation = (message, data, title = 'Confirm', submitFunction = (data) => { hideAlerts(); }) => {
    let alert = $('#alert');
    $(document).on('click', '#alert #alertYes', () => {
        hideAlerts();
        submitFunction(data);
        $(document).off('click', '#alert #alertYes');
    });
    alert.css('display', 'flex');
    $('#alertHeading').html(title);
    $('#alertMessage').html(message);
    $('#alertOk').css('display', 'none');
    $('#alertYes').css('display', 'flex');
    $('#alertNo').css('display', 'flex');
};
let hideAlerts = () => $('#alert').hide();
let turnOnMode = (mode) => {
    Application.mode.push(mode);
    setElementVisibilityByMode();
};
let turnOffMode = (mode) => {
    Application.mode = Application.mode.filter((value) => value != mode);
    setElementVisibilityByMode();
};
let toggleMode = (mode) => {
    if (!isInMode(mode))
        turnOnMode(mode);
    else
        turnOffMode(mode);
};
let clearModes = () => { Application.mode = []; };
let isInMode = (mode) => Application.mode.includes(mode);
let setElementVisibilityByMode = () => {
    const mode = Application.mode;
    const elements = $('[data-visible-in-mode]');
    elements.each((index, elem) => {
        let element = $(elem);
        let visibleInModes = element.data('visible-in-mode');
        let showElement = visibleInModes.every(visibleMode => {
            return mode.includes(visibleMode);
        });
        if (element.hasClass('useVisibility')) {
            if (showElement) {
                element.css('visibility', 'visible');
            }
            else
                element.css('visibility', 'hidden');
        }
        else
            element.toggle(showElement);
    });
    const invisibleElements = $('[data-invisible-in-mode]');
    invisibleElements.each((index, elem) => {
        let element = $(elem);
        let inVisibleInModes = element.data('invisible-in-mode');
        let hideElement = inVisibleInModes.some(invisibleMode => {
            return mode.includes(invisibleMode);
        });
        element.toggle(!hideElement);
    });
    $('[data-active-in-mode]').each((index, elem) => {
        const button = $(elem);
        const activeInMode = button.data('active-in-mode');
        mode.includes(activeInMode)
            ? button.addClass('active')
            : button.removeClass('active');
    });
};
$(() => ajax('/ajax/languageVars', null, 'GET', setupCore, null, null));
//# sourceMappingURL=dredgepos.core.js.map