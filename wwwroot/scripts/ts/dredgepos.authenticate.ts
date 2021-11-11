let showLoginBox = () => showVirtualNumpad('Enter Login Code', 6, true, false, false, authenticate)

let  authenticate = (input : string) => {
    let login = ajaxSync('/ajax/authenticateClerk', input)
    if(login === 'success')
        redirect('/floorplan')
    else
        showLoginBox()
}

$(() => {
    showLoginBox()
})