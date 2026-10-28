// Native DOM properties that cannot be expressed by HTML attributes.
export function syncCheckbox(input, indeterminate) {
  if (input instanceof HTMLInputElement && input.type === "checkbox") {
    input.indeterminate = indeterminate;
  }
}

const otpStates = new WeakMap();
const otpConnections = new Map();

// A single real input owns editing, paste and autofill. Interop only maps the
// visible slot geometry to its native selection and reports selection/focus.
export function connectOtp(root, input, reference, connectionId) {
  if (otpStates.has(root)) return syncOtp(root);
  const state = { root, input, reference, connectionId, slots: [], last: "", disposed: false, accepted: input.value };
  state.notify = () => {
    const start = Math.min(input.selectionStart ?? 0, input.maxLength);
    const end = Math.min(input.selectionEnd ?? start, input.maxLength);
    const focused = document.activeElement === input;
    const key = `${start}:${end}:${focused}`;
    if (state.disposed || key === state.last) return;
    state.last = key;
    void reference.invokeMethodAsync("NativeSelectionChanged", start, end, focused).catch(error => {
      if (!state.disposed) throw error;
    });
  };
  state.focus = () => {
    if (!state.pointerFocus) {
      const length = input.value.length;
      input.setSelectionRange(Math.min(length, input.maxLength - 1), length);
      state.notify();
    }
  };
  state.pointer = event => {
    if (input.disabled || event.button !== 0 || !state.slots.length) return;
    event.preventDefault();
    let nearest = 0, distance = Infinity;
    state.slots.forEach((slot, index) => {
      const box = slot.getBoundingClientRect();
      const next = (event.clientX - box.x - box.width / 2) ** 2 + (event.clientY - box.y - box.height / 2) ** 2;
      if (next < distance) { nearest = Number(slot.dataset.rbiOtpSlot); distance = next; }
    });
    state.pointerFocus = true;
    input.focus({ preventScroll: true });
    state.pointerFocus = false;
    const start = Math.min(nearest, input.value.length);
    input.setSelectionRange(start, Math.min(start + 1, input.value.length));
    state.notify();
  };
  // The source moves a one-character selection through the code instead of a caret, so typing over a full code replaces a character.
  // Plain arrows/Home/End follow that model; modified keys keep the browser's native selection extension.
  state.keydown = event => {
    if (input.disabled || event.shiftKey || event.ctrlKey || event.altKey || event.metaKey) return;
    const length = input.value.length, limit = input.maxLength;
    const select = index => {
      event.preventDefault();
      if (length === 0) input.setSelectionRange(0, 0);
      else if (index >= length) { if (length < limit) input.setSelectionRange(length, length); else input.setSelectionRange(length - 1, length); }
      else { const start = Math.max(0, index); input.setSelectionRange(start, start + 1); }
      state.notify();
    };
    const start = input.selectionStart ?? 0, end = input.selectionEnd ?? start;
    if (event.key === "ArrowLeft") select(start - 1);
    else if (event.key === "ArrowRight") select(start === end ? start : end);
    else if (event.key === "Home") select(0);
    else if (event.key === "End") select(length);
  };
  state.selection = () => { if (document.activeElement === input) state.notify(); };
  state.beforeInput = () => { state.previousSelection = [input.selectionStart ?? 0, input.selectionEnd ?? 0]; };
  state.accepts = value => !value.length || !input.pattern || new RegExp(input.pattern).test(value);
  state.inputting = event => {
    if (input.disabled || input.readOnly || !state.accepts(input.value)) {
      input.value = state.accepted;
      const selection = state.previousSelection ?? [state.accepted.length, state.accepted.length];
      input.setSelectionRange(...selection);
      event.stopImmediatePropagation();
      state.notify();
      return;
    }
    state.accepted = input.value;
  };
  state.paste = async event => {
    if (input.dataset.rbiOtpTransformPaste !== "true" || !event.clipboardData) return;
    event.preventDefault();
    if (input.disabled || input.readOnly) return;
    const previous = input.value, start = input.selectionStart ?? 0, end = input.selectionEnd ?? start;
    const text = await reference.invokeMethodAsync("TransformPaste", event.clipboardData.getData("text/plain")).catch(error => { if (!state.disposed) throw error; });
    if (state.disposed || text == null || input.value !== previous || input.selectionStart !== start || input.selectionEnd !== end || input.disabled || input.readOnly) return;
    const next = (previous.slice(0, start) + text + previous.slice(end)).slice(0, input.maxLength);
    if (!state.accepts(next)) return;
    input.value = next;
    input.setSelectionRange(Math.min(next.length, input.maxLength - 1), next.length);
    input.dispatchEvent(new InputEvent("input", { bubbles: true, inputType: "insertFromPaste", data: text }));
    state.notify();
  };
  input.addEventListener("focus", state.focus);
  input.addEventListener("blur", state.notify);
  root.addEventListener("click", state.pointer);
  input.addEventListener("input", state.notify);
  input.addEventListener("beforeinput", state.beforeInput);
  input.addEventListener("input", state.inputting, true);
  input.addEventListener("paste", state.paste);
  input.addEventListener("select", state.notify);
  input.addEventListener("keydown", state.keydown);
  document.addEventListener("selectionchange", state.selection);
  otpStates.set(root, state);
  if (connectionId) otpConnections.set(connectionId, state);
  syncOtp(root);
  input.dataset.rbiOtpReady = "true";
}

export function syncOtp(root) {
  const state = otpStates.get(root);
  if (!state) return;
  state.slots = [...root.querySelectorAll("[data-rbi-otp-slot]")];
  state.accepted = state.input.value;
  state.notify();
}

export function disconnectOtp(root, connectionId) {
  const state = otpStates.get(root) ?? otpConnections.get(connectionId);
  if (!state) return;
  root = state.root;
  state.disposed = true;
  const { input } = state;
  delete input.dataset.rbiOtpReady;
  input.removeEventListener("focus", state.focus);
  input.removeEventListener("blur", state.notify);
  root.removeEventListener("click", state.pointer);
  input.removeEventListener("input", state.notify);
  input.removeEventListener("beforeinput", state.beforeInput);
  input.removeEventListener("input", state.inputting, true);
  input.removeEventListener("paste", state.paste);
  input.removeEventListener("select", state.notify);
  input.removeEventListener("keydown", state.keydown);
  document.removeEventListener("selectionchange", state.selection);
  delete input.dataset.rbiOtpReady;
  otpStates.delete(root);
  otpConnections.delete(state.connectionId);
}

export function activeConnectionCount() { return otpConnections.size; }
