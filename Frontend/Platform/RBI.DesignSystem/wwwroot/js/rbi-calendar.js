const calendars = new WeakMap();
const connections = new Map();
const navigationKeys = new Set(["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown", "Home", "End", "PageUp", "PageDown"]);

export function connect(root, reference, connectionId) {
  if (calendars.has(root)) return;
  const state = { disposed: false, root, connectionId };
  state.keydown = event => {
    const day = event.target.closest?.("[data-rbi-calendar-day]");
    if (!day || !root.contains(day) || day.disabled || !navigationKeys.has(event.key)) return;
    event.preventDefault();
    event.stopPropagation();
    void reference.invokeMethodAsync("NativeCalendarKey", day.dataset.rbiCalendarDay, event.key, event.shiftKey, getComputedStyle(root).direction === "rtl")
      .catch(error => { if (!state.disposed) throw error; });
  };
  root.addEventListener("keydown", state.keydown);
  calendars.set(root, state);
  if (connectionId) connections.set(connectionId, state);
  root.dataset.rbiCalendarReady = "true";
}

export function focusDay(root, date) {
  const state = calendars.get(root);
  if (!state || state.disposed) return;
  const day = [...root.querySelectorAll("[data-rbi-calendar-day]")].find(button => button.dataset.rbiCalendarDay === date && !button.disabled && button.tabIndex === 0);
  if (day) day.focus({ preventScroll: true });
}

export function disconnect(root, connectionId) {
  const state = calendars.get(root) ?? connections.get(connectionId);
  if (!state) return;
  root = state.root;
  state.disposed = true;
  root.removeEventListener("keydown", state.keydown);
  delete root.dataset.rbiCalendarReady;
  calendars.delete(root);
  connections.delete(state.connectionId);
}
export function activeConnectionCount() { return connections.size; }
