/// <reference path="./typings/konva.d.ts" />
let stage;
let transformer;
let tableLayer;
let editMode = false;
let activeTables = [];
let selectedTable;
let selectedTableNumber;
let currentRoom;
let basis = 1280;
let scale = 1;
let newTable;
let roomName;
//Makes sure canvas always fits inside the div
function getDimensions(parentDiv) {
    let tableMap = $('#tableMap');
    let outerWidth = parentDiv.outerWidth();
    let outerHeight = parentDiv.outerHeight();
    let width = outerWidth;
    let height = outerWidth;
    if (outerWidth >= outerHeight) {
        width = outerHeight;
        height = outerHeight;
    }
    tableMap.height(height);
    tableMap.width(width);
    scale = width / basis;
    return { width: width, height: height };
}
function setupTableMap() {
    let doc = $(document);
    activeTables = ajaxSync('/ajax/getActiveTables/1', null, 'GET');
    let dimensions = getDimensions($('#mapContainer'));
    roomName = 'Deck & Courtyard';
    stage = new Konva.Stage({
        container: 'tableMap',
        width: dimensions.width,
        height: dimensions.height,
    });
    $('body').on('click', '.editModeButton', () => { toggleEditMode(); });
    $('.roomButton').on('click', function () {
        roomName = $(this).text();
        loadRoom($(this).data('value'));
    });
    $('.transferTableButton').on('click', function () {
        transferModeOn();
    });
    $('.addDecoration').on('click', function () {
        $('#decorator').css('display', 'flex');
    });
    $('.deleteDecoration').on('click', function () {
        deleteDecoration(selectedDecoration);
    });
    $('.decoratorItem').on('click', function () {
        addDecoration(this);
    });
    $('.changeShapeButton').on('click', function () {
        changeTableShape(selectedTableNumber);
    });
    $('.reserveTableButton').on('click', function () {
        if ($(this).text() === lang('reserve_table')) {
            reserveTable();
        }
        else {
            unreserveTable();
        }
    });
    $('.addTableButton').on('click', function () {
        addTable();
    });
    $('.deleteTableButton').on('click', function () {
        deleteTable();
    });
    loadRoom(roomToLoad);
}
let updateTableShape = (tableData) => {
    return ajaxSync('/ajax/updateTableShape', tableData);
};
//Change the shape of a table in edit mode.
function changeTableShape(tableNumber) {
    let tableData = getTableData(tableNumber);
    let tableShape = tableData['shape'];
    let tableWidth = tableData['width'];
    let tableHeight = tableData['height'];
    let tableRotation = tableData['rotation'];
    let order = ['square', 'rect', 'longrect', 'diamond', 'circle', 'ellipse', 'longellipse'];
    if (order.indexOf(tableShape) === -1)
        tableShape = 'square';
    //What the next shape is
    let currentIndex = order.indexOf(tableShape);
    let nextIndex = currentIndex + 1;
    if (nextIndex > (order.length) - 1)
        nextIndex = 0;
    let nextShape = order[nextIndex];
    switch (nextShape) {
        case 'square':
        case 'circle':
            tableHeight = tableWidth;
            tableRotation = 0;
            break;
        case 'diamond':
            tableHeight = tableWidth;
            tableRotation = 45;
            break;
        case 'rect':
        case 'ellipse':
            tableHeight = tableWidth * 2;
            tableRotation = 0;
            break;
        case 'longrect':
        case 'longellipse':
            tableRotation = 90;
            break;
    }
    let updateData = {
        table_number: tableNumber,
        shape: nextShape,
        height: tableHeight,
        width: tableWidth,
        rotation: tableRotation
    };
    tableData = updateTableShape(updateData);
    let tableGroup = stage.find('#' + tableNumber)[0];
    transformer.nodes([]);
    tableGroup.destroy();
    let newTable = createTableElement(tableData);
    tableLayer.add(newTable);
    stage.draw();
    selectTable(tableNumber);
    loadRoom(currentRoom, tableNumber);
}
let createTable = (tableData) => {
    return ajaxSync('/ajax/createTable', tableData);
};
let tableExists = (tableNumber) => {
    return ajaxSync(`/ajax/tableExists/${tableNumber}`);
};
function addTable(tableNumber) {
    if (!tableNumber) {
        showVirtualNumpad(lang('new_table_number'), 4, false, false, true, addTable);
    }
    else {
        let newTableInfo = {
            table_number: tableNumber,
            room_id: currentRoom,
            default_covers: 2,
            width: 200,
            height: 200,
            rotation: 0,
            pos_x: basis / 2,
            pos_y: basis / 2,
            shape: 'square',
            merged_children: '',
            previous_state: '',
            status: 0,
            reservation: 0,
            venue_id: 1
        };
        let newTableData = createTable(newTableInfo);
        if (!newTableData.table_number) {
            alert(newTableData);
            return false;
        }
        newTable = createTableElement(newTableData);
        tableLayer.add(newTable);
        tableLayer.draw();
        selectTable(tableNumber);
    }
}
function selectTable(tableNumber) {
    let table = stage.find('#' + tableNumber)[0];
    table.fire('click');
}
function deleteTable(tableNumber = 0) {
    if (!tableNumber) {
        confirm(lang('confirm_delete_table', selectedTableNumber), selectedTableNumber, 'Confirm', deleteTable);
    }
    else {
        if (tableIsOpen(selectedTableNumber)) {
            alert(lang('error_delete_existing_table'));
        }
        else {
            ajax(`/ajax/deleteTable/${selectedTableNumber}`, null, 'GET');
            let table = stage.find('#' + tableNumber)[0];
            transformer.nodes([]);
            table.destroy();
            tableLayer.draw();
            selectedTable = null;
            selectedTableNumber = null;
        }
    }
}
// Rotate a shape around any point.
// shape is a Konva shape
// angleDegrees is the angle to rotate by, in degrees.
// point is an object {x: posX, y: posY}
function rotateAroundPoint(shape, angleDegrees, point) {
    let angleRadians = angleDegrees * Math.PI / 180;
    // they lied, I did have to use trigonometry
    const x = point.x +
        (shape.x() - point.x) * Math.cos(angleRadians) -
        (shape.y() - point.y) * Math.sin(angleRadians);
    const y = point.y +
        (shape.x() - point.x) * Math.sin(angleRadians) +
        (shape.y() - point.y) * Math.cos(angleRadians);
    shape.rotation(shape.rotation() + angleDegrees); // rotate the shape in place
    shape.x(x); // move the rotated shape in relation to the rotation point.
    shape.y(y);
}
function createDecoration(data, idToSelect = false) {
    let draggable = editMode;
    var decoration = new Image();
    decoration.onload = function () {
        var dec = new Konva.Image({
            id: data.decoration_id.toString(),
            x: data.decoration_pos_x * scale,
            y: data.decoration_pos_y * scale,
            image: decoration,
            offsetX: data.decoration_width * 0.5 * scale,
            offsetY: data.decoration_height * 0.5 * scale,
            rotation: data.decoration_rotation,
            width: data.decoration_width * scale,
            height: data.decoration_height * scale,
            draggable: draggable,
        });
        if (editMode && dec.id() === idToSelect) {
            transformer.nodes([dec]);
            transformer.moveToTop();
        }
        dec.on('click', function () {
            selectDecoration(this);
        });
        dec.on('tap', function () {
            selectDecoration(this);
        });
        dec.on('dragend', function () {
            saveDecTransformation(this);
        });
        dec.on('transformend', function () {
            saveDecTransformation(this);
        });
        // add the shape to the layer
        tableLayer.add(dec);
        tableLayer.draw();
        dec.moveToBottom();
    };
    decoration.src = 'images/decorations/' + data.decoration_image;
    return decoration;
}
var selectedDecoration = false;
function selectDecoration(decoration) {
    if (editMode) {
        if ((transformer.nodes().length > 0 && transformer.nodes()[0] != decoration) || transformer.nodes().length == 0) {
            resetActiveTable();
            transformer.nodes([decoration]);
            decoration.moveToTop();
            transformer.moveToTop();
            selectedDecoration = decoration;
            toggleFloorplanControls();
        }
        else {
            transformer.nodes([]);
            selectedDecoration = false;
            $('.deleteDecoration').css('display', 'none');
        }
    }
}
function createTableElement(data, selectTable = false) {
    // Create container group
    let draggable = editMode || newTable === data.table_number;
    let table = new Konva.Group({
        x: data.pos_x * scale,
        y: data.pos_y * scale,
        draggable: draggable,
        listening: true,
        id: data.table_number.toString()
    });
    let fillColor = 'gray';
    if (data.status === 'reserved') {
        fillColor = 'lightgreen';
    }
    if (activeTables.includes(data.table_number)) {
        fillColor = 'lightblue';
    }
    data.width = data.width * scale;
    data.height = data.height * scale;
    // Create background shape
    let shape;
    switch (data.shape) {
        case "circle": // fall-through
        case "ellipse": // fall-through
        case "longellipse":
            shape = new Konva.Ellipse({
                x: 0,
                y: 0,
                radiusX: data.width * 0.5,
                radiusY: data.height * 0.5,
                rotation: data.rotation,
                fill: fillColor,
                stroke: "black",
                strokeWidth: 4,
                draggable: false,
                listening: true
            });
            break;
        default:
            shape = new Konva.Rect({
                x: 0,
                y: 0,
                offsetX: data.width * 0.5,
                offsetY: data.height * 0.5,
                width: data.width,
                height: data.height,
                rotation: data.rotation,
                fill: fillColor,
                stroke: "black",
                strokeWidth: 4,
                draggable: false,
                listening: true
            });
            break;
    } // End switch
    // Create label
    let label = new Konva.Text({
        x: data.width * -0.5,
        y: data.height * -0.5,
        width: data.width,
        height: data.height,
        text: data.table_number.toString(),
        fontSize: 40 * scale,
        fill: "black",
        align: "center",
        verticalAlign: "middle",
        draggable: false,
        listening: false
    });
    tableNumber = data.tablenumber;
    table.add(shape, label);
    table.on('dblclick', function () {
        tableNumber = parseInt(getTableNumber(this));
        if (!editMode) {
            loadScreen('orderScreen', 'table=' + tableNumber);
        }
    });
    table.on('dbltap', function () {
        tableNumber = getTableNumber(this);
        loadScreen('orderScreen', 'table=' + tableNumber);
    });
    table.on('dragend', function () {
        saveTransformation(table);
    });
    innerShape = getTableShape(table);
    table.on('click', function () {
        selectTableShape(this);
    });
    table.on('tap', function () {
        selectTableShape(this);
    });
    innerShape.on('transformend', function () {
        saveTransformation(table);
    });
    // add the shape to the layer
    tableLayer.add(table);
    table.moveToTop();
    if (tableNumber === selectedTableNumber) {
        selectTable = table;
    }
    if (selectTable) {
        if (selectTable === tableNumber) {
            table.fire('click');
        }
    }
    return table;
}
function loadRoom(room, selectTable = 0, selectDecoration = false) {
    //if (room === currentRoom) return false
    ajax(`/ajax/getRoomData/${room}`, null, 'GET', (response) => {
        let floorplanDiv = $('#tableMap');
        let backgroundImage = response.background_image;
        floorplanDiv.css("background-image", `url(images/rooms/${backgroundImage})`);
        floorplanDiv.css("background-size", `${width}px ${height}px`);
    }, null, null);
    $('.roomButton').removeClass('active');
    let selector = ".roomButton:contains('" + roomName + "')";
    $(selector).addClass('active');
    currentRoom = room;
    resetActiveTable();
    stage.destroy();
    stage = new Konva.Stage({
        container: 'tableMap',
        width: width,
        height: height,
    });
    transformer = new Konva.Transformer({
        rotationSnaps: [0, 15, 30, 45, 60, 75, 90, 105, 120, 135, 150, 165, 180, 225, 270, -15, -30, -45, -60, -75, -90, -105, -120, -135, -150, -165, -180, -225, -270, 360, -360],
        anchorSize: 40 * scale,
        ignoreStroke: true,
        centeredScaling: true
    });
    let tablesAndDecorations = ajaxSync(`/ajax/getTablesAndDecorations/${room}`, null, 'GET');
    let decorations = tablesAndDecorations['decorations'];
    let tables = tablesAndDecorations['tables'];
    tableLayer = new Konva.Layer();
    tableLayer.add(transformer);
    // Loop data and call the creation method for each decoration/table.
    decorations.forEach(itemData => {
        createDecoration(itemData, selectDecoration);
    });
    tables.forEach(itemData => {
        tableLayer.add(createTableElement(itemData, selectTable));
    });
    activeTables = getOpenTables();
    stage.add(tableLayer);
}
var mergeMode = false;
var parentMergeTable;
var childMergeTable;
var tableTransferOrigin;
var transferMode = false;
function transferModeOn() {
    mergeModeOff();
    if (!transferMode) {
        tableTransferOrigin = selectedTableNumber;
        transferMode = true;
        $('.transferTableButton').addClass('active');
        $('.transferTableButton').text('Select a table to transfer items to');
    }
    else {
        transferModeOff();
    }
}
function transferModeOff() {
    transferMode = false;
    $('.transferTableButton').removeClass('active');
    $('.transferTableButton').text(lang('transfer_table'));
}
let getOpenTables = () => {
    return ajaxSync('/ajax/getActiveTables/1', null, 'GET');
};
let transferTableAjax = (origin, destination) => {
    ajax(`/ajax/transferTables/${origin}/${destination}`, null, 'GET');
};
function transferTables() {
    destination = selectedTableNumber;
    origin = tableTransferOrigin;
    if (destination !== origin) {
        transferTableAjax(origin, destination);
        activeTables = getOpenTables();
        transferModeOff();
        getTableShape(selectedTable).fill('lightblue');
        getTableShape(getTableGroup(origin)).fill('gray');
    }
    else {
        alert("Can't transfer a table to itself.");
        transferModeOff();
    }
}
function mergeModeOn() {
    transferModeOff();
    if (!mergeMode) {
        mergeMode = true;
        $('.mergeButton').addClass('active');
        $('.mergeButton').text('Select a table to merge with Table ' + selectedTableNumber);
        parentMergeTable = selectedTableNumber;
    }
    else {
        mergeModeOff();
    }
}
function mergeModeOff() {
    mergeMode = false;
    $('.mergeButton').removeClass('active');
    $('.mergeButton').text(lang('merge_table'));
}
let ajaxMergeTables = (parent, child) => {
    return ajaxSync(`/ajax/mergeTables/${parent}/${child}`, null, 'GET');
};
let ajaxUnmergeTable = (parent) => {
    return ajaxSync(`/ajax/unmergeTable/${parent}`, null, 'GET');
};
function mergeTables() {
    parentMergeTable = parseInt(parentMergeTable);
    childMergeTable = parseInt(childMergeTable);
    if (childMergeTable !== parentMergeTable) {
        let result = ajaxMergeTables(parentMergeTable, childMergeTable);
        mergeModeOff();
        loadRoom(currentRoom);
        newTable = getTableGroup(parentMergeTable);
        newTable.draggable(true);
        if (tableIsOpen(parentMergeTable)) {
            getTableShape(newTable).fill('lightblue');
        }
    }
    else {
        alert("Can't merge a table with itself!");
        mergeModeOff();
    }
}
//When a table is passed (a group of the shape plus the text), returns the number as string.
function getTableNumber(tableGroup) {
    textItem = tableGroup.getChildren()[1];
    return textItem.getText();
}
function getTableGroup(tableNumber) {
    return stage.find('#' + tableNumber)[0];
}
function getTableShape(tableGroup) {
    return tableGroup.getChildren()[0];
}
function getReservation(id) {
    return ajaxSync('/ajax/getReservation', id);
}
//When a user selects a table.
function selectTableShape(table) {
    let tableNumber = getTableNumber(table);
    let shape = getTableShape(table);
    let strokeColor = shape.stroke();
    selectedTable = table;
    selectedTableNumber = tableNumber;
    if (transferMode)
        transferTables();
    if (mergeMode) {
        childMergeTable = tableNumber;
        mergeTables();
    }
    else {
        //If table is not selected
        if (strokeColor !== "yellow") {
            let tableData = getTableData(selectedTableNumber);
            let coverNumberString = lang('covers', tableData.default_covers.toString());
            let tableString = '<b>' + lang('activeTable', selectedTableNumber.toString()) + '</b>';
            $('.reserveTableButton').text(lang('reserve_table'));
            if (tableData.status === 'reserved') {
                let reservation = getReservation(tableData.reservation_id);
                console.log(reservation);
                $('.reserveTableButton').text(lang('unreserve_table'));
                if (reservation.reservation_name) {
                    reservationString = lang('reserved_for', reservation.reservation_name);
                }
                else {
                    reservationString = lang('reserved');
                }
                tableString += '<small>' + reservationString + '</small>';
            }
            tableString += "<small> (" + coverNumberString + ")</small>";
            $('.currentTable').html(tableString);
            stage.find('Rect').forEach(function (rect, index) {
                rect.stroke("black");
            });
            stage.find('Ellipse').forEach(function (circ, index) {
                circ.stroke("black");
            });
            shape.stroke("yellow");
            toggleEditControls(true);
            if (editMode) {
                toggleFloorplanControls();
                $('.deleteDecoration').css('display', 'none');
                transformer.nodes([getTableShape(table)]);
                table.moveToTop();
                transformer.moveToTop();
            }
            tableLayer.draw();
            //If the table is already selected
        }
        else {
            resetActiveTable();
            transformer.nodes([]);
            tableLayer.draw();
        }
    }
}
let getTableData = (tableNumber) => {
    return ajaxSync('/ajax/getTableData', tableNumber);
};
let isTableMerged = (tableNumber) => {
    let mergeData = getTableData(tableNumber).merged_children;
    return mergeData !== "";
};
function resetActiveTable() {
    if (!transferMode) {
        if (selectedTable) {
            getTableShape(selectedTable).stroke('black');
        }
        selectedTable = null;
        selectedTableNumber = "";
        toggleFloorplanControls(false, editMode);
        toggleEditControls(false);
    }
    else {
        $('.editControls').css('display', 'none');
    }
}
function addDecoration(button) {
    let insertData = {
        decoration_room: currentRoom,
        basis: basis,
        decoration_image: $(button).data('image')
    };
    ajaxSync('/ajax/addDecoration', insertData);
    $('#decorator').css('display', 'none');
    selectedDecoration = false;
    loadRoom(currentRoom);
}
function deleteDecoration(decoration) {
    ajax('/ajax/deleteDecoration', decoration.id());
    $('.deleteDecoration').css('display', 'none');
    decoration.destroy();
    selectedDecoration = false;
    transformer.nodes([]);
}
function saveDecTransformation(decoration) {
    let newData = {
        decoration_id: decoration.id(),
        decoration_pos_x: decoration.x() / scale,
        decoration_pos_y: decoration.y() / scale,
        decoration_width: parseInt((decoration.scaleX() * decoration.width()) / scale),
        decoration_height: parseInt((decoration.scaleY() * decoration.height()) / scale),
        decoration_rotation: parseInt(decoration.rotation()),
        decoration_image: decodeURIComponent(decoration.image().src),
        decoration_room: currentRoom
    };
    if (editMode) {
        idToSelect = decoration.id();
    }
    ajax('/ajax/updateDecoration', newData);
}
//When a table has been resized, rotated etc.
function saveTransformation(table) {
    tableNumber = getTableNumber(table);
    shape = getTableShape(table);
    newRotation = parseInt(shape.rotation());
    newWidth = parseInt(shape.scaleX() * shape.width() / scale);
    newHeight = parseInt((shape.scaleY() * shape.height()) / scale);
    newXPos = parseInt(table.x() / scale);
    newYPos = parseInt(table.y() / scale);
    updateData = {
        table_number: tableNumber,
        rotation: newRotation,
        width: newWidth,
        height: newHeight,
        pos_x: newXPos,
        pos_y: newYPos
    };
    transformTable(updateData);
}
let transformTable = (tableData) => {
    return ajax("/ajax/transformTable", tableData);
};
function unmergeTable() {
    ajaxUnmergeTable(selectedTableNumber);
    loadRoom(currentRoom);
}
function reserveTable(covers) {
    if (!covers) {
        showVirtualNumpad(lang('how_many_covers'), 2, false, false, true, reserveTable);
    }
    else {
        let table = getTableGroup(selectedTableNumber);
        let newReservation = ajaxSync('/ajax/newEmptyReservation', selectedTableNumber);
        table.fire('click');
        let tableShape = getTableShape(table);
        tableShape.fill('lightgreen');
        table.draw();
        table.fire('click');
        completeReservation(newReservation);
    }
}
function unreserveTable(input) {
    if (!input) {
        confirm(lang('confirm_delete_reservation', selectedTableNumber), selectedTableNumber, lang('confirm'), unreserveTable);
    }
    else {
        ajaxSync('/ajax/unreserveTable', input);
        hideAlerts();
        table = getTableGroup(input);
        table.fire('click');
        tableShape = getTableShape(table);
        tableShape.fill('gray');
        table.draw();
        table.fire('click');
    }
}
function completeReservation(resName) {
    if (!resName) {
        showVirtualKeyboard(lang('enter_reservation_name'));
    }
    else {
        //callPhpFunction('updateTableMapTable', [selectedTableNumber, 'reservation_name', resName]);
        loadRoom(currentRoom, selectedTableNumber);
    }
}
function toggleEditMode() {
    let editModeButton = $('.editModeButton');
    if (editMode === true) {
        editMode = false;
        loadRoom(currentRoom);
        editModeButton.removeClass('active');
        editModeButton.html(lang('edit_floorplan'));
        toggleFloorplanControls(false);
        if (selectedTable)
            selectedTable.fire('click');
        stage.find('Group').forEach(function (table, index) {
            table.draggable(false);
        });
    }
    else {
        editMode = true;
        stage.find('Group').forEach(function (table, index) {
            table.draggable(true);
            if (getTableShape(table).stroke() === "yellow") {
                table.moveToTop();
                transformer.nodes([getTableShape(table)]);
                transformer.moveToTop();
            }
        });
        stage.find('Image').forEach(function (img, index) {
            img.draggable(true);
        });
        toggleFloorplanControls();
        transformer.moveToTop();
        tableLayer.draw();
        editModeButton.addClass('active');
        editModeButton.html(lang('stop_edit_floorplan'));
    }
}
function toggleFloorplanControls(onOrOff = true, subControlsOnly = false) {
    if (onOrOff || subControlsOnly) {
        $('.floorplanControls').css('visibility', 'visible');
    }
    else {
        $('.floorplanControls').css('visibility', 'hidden');
    }
    if (selectedTable) {
        $('.changeShapeButton').css('visibility', 'visible');
        $('.deleteTableButton').css('visibility', 'visible');
    }
    else {
        $('.changeShapeButton').css('visibility', 'hidden');
        $('.deleteTableButton').css('visibility', 'hidden');
    }
    if (selectedDecoration) {
        $('.deleteDecoration').css('display', 'flex');
    }
    else {
        $('.deleteDecoration').css('display', 'none');
    }
}
let tableIsOpen = (tableNumber) => {
    return ajaxSync(`/ajax/tableIsOpen/${tableNumber}`, null, 'GET');
};
function toggleEditControls(onOrOff = true) {
    if (onOrOff) {
        $('.editControls').css("display", "flex");
        if (isTableMerged(selectedTableNumber)) {
            $('.mergeControls').css("visibility", "visible");
            $('.unmergeButton').css('display', 'flex');
            $('.mergeButton').css('display', 'flex');
        }
        else {
            $('.mergeControls').css("visibility", "visible");
            $('.mergeButton').css('display', 'flex');
            $('.unmergeButton').css('display', 'none');
        }
        if (tableIsOpen(selectedTableNumber)) {
            $('.payTableButton').css('display', 'flex');
            $('.viewTableButton').css('display', 'flex');
            $('.reserveTableButton').css('display', 'none');
            $('.transferTableButton').css('display', 'flex');
        }
        else {
            $('.payTableButton').css('display', 'none');
            $('.viewTableButton').css('display', 'none');
            $('.reserveTableButton').css('display', 'flex');
            $('.transferTableButton').css('display', 'none');
        }
    }
    else {
        $('.editControls').css("display", "none");
        $('.mergeControls').css("visibility", "hidden");
        $('.mergeButton').css("display", "none");
        $('.unmergeButton').css("display", "none");
    }
}
//# sourceMappingURL=dredgepos.floorplan.js.map