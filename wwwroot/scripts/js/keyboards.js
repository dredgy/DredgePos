let showVirtualNumpad = (heading, maxlength = 4, isPassword, allowDecimals = true, allowClose = true, submitFunction) => {
    let numpad = $('#virtualNumpad');
    let inputBox = $('#virtualNumpadInput');
    let closeKeyboardButton = $('.closeKeyboards');
    numpad.css('display', 'flex');
    let showCloseButton = allowClose ? 'flex' : 'none';
    closeKeyboardButton.css('display', showCloseButton);
    $('#virtualNumpadHeading').html(heading);
    /*
        The numpad always submits to a function.
        If a function isn't specified, it will submit
        to the same function that called it
    */
    numpad.data('value', '');
    inputBox.text('');
    numpad.data('maxlength', maxlength);
    numpad.data('submitfunction', submitFunction);
    numpad.data('password', isPassword);
    numpad.data('allowdecimals', allowDecimals);
    $(document).off('keyup');
    $(document).on('keyup', e => {
        let key = e.key;
        switch (key) {
            case 'Backspace':
            case 'Delete':
                key = 'clear';
                break;
            case 'Enter':
                key = 'submit';
                break;
        }
        virtualNumpadInput(key);
    });
};
let hideVirtualKeyboard = () => {
    let keyboard = $('#virtualKeyboard');
    keyboard.hide();
    $('#virtualKeyboardHeading').html('');
    $(document).unbind('keyup');
};
let hideVirtualNumpad = () => {
    let numpad = $('#virtualNumpad');
    numpad.css('display', 'none');
    $('#virtualNumpadHeading').html('');
    $(document).unbind('keyup');
};
let virtualNumpadInput = (input) => {
    let inputBox = $('#virtualNumpadInput');
    let numpad = $('#virtualNumpad');
    let maxlength = numpad.data('maxlength');
    let allowDecimals = numpad.data('allowdecimals');
    let submitFunction = numpad.data('submitfunction');
    let allowedValues = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'submit', 'clear'];
    let currentValue = numpad.data('value').toString();
    if (allowDecimals)
        allowedValues.push('.', ',');
    let validInput = allowedValues.includes(input);
    //If the input is a valid number, decimal point or command.
    if (validInput) {
        switch (input) {
            case 'submit':
                hideVirtualNumpad();
                let numpadValue = numpad.data('value').length > 0 ? numpad.data('value') : "0";
                submitFunction(numpadValue);
                break;
            case 'clear':
                clearNumpadInput();
                break;
            default:
                let newText = currentValue + input;
                let isPassword = numpad.data('password');
                let length = input.length + inputBox.text().length;
                if (length <= maxlength) {
                    inputBox.append(isPassword ? '*' : input);
                    numpad.data('value', newText);
                }
        }
    }
};
let clearNumpadInput = () => {
    $('#virtualNumpadInput').text("");
    $('#virtualNumpad').data('value', '');
};
let setupVirtualNumpad = () => {
    $(document).on('click', '.virtualNumpadButton', e => {
        virtualNumpadInput($(e.target).data('value').toString());
    });
    $('.closeKeyboards').on('click', () => {
        hideVirtualKeyboard();
        hideVirtualNumpad();
    });
};
let setupVirtualKeyboard = (keyboardLayouts) => {
    Application.keyboard = {
        capsLock: false,
        shift: false,
        layouts: keyboardLayouts,
        currentLayout: 'default',
    };
    $(document).on('click', '.virtualKeyboardButton', e => {
        virtualKeyboardInput($(e.target).data('value'));
    });
    setKeyboardLayout('default');
};
let showVirtualKeyboard = (heading, maxlength = 32, isPassword = false, submitFunction = () => {
    hideVirtualKeyboard();
}) => {
    let keyboard = $('#virtualKeyboard');
    let inputBox = $('#virtualKeyboardInput');
    keyboard.css('display', 'flex');
    $('#virtualKeyboardHeading').html(heading);
    keyboard.data('value', '');
    inputBox.text('');
    keyboard.data('maxlength', maxlength);
    keyboard.data('password', isPassword);
    keyboard.data('submitfunction', submitFunction);
    $(document).off('keyup');
    $(document).on('keyup', e => {
        let key = e.key;
        if (key == 'Enter')
            key = 'submit';
        virtualKeyboardInput(key);
    });
};
let virtualKeyboardInput = (input) => {
    let inputBox = $('#virtualKeyboardInput');
    let keyboard = $('#virtualKeyboard');
    let maxlength = keyboard.data('maxlength');
    let isPassword = keyboard.data('password');
    let length = input.length + inputBox.text().length;
    switch (input.toLowerCase()) {
        case 'backspace':
        case 'delete':
            let newText = inputBox.text().slice(0, -1);
            inputBox.text(newText);
            keyboard.data('value', newText);
            break;
        case 'submit':
            hideVirtualKeyboard();
            let submitFunction = keyboard.data('submitfunction');
            submitFunction(inputBox.text());
            break;
        case 'shift':
            if (Application.keyboard.capsLock)
                break;
            Application.keyboard.shift = !Application.keyboard.shift;
            Application.keyboard.capsLock = false;
            setKeyboardLayout('default', Application.keyboard.shift ? 'shift' : '');
            break;
        case 'capslock':
            Application.keyboard.shift = false;
            Application.keyboard.capsLock = !Application.keyboard.capsLock;
            let capsLockButton = $('[data-value="capslock"]');
            capsLockButton.toggleClass('active');
            setKeyboardLayout('default', Application.keyboard.capsLock ? 'shift' : '');
            break;
        case 'space':
            input = ' ';
            break;
    }
    //Stops keys such as F5 being pressed.
    if (input.length == 1) {
        if (Application.keyboard.shift || Application.keyboard.capsLock) {
            input = input.toUpperCase();
        }
        let newText = inputBox.text() + input;
        keyboard.data('value', newText);
        inputBox.text(newText);
        //If shift, reload lowercase
        if (Application.keyboard.shift) {
            Application.keyboard.shift = false;
            setKeyboardLayout('default');
        }
    }
};
let setKeyboardLayout = (layout, modifier = '') => {
    if (modifier != '')
        modifier = `_${modifier}`;
    Application.keyboard.currentLayout = layout;
    let layoutToLoad = Application.keyboard.layouts[layout];
    $('.virtualKeyboardRow').each((index, row) => {
        /*
        We start at 1 instead of 0. Makes it easier for non-programmers
        and translators making their own language packs
        */
        index = index + 1;
        let currentRow = layoutToLoad[`row${index}${modifier}`];
        $(row).children('a').each((keyIndex, button) => {
            let key = $(button);
            let keyValue = currentRow[keyIndex];
            /*
                KeyText is the text that appears
                in the button. KeyData is the value
                submitted when the button is pressed.
            */
            let keyText = keyValue;
            let keyData = keyValue;
            key.addClass('posButton');
            key.addClass('virtualKeyboardButton');
            let pattern = new RegExp(/\[([^)]+)\]/);
            let matches = keyValue.match(pattern);
            if (matches) {
                keyText = keyValue.replace(pattern, '');
                keyData = matches[1];
            }
            key.html(keyText);
            //Use attr() as some keys have CSS dependent on data-value
            key.attr('data-value', keyData);
            key.data('value', keyData);
        });
    });
};
$(() => {
    setupVirtualNumpad();
    ajax('/ajax/getKeyboardLayout/english', null, 'get', setupVirtualKeyboard, null, null);
});
//# sourceMappingURL=keyboards.js.map