window.appLanguage = {
    submit: function (formId) {
        var form = document.getElementById(formId);
        if (form) { form.requestSubmit(); }
    }
};

// Combobox keys that must not move the caret or submit the surrounding form while the results list is open.
document.addEventListener("keydown", function (event) {
    var target = event.target;
    if (!(target instanceof HTMLInputElement) || !target.hasAttribute("data-app-combobox")) { return; }
    var open = target.getAttribute("aria-expanded") === "true";
    if (!open) { return; }
    if (event.key === "ArrowDown" || event.key === "ArrowUp" || event.key === "Escape" || (event.key === "Enter" && target.getAttribute("aria-activedescendant"))) { event.preventDefault(); }
}, true);
// Enter toggles a focused checkbox (like Space) instead of submitting the form; Tab keeps its normal focus movement.
document.addEventListener("keydown", function (event) {
    var target = event.target;
    if (event.key !== "Enter" || event.defaultPrevented || event.isComposing) { return; }
    if (!(target instanceof HTMLInputElement) || target.type !== "checkbox" || target.disabled) { return; }
    event.preventDefault();
    target.click();
});

// Focus management after a failed submit: first field the form marked invalid.
window.appFocus = {
    firstInvalid: function () {
        var dialog = document.querySelector("[role=dialog][open], dialog[open], [role=dialog]");
        var root = dialog || document;
        var field = root.querySelector("[aria-invalid=true]");
        if (field) { field.focus(); }
    }
};
