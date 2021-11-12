/// <reference path="./typings/konva.d.ts" />
const Floorplan = {
    rooms: [],
    tables: [],
    decorations: [],
    reservations: [],
    activeTableNumbers: [],
    stage: null,
    transformer: null,
    tableLayer: null,
    selectedTableNumber: 0,
    currentRoom: null,
    roomToLoad: null,
    visualScale: 1,
    visualScaleBasis: 1280,
    floorplanDiv: null,
    selectedDecorationId: 0
};
$(() => ajax('/ajax/getFloorplanData/1', null, 'get', setupFloorplan, null, null));
const setupFloorplanEvents = () => {
    const doc = $(document);
    doc.on('click', '.roomButton', roomButtonClicked);
    doc.on('click', '.editModeButton', editModeButtonClicked);
    doc.on('click', '.changeShapeButton', changeTableShape);
    doc.on('click', '.addTableButton', showAddTablePopup);
    doc.on('click', '.deleteTableButton', confirmDeleteTable);
    doc.on('click', '.addDecoration', showDecorator);
    doc.on('click', '.deleteDecoration', deleteDecoration);
    doc.on('click', '.decoratorItem', addDecoration);
    doc.on('click', '.mergeButton', toggleMergeMode);
    doc.on('click', '.unmergeButton', unmergeTable);
    doc.on('click', '.transferTableButton', toggleTransferMode);
    doc.on('click', '.reserveTableButton', reserveTable);
    doc.on('click', '.unreserveTableButton', unreserveTable);
};
const roomButtonClicked = (e) => {
    const button = $(e.target);
    const roomId = button.data('value');
    loadRoom(getRoomById(roomId));
};
const editModeButtonClicked = (e) => {
    const button = $(e.target);
    button.toggleClass('active');
    toggleMode('edit');
    if (isInMode('edit')) {
        Floorplan.stage.find('Group, Image').forEach(table => table.draggable(true));
        if (isInMode('tableSelected')) {
            const selectedTableShape = getTableShapeFromTableNumber(Floorplan.selectedTableNumber);
            selectTable(selectedTableShape);
        }
    }
    else {
        setTransformerNodes([]);
        Floorplan.stage.find('Group, Image').forEach(table => table.draggable(false));
    }
};
const setupFloorplan = (floorplanData) => {
    Floorplan.tables = floorplanData.tables;
    Floorplan.activeTableNumbers = floorplanData.activeTableNumbers;
    Floorplan.rooms = floorplanData.rooms;
    Floorplan.decorations = floorplanData.decorations;
    Floorplan.reservations = floorplanData.reservations;
    getDimensions();
    setupFloorplanEvents();
    loadRoom(Floorplan.rooms[0]);
};
const loadRoom = (roomToLoad) => {
    setRoomBackground(roomToLoad);
    setupKonva();
    $('.roomButton').removeClass('active');
    let button = $(`.roomButton[data-value=${roomToLoad.id}]`);
    button.addClass('active');
    const tablesInRoom = Floorplan.tables.filter(table => table.room_id == roomToLoad.id);
    const decorationsInRoom = Floorplan.decorations.filter(decoration => decoration.decoration_room == roomToLoad.id);
    decorationsInRoom.forEach(decoration => createDecorationShape(decoration, false));
    tablesInRoom.forEach(createTableShape);
    if (!isInMode('edit')) {
        deselectTables();
    }
    Floorplan.currentRoom = roomToLoad;
};
const getRoomById = (roomId) => {
    return Floorplan.rooms.find((room) => room.id == roomId);
};
const tableIsOpen = (table) => Floorplan.activeTableNumbers.includes(table.table_number);
const createTableShape = (table) => {
    const draggable = isInMode('edit');
    const tableGroup = new Konva.Group({
        x: table.pos_x * Floorplan.visualScale,
        y: table.pos_y * Floorplan.visualScale,
        draggable: draggable,
        listening: true,
        id: table.table_number.toString()
    });
    const fillColor = tableIsOpen(table)
        ? 'lightblue'
        : table.status == 2
            ? 'lightgreen'
            : 'gray';
    let tableShape;
    switch (table.shape) {
        case "circle": // fall-through
        case "ellipse": // fall-through
        case "longellipse":
            tableShape = new Konva.Ellipse({
                x: 0,
                y: 0,
                radiusX: table.width * 0.5 * Floorplan.visualScale,
                radiusY: table.height * 0.5 * Floorplan.visualScale,
                rotation: table.rotation,
                fill: fillColor,
                stroke: "black",
                strokeWidth: 4,
                draggable: false,
                listening: true
            });
            break;
        default:
            tableShape = new Konva.Rect({
                x: 0,
                y: 0,
                offsetX: table.width * 0.5 * Floorplan.visualScale,
                offsetY: table.height * 0.5 * Floorplan.visualScale,
                width: table.width * Floorplan.visualScale,
                height: table.height * Floorplan.visualScale,
                rotation: table.rotation,
                fill: fillColor,
                stroke: "black",
                strokeWidth: 4,
                draggable: false,
                listening: true
            });
            break;
    }
    const label = new Konva.Text({
        x: table.width * -0.5 * Floorplan.visualScale,
        y: table.height * -0.5 * Floorplan.visualScale,
        width: table.width * Floorplan.visualScale,
        height: table.height * Floorplan.visualScale,
        text: table.table_number.toString(),
        fontSize: 40 * Floorplan.visualScale,
        fill: "black",
        align: "center",
        verticalAlign: "middle",
        draggable: false,
        listening: false
    });
    tableGroup.add(tableShape, label);
    setupTableEvents(tableGroup);
    Floorplan.tableLayer.add(tableGroup);
    return tableGroup;
};
const setupTableEvents = (tableGroup) => {
    const tableShape = getTableShapeFromGroup(tableGroup);
    tableGroup.on('click', tableClicked);
    tableGroup.on('tap', tableClicked);
    tableGroup.on('dragend', tableGroupTransformed);
    tableShape.on('transformend', tableShapeTransformed);
};
const getTableShapeFromGroup = (group) => group.getChildren()[0];
const getTableGroupFromShape = (shape) => shape.parent;
const tableGroupTransformed = (e) => {
    saveTableTransformation(e.target);
};
const tableShapeTransformed = (e) => {
    let shape = e.target;
    let group = getTableGroupFromShape(shape);
    saveTableTransformation(group);
};
const saveTableTransformation = (tableGroup) => {
    const originalTable = getTableDataFromGroup(tableGroup);
    const tableShape = getTableShapeFromGroup(tableGroup);
    const newTableInfo = {
        table_number: originalTable.table_number,
        previous_state: originalTable.previous_state,
        merged_children: originalTable.merged_children,
        id: originalTable.id,
        width: Math.round(tableShape.scaleX() * tableShape.width() / Floorplan.visualScale),
        height: Math.round(tableShape.scaleY() * tableShape.height() / Floorplan.visualScale),
        pos_x: Math.round(tableGroup.x() / Floorplan.visualScale),
        pos_y: Math.round(tableGroup.y() / Floorplan.visualScale),
        rotation: Math.round(tableShape.rotation()),
        room_id: originalTable.room_id,
        status: originalTable.status,
        venue_id: originalTable.venue_id,
        shape: originalTable.shape,
        default_covers: originalTable.default_covers,
    };
    saveTable(newTableInfo);
    redrawTable(tableGroup);
};
const saveTable = (tableToUpdate) => {
    const tables = Floorplan
        .tables
        .filter(table => {
        return table.id != tableToUpdate.id;
    });
    tables.push(tableToUpdate);
    Floorplan.tables = tables;
    ajax("/ajax/transformTable", tableToUpdate, 'post', null, null, null);
};
const setTransformerNodes = (nodes) => {
    Floorplan.transformer.moveToTop();
    if (nodes.length < 1)
        Floorplan.transformer.moveToBottom();
    Floorplan.transformer.nodes(nodes);
};
const getTableDataFromTableNumber = (tableNumber) => {
    return Floorplan.tables.filter(table => table.table_number == tableNumber)[0];
};
const getTableDataFromGroup = (tableGroup) => {
    const tableNumber = tableGroup.attrs.id;
    return Floorplan.tables.find(table => tableNumber == table.table_number);
};
const getTableDataFromShape = (tableShape) => getTableDataFromGroup(tableShape.parent);
const getTableShapeFromTableNumber = (tableNumber) => {
    const tableGroup = Floorplan.stage.find('Group').find((group) => {
        return group.attrs.id == tableNumber;
    });
    return tableGroup.getChildren()[0];
};
const getTableGroupFromTableNumber = (tableNumber) => {
    const tableShape = getTableShapeFromTableNumber(tableNumber);
    return getTableGroupFromShape(tableShape);
};
const setReservationStatus = (table) => {
    const reservationText = $('.reservationStatus');
    const tableShape = getTableShapeFromTableNumber(table.table_number);
    reservationText.text('');
    if (table.status == 2) {
        tableShape.fill('lightgreen');
        const reservations = Floorplan.reservations.filter(reservation => reservation.reservation_table_id == table.id);
        if (reservations.length) {
            turnOnMode('reservedTableSelected');
            reservationText.text(lang('reserved'));
            let reservation = reservations[0];
            if (reservation.reservation_name != '') {
                reservationText.text(lang('reserved_for', reservation.reservation_name));
            }
        }
    }
    else {
        let fillColor = tableIsOpen(table) ? 'lightblue' : 'gray';
        tableShape.fill(fillColor);
        turnOffMode('reservedTableSelected');
    }
};
const reserveTable = () => {
    showVirtualNumpad(lang('how_many_covers'), 2, false, false, true, createEmptyReservation);
};
const createEmptyReservation = (covers) => {
    const newReservation = {
        id: 0,
        reservation_covers: covers,
        reservation_created_at: 0,
        reservation_table_id: getSelectedTableData().id,
        reservation_name: '',
        reservation_time: 0,
    };
    ajax('/ajax/newEmptyReservation', newReservation, 'post', emptyReservationCreated, null, null);
};
const emptyReservationCreated = (reservation) => {
    Floorplan.reservations.push(reservation);
    const selectedTable = getSelectedTableData();
    selectedTable.status = 2;
    selectedTable.default_covers = reservation.reservation_covers;
    updateTableData(selectedTable);
    updateCoverText(selectedTable);
    setReservationStatus(getSelectedTableData());
    showVirtualKeyboard(lang('confirm_reservation_name'), 32, false, addReservationName);
};
const addReservationName = (name) => {
    hideVirtualKeyboard();
    const reservation = Floorplan.reservations.filter(reservation => reservation.reservation_table_id == getSelectedTableData().id)[0];
    reservation.reservation_name = name;
    ajax('/ajax/updateReservation', reservation, 'post', reservationNameAdded, null, null);
};
const reservationNameAdded = (updatedReservation) => {
    console.log(updatedReservation);
    Floorplan.reservations = Floorplan.reservations.filter(reservation => reservation.id != updatedReservation.id);
    Floorplan.reservations.push(updatedReservation);
    setReservationStatus(getSelectedTableData());
};
const getReservationsOnTable = (table) => Floorplan.reservations.filter(reservation => reservation.reservation_table_id == table.id);
const updateTableData = (tableToRemove) => {
    Floorplan.tables = Floorplan.tables.filter(table => table.id != tableToRemove.id);
    Floorplan.tables.push(tableToRemove);
};
const unreserveTable = () => {
    const selectedTable = getSelectedTableData();
    selectedTable.status = 0;
    ajax('/ajax/unreserveTable', selectedTable, 'post', tableUnreserved, null, null);
};
const tableUnreserved = (table) => {
    Floorplan.reservations = Floorplan.reservations.filter(reservation => reservation.reservation_table_id != table.id);
    updateTableData(table);
    setReservationStatus(table);
};
const getSelectedTableData = () => getTableDataFromTableNumber(Floorplan.selectedTableNumber);
const deselectTables = () => {
    Floorplan.stage.find('Rect, Ellipse').forEach((shape, index) => {
        shape.stroke('black');
    });
    Floorplan.selectedDecorationId = 0;
    Floorplan.selectedTableNumber = 0;
    turnOffMode('tableSelected');
    turnOffMode('activeTableSelected');
    turnOffMode('decorationSelected');
    turnOffMode('merge');
    turnOffMode('transfer');
    setTransformerNodes([]);
};
const selectTable = (tableShape) => {
    tableShape.stroke('yellow');
    const table = getTableDataFromShape(tableShape);
    Floorplan.selectedTableNumber = table.table_number;
    if (isInMode('edit')) {
        setTransformerNodes([tableShape]);
    }
    if (tableIsOpen(table)) {
        turnOnMode('activeTableSelected');
    }
    $('.reservationStatus').html('<b>' + lang('active_table', table.table_number.toString() + '</b>'));
    updateCoverText(table);
    $('.selectedTableNumber').text(lang('active_table', table.table_number.toString()));
    setReservationStatus(table);
    const unmergeVisibility = table.merged_children ? 'visible' : 'hidden';
    $('.unmergeButton').css('visibility', unmergeVisibility);
    turnOnMode('tableSelected');
};
const updateCoverText = (table) => $('.selectedTableCovers').text(lang('covers', table.default_covers.toString()));
const tableClicked = (event) => {
    let tableShape = getTableShapeFromGroup(event.currentTarget);
    const table = getTableDataFromShape(tableShape);
    if (isInMode('merge')) {
        mergeTables(getTableDataFromTableNumber(Floorplan.selectedTableNumber), table);
        return;
    }
    if (isInMode('transfer')) {
        transferTables(getTableDataFromTableNumber(Floorplan.selectedTableNumber), table);
    }
    const selectedTableNumber = Floorplan.selectedTableNumber;
    deselectTables();
    if (selectedTableNumber != table.table_number) {
        selectTable(tableShape);
    }
};
const createDecorationShape = (decoration, select) => {
    const draggable = isInMode('edit');
    const decorationShape = new Image();
    decorationShape.onload = () => {
        const decorationImage = new Konva.Image({
            id: decoration.id.toString(),
            x: decoration.decoration_pos_x * Floorplan.visualScale,
            y: decoration.decoration_pos_y * Floorplan.visualScale,
            image: decorationShape,
            offsetX: decoration.decoration_width * 0.5 * Floorplan.visualScale,
            offsetY: decoration.decoration_height * 0.5 * Floorplan.visualScale,
            rotation: decoration.decoration_rotation,
            width: decoration.decoration_width * Floorplan.visualScale,
            height: decoration.decoration_height * Floorplan.visualScale,
            draggable: draggable,
        });
        // add the shape to the layer
        Floorplan.tableLayer.add(decorationImage);
        Floorplan.tableLayer.draw();
        decorationImage.moveToBottom();
        setupDecorationEvents(decorationImage);
        if (select) {
            decorationImage.moveToTop();
            selectDecorationShape(decorationImage);
        }
    };
    decorationShape.src = 'images/decorations/' + decoration.decoration_image;
};
const setupDecorationEvents = (decorationShape) => {
    decorationShape.on('click', decorationClicked);
    decorationShape.on('tap', decorationClicked);
    decorationShape.on('transformend', decorationTransformed);
    decorationShape.on('dragend', decorationTransformed);
};
const decorationClicked = (event) => {
    let decorationShape = event.target;
    if (isInMode('edit')) {
        turnOffMode('tableSelected');
        if ((Floorplan.transformer.nodes().length > 0 && Floorplan.transformer.nodes()[0] != decorationShape) || Floorplan.transformer.nodes().length == 0) {
            selectDecorationShape(decorationShape);
        }
        else {
            deselectTables();
            decorationShape.moveToBottom();
        }
    }
};
const selectDecorationShape = (decorationShape) => {
    deselectTables();
    Floorplan.transformer.nodes([decorationShape]);
    Floorplan.selectedDecorationId = Number(decorationShape.id());
    decorationShape.moveToTop();
    Floorplan.transformer.moveToTop();
    turnOnMode('decorationSelected');
};
const getDecorationDataById = (id) => {
    return Floorplan.decorations.find(decoration => id == decoration.id);
};
const decorationTransformed = (event) => {
    let decorationShape = event.currentTarget;
    const oldDecorationData = getDecorationDataById(Number(decorationShape.id()));
    const newDecoration = {
        id: oldDecorationData.id,
        decoration_room: oldDecorationData.decoration_room,
        decoration_pos_x: Math.round(decorationShape.x() / Floorplan.visualScale),
        decoration_pos_y: Math.round(decorationShape.y() / Floorplan.visualScale),
        decoration_rotation: Math.round(decorationShape.rotation()),
        decoration_width: Math.round((decorationShape.scaleX() * decorationShape.width()) / Floorplan.visualScale),
        decoration_height: Math.round((decorationShape.scaleY() * decorationShape.height()) / Floorplan.visualScale),
        decoration_image: oldDecorationData.decoration_image,
    };
    saveDecoration(newDecoration);
};
const saveDecoration = (decorationToUpdate) => {
    const decorations = Floorplan
        .decorations
        .filter(decoration => {
        return decoration.id != decorationToUpdate.id;
    });
    decorations.push(decorationToUpdate);
    Floorplan.decorations = decorations;
    ajax("/ajax/updateDecoration", decorationToUpdate, 'post', null, null, null);
};
const showDecorator = () => $('#decorator').css('display', 'flex');
const hideDecorator = () => $('#decorator').css('display', 'flex').hide();
const addDecoration = (e) => {
    const button = $(e.currentTarget);
    const newDecoration = {
        id: 0,
        decoration_room: Floorplan.currentRoom.id,
        decoration_pos_x: Floorplan.visualScaleBasis / 2,
        decoration_pos_y: Floorplan.visualScaleBasis / 2,
        decoration_rotation: 0,
        decoration_width: 200,
        decoration_height: 200,
        decoration_image: button.data('image')
    };
    ajax('/ajax/addDecoration', newDecoration, 'post', decorationAdded, null, null);
};
const decorationAdded = (decoration) => {
    Floorplan.decorations.push(decoration);
    createDecorationShape(decoration, true);
    hideDecorator();
};
const deleteDecoration = () => ajax('/ajax/deleteDecoration', getDecorationDataById(Floorplan.selectedDecorationId), 'post', decorationDeleted, null, null);
const decorationDeleted = (deletedDecoration) => {
    Floorplan.decorations = Floorplan.decorations.filter(decoration => decoration.id != deletedDecoration.id);
    const decorationShape = Floorplan.stage.findOne(`#${deletedDecoration.id}`);
    decorationShape.destroy();
    deselectTables();
};
const setRoomBackground = (roomToLoad) => {
    const width = Floorplan.floorplanDiv.width();
    const height = Floorplan.floorplanDiv.height();
    Floorplan.floorplanDiv.css("background-image", `url(images/rooms/${roomToLoad.background_image})`);
    Floorplan.floorplanDiv.css("background-size", `${width}px ${height}px`);
};
const setupKonva = () => {
    const dimensions = getDimensions();
    if (Floorplan.stage !== null)
        Floorplan.stage.destroy();
    Floorplan.stage = new Konva.Stage({
        container: 'floorplanCanvas',
        width: dimensions.width,
        height: dimensions.height,
    });
    Floorplan.stage.on('click', e => {
        if (e.target == Floorplan.stage) {
            deselectTables();
        }
    });
    Floorplan.transformer = new Konva.Transformer({
        rotationSnaps: [0, 15, 30, 45, 60, 75, 90, 105, 120, 135, 150, 165, 180, 225, 270, -15, -30, -45, -60, -75, -90, -105, -120, -135, -150, -165, -180, -225, -270, 360, -360],
        anchorSize: 30 * Floorplan.visualScale,
        ignoreStroke: true,
        centeredScaling: true,
        anchorCornerRadius: 10,
    });
    Floorplan.tableLayer = new Konva.Layer();
    Floorplan.tableLayer.add(Floorplan.transformer);
    Floorplan.stage.add(Floorplan.tableLayer);
};
const resetKonva = setupKonva;
const changeTableShape = () => {
    if (!Floorplan.selectedTableNumber)
        return;
    const table = getTableDataFromTableNumber(Floorplan.selectedTableNumber);
    const tableGroup = getTableGroupFromTableNumber(table.table_number);
    const order = ['square', 'rect', 'longrect', 'diamond', 'circle', 'ellipse', 'longellipse'];
    if (order.indexOf(table.shape) === -1)
        table.shape = 'square';
    const currentIndex = order.indexOf(table.shape);
    let nextIndex = currentIndex + 1;
    if (nextIndex > (order.length) - 1)
        nextIndex = 0;
    table.shape = order[nextIndex];
    switch (table.shape) {
        case 'square':
        case 'circle':
            // noinspection JSSuspiciousNameCombination
            table.height = table.width;
            table.rotation = 0;
            break;
        case 'diamond':
            // noinspection JSSuspiciousNameCombination
            table.height = table.width;
            table.rotation = 45;
            break;
        case 'rect':
        case 'ellipse':
            table.height = table.width * 2;
            table.rotation = 0;
            break;
        case 'longrect':
        case 'longellipse':
            table.rotation = 90;
            break;
    }
    saveTable(table);
    deselectTables();
    redrawTable(tableGroup);
};
const redrawTable = (tableGroup) => {
    deselectTables();
    const draggable = tableGroup.draggable();
    const table = getTableDataFromGroup(tableGroup);
    tableGroup.destroy();
    const newTableGroup = createTableShape(table);
    const newTableShape = getTableShapeFromTableNumber(table.table_number);
    selectTable(newTableShape);
    newTableGroup.draggable(draggable);
};
const showAddTablePopup = () => showVirtualNumpad(lang('new_table_number'), 4, false, false, true, addTable);
const addTable = (tableNumber) => {
    const newTable = {
        id: 0,
        table_number: tableNumber,
        room_id: Floorplan.currentRoom.id,
        default_covers: 2,
        width: 200,
        height: 200,
        rotation: 0,
        pos_x: Floorplan.visualScaleBasis / 2,
        pos_y: Floorplan.visualScaleBasis / 2,
        shape: 'square',
        merged_children: '',
        previous_state: '',
        status: 0,
        venue_id: 1
    };
    ajax('/ajax/createTable', newTable, 'post', tableAdded, tableNotAdded, null);
};
const tableAdded = (table) => {
    deselectTables();
    const newTableGroup = createTableShape(table);
    Floorplan.tables.push(table);
    selectTable(getTableShapeFromGroup(newTableGroup));
};
const tableNotAdded = (response) => {
    posAlert(response);
};
const confirmDeleteTable = () => confirmation(lang('confirm_delete_table', Floorplan.selectedTableNumber.toString()), Floorplan.selectedTableNumber, 'Confirm', deleteTable);
const deleteTable = (tableNumber) => {
    if (!tableNumber)
        return false;
    const tableToDelete = getTableDataFromTableNumber(tableNumber);
    if (tableIsOpen(tableToDelete)) {
        posAlert(lang('error_delete_existing_table'));
        return false;
    }
    ajax(`/ajax/deleteTable`, tableToDelete, 'post', tableDeleted, null, null);
};
const tableDeleted = (deletedTable) => {
    Floorplan.tables = Floorplan.tables.filter(table => table.table_number != deletedTable.table_number);
    const tableGroup = getTableGroupFromTableNumber(deletedTable.table_number);
    deselectTables();
    tableGroup.destroy();
};
const toggleMergeMode = () => toggleMode('merge');
const mergeTables = (table1, table2) => {
    toggleMergeMode();
    if (table1.table_number == table2.table_number) {
        posAlert(lang('error_self_merge'));
        return false;
    }
    ajax('/ajax/mergeTables', [table1, table2], 'post', tablesMerged, null, null);
};
const tablesMerged = (tables) => {
    tableDeleted(tables['child']);
    tableDeleted(tables['parent']);
    tableAdded(tables['merged']);
    deselectTables();
    const tableGroup = getTableGroupFromTableNumber(tables['merged'].table_number);
    selectTable(getTableShapeFromGroup(tableGroup));
    tableGroup.draggable(true);
};
const unmergeTable = () => ajax(`/ajax/unmergeTable/${Floorplan.selectedTableNumber}`, null, 'get', tablesUnmerged, null, null);
const tablesUnmerged = (tables) => {
    const parentTable = tables['parent'];
    const childTable = tables['child'];
    tableDeleted(parentTable);
    tableAdded(parentTable);
    tableAdded(childTable);
    deselectTables();
};
const toggleTransferMode = () => toggleMode('transfer');
const transferTables = (origin, destination) => {
    if (origin.table_number == destination.table_number) {
        posAlert(lang('transfer_self_error'));
        return;
    }
    ajax(`/ajax/transferTable/${origin.table_number}/${destination.table_number}`, null, 'get', tableTransferred, null, null);
};
const tableTransferred = (tables) => {
    const origin = tables['origin'];
    const destination = tables['destination'];
    Floorplan.activeTableNumbers = Floorplan.activeTableNumbers.filter(tableNumber => tableNumber != origin.table_number);
    Floorplan.activeTableNumbers.push(destination.table_number);
    if (Floorplan.currentRoom.id == origin.room_id) {
        redrawTable(getTableGroupFromTableNumber(origin.table_number));
    }
    redrawTable(getTableGroupFromTableNumber(destination.table_number));
};
const getDimensions = () => {
    Floorplan.floorplanDiv = $('#floorplanCanvas');
    const parentDiv = $('#floorplanCenterColumn .middleCell');
    const outerWidth = parentDiv.outerWidth();
    const outerHeight = parentDiv.outerHeight();
    if (outerWidth >= outerHeight) {
        Floorplan.floorplanDiv.css('height', '100%');
    }
    else {
        Floorplan.floorplanDiv.css('width', '100%');
    }
    Floorplan.visualScale = Floorplan.floorplanDiv.width() / Floorplan.visualScaleBasis;
    return { width: Floorplan.floorplanDiv.width(), height: Floorplan.floorplanDiv.height() };
};
//# sourceMappingURL=dredgepos.floorplan.js.map