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