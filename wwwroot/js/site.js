// Drag & drop for multiplication table (input to input)
let dragSourceInput = null;
window.dragInputValue = function(ev) {
	ev.dataTransfer.setData("text", ev.target.value);
	dragSourceInput = ev.target;
}
window.allowDrop = function(ev) {
	ev.preventDefault();
}
window.dragAnswer = function(ev) {
	ev.dataTransfer.setData("text", ev.target.getAttribute("data-value"));
	dragSourceInput = null;
}
window.dropAnswer = function(ev) {
	ev.preventDefault();
	var value = ev.dataTransfer.getData("text");
	if (!/^[0-9]+$/.test(value)) return;
	ev.target.value = value;
	if (dragSourceInput && dragSourceInput !== ev.target) {
		dragSourceInput.value = "";
	}
	dragSourceInput = null;
}
// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Drag & drop for multiplication table
window.allowDrop = function(ev) {
	ev.preventDefault();
}
window.dragAnswer = function(ev) {
	ev.dataTransfer.setData("text", ev.target.getAttribute("data-value"));
}
window.dropAnswer = function(ev) {
	ev.preventDefault();
	var value = ev.dataTransfer.getData("text");
	if (!/^[0-9]+$/.test(value)) return;
	ev.target.value = value;
}
