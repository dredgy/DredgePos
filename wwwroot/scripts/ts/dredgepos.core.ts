     let Application : ApplicationState = {
        keyboard : null,
        mode: [],
        languageVars: {}
    }


    /** Parses a language variable. */
     let lang = (key: string, replacements?: string[] | string) => {
        let finalValue = Application.languageVars[key] || ''

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
     let ajax = (endpoint : string, data: any, method = 'POST', successFunction : Function , errorFunction : Function, beforeFunction: any) => {
        data = (data == null) ? data :  JSON.stringify(data)
        return $.ajax({
            url: endpoint,
            method: method,
            data: data,
            success: (response: ajaxResult) => {
                if(successFunction && response.status == 'success')
                    successFunction(JSON.parse(response.data))
                else if (errorFunction && response.status != 'success'){
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


     let setupCore = (languageVars: Record<string, string>) => {
         Application.languageVars = languageVars
         const doc = $(document)
         doc.on('click', '#alertNo, #alertOk', hideAlerts)

         setElementVisibilityByMode()
     }


     // @ts-ignore
     let posAlert = (message: string, title='Message') => {
        let alertBox = $('#alert')
        alertBox.css('display', 'flex');
        alertBox.data('value', '');
        $('#alertHeading').text(title);
        $('#alertMessage').text(message);

        $('#alertOk').css('display', 'flex');
        $('#alertYes').css('display', 'none');
        $('#alertNo').css('display', 'none');
    }

     let confirmation = (message: string, data: any, title='Confirm', submitFunction = (data: any) => {hideAlerts()}) => {
        let alert = $('#alert')

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


     let hideAlerts = () => $('#alert').hide()

     let turnOnMode = (mode : PosMode) => {
         Application.mode.push(mode)
         setElementVisibilityByMode()
     }

     let turnOffMode = (mode : PosMode) => {
         Application.mode = Application.mode.filter((value) => value != mode)
         setElementVisibilityByMode()

     }

     let toggleMode = (mode: PosMode) => {
         if(!isInMode(mode))
             turnOnMode(mode)
         else
             turnOffMode(mode)
     }

     let clearModes = () => {Application.mode = []}
     let isInMode = (mode: PosMode) => Application.mode.includes(mode)

     let setElementVisibilityByMode = () => {
         const mode = Application.mode
         const elements = $('[data-visible-in-mode]')

         elements.each((index, elem) => {
             let element = $(elem)
             let visibleInModes : PosModes = element.data('visible-in-mode')

             let showElement = visibleInModes.every( visibleMode => {
                 return mode.includes(visibleMode)
             });

             if(element.hasClass('useVisibility')){
                 if(showElement) {
                    element.css('visibility', 'visible')
                 } else element.css('visibility', 'hidden')
             } else element.toggle(showElement)
         })

        const invisibleElements = $('[data-invisible-in-mode]')
        invisibleElements.each((index, elem) => {
            let element = $(elem)
            let inVisibleInModes: PosModes = element.data('invisible-in-mode')
            let hideElement = inVisibleInModes.every(invisibleMode => {
                return mode.includes(invisibleMode)
            })
            element.toggle(!hideElement)
        })


         $('[data-active-in-mode]').each((index, elem) =>{
             const button = $(elem)
             const activeInMode : PosMode = button.data('active-in-mode')

            mode.includes(activeInMode)
                ? button.addClass('active')
                : button.removeClass('active')

         })

     }

$( () => ajax('/ajax/languageVars', null, 'GET', setupCore, null, null))