let showLoginBox = () => showVirtualNumpad('Enter Login Code', 6, true, false, false, authenticate)

let  authenticate = (input : string) => {
    let login = ajaxSync('/login/authenticateClerk', input)
    if(login === 'success'){
        location.assign('/floorplan/')
    }
    else
        showLoginBox()
}

$(() => {
    showLoginBox()
})