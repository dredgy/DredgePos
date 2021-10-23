let showLoginBox = () => {
    showVirtualNumpad('Enter Login Code', 6, true, false, false, authenticate);
};
let authenticate = (input) => {
    let login = ajaxSync('/ajax/authenticateClerk', input);
    if (login === 'success')
        redirect('/floorplan');
    else
        showLoginBox();
};
$(() => {
    showLoginBox();
});
//# sourceMappingURL=dredgepos.authenticate.js.map