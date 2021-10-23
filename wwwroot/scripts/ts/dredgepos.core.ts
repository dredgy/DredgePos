     let Application : ApplicationState = {
        keyboard : null,
        mode: "default",
        languageVars: {}
    }


    /** Parses a language variable. */
     let lang = (key: string, replacements?: string[] | string) => {
        let finalValue = Application.languageVars[key]

        if(!replacements) return finalValue
        if(typeof replacements === 'string') replacements = [replacements]

        replacements.forEach( (replacement, index) => {
            let correctIndex = index+1
            finalValue = finalValue.replace(`[${correctIndex}]`, replacement)
        })

        return finalValue
    }

    /** Check if a variable is defined */
     let defined = (variable: any) => {
        return typeof variable !== 'undefined'
    }

    /** Call an Ajax function asynchronously */
     let ajax = (endpoint : string, data: any, method = 'POST', successFunction : Function , errorFunction : JQuery.Ajax.ErrorCallback<any>, beforeFunction: any) => {
        data = (data == null) ? data :  JSON.stringify(data)
        return $.ajax({
            url: endpoint,
            method: method,
            data: data,
            success: (response) => {
                if(successFunction)
                    successFunction(JSON.parse(response.data))
            },
            error: errorFunction,
            beforeSend: beforeFunction
        })
    }


    /*
        For the flow of the app, synchronous is commonly preferred
        though trying to keep it's usage as low as possible.
     */
     let ajaxSync = (endpoint : string, data?: any, method = 'POST') => {
        let response =  JSON.parse(
            $.ajax({
                url: endpoint,
                method: method,
                data: JSON.stringify(data),
                async:false,
            }).responseText)

        if(response.data) {
            response.data = JSON.parse(response.data)
            return response.data
        }

        return response
    }

    /* Redirect to a specific URL */
     let redirect = (url: string) : void => {
        window.location.href = url
    }


     let setLanguageVariables = () => {
        Application.languageVars = ajaxSync('/ajax/languageVars', null, 'GET')
    }

     // @ts-ignore
     let alert = (message: string, title='Message') => {
        let alertBox = $('#alert')
        alertBox.css('display', 'flex');
        alertBox.data('value', '');
        $('#alertHeading').text(title);
        $('#alertMessage').text(message);

        $('#alertOk').css('display', 'flex');
        $('#alertYes').css('display', 'none');
        $('#alertNo').css('display', 'none');
    }

     // @ts-ignore
     let confirm = (message: string, data: any, title='Confirm', submitFunction = (data: any) => {hideAlerts()}) => {
        let alert = $('#alert')

        $(document).on('click', '#alert #alertYes', () => {
            submitFunction(data)
            hideAlerts()
            $(document).off('click', '#alert #alertYes')
        })

        alert.css('display', 'flex')
        $('#alertHeading').html(title)
        $('#alertMessage').html(message)

        $('#alertOk').css('display', 'none')
        $('#alertYes').css('display', 'flex')
        $('#alertNo').css('display', 'flex')
    }


     let hideAlerts = () => {
        $('#alert').hide()
    }

$( () => {
    let doc = $(document)
    setLanguageVariables()

    doc.on('click', '#alertNo, #alertOk', () => $('#alert').hide())
})