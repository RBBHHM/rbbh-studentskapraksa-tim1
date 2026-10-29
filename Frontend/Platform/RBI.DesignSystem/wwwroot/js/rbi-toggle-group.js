// The single-selection API uses ARIA radios, not independent pressed buttons.
// Blazor owns checked state; DOM interop only supplies focus and key defaults.
const connections = new WeakMap();
const connectionIds = new Map();
const items = root => [...root.querySelectorAll(':scope > button[role="radio"]')].filter(item => !item.disabled);
function sync(root) {
  const enabled = items(root);
  const entry = enabled.find(item => item.getAttribute('aria-checked') === 'true') ?? enabled[0];
  for (const item of root.querySelectorAll(':scope > button[role="radio"]')) item.tabIndex = item === entry ? 0 : -1;
}
export function connect(root, connectionId) {
  if (!(root instanceof HTMLElement) || !root.isConnected) return;
  if (!connections.has(root)) {
    const keydown = event => {
      const enabled = items(root), index = enabled.indexOf(event.target);
      if (index < 0) return;
      const rtl = getComputedStyle(root).direction === 'rtl';
      let next;
      switch (event.key) {
        case 'ArrowRight': next = index + (rtl ? -1 : 1); break;
        case 'ArrowLeft': next = index + (rtl ? 1 : -1); break;
        case 'ArrowDown': next = index + 1; break;
        case 'ArrowUp': next = index - 1; break;
        case 'Home': next = 0; break;
        case 'End': next = enabled.length - 1; break;
        default: return;
      }
      event.preventDefault();
      const target = enabled[(next + enabled.length) % enabled.length];
      target.focus();
      target.click();
    };
    root.addEventListener('keydown', keydown);
    // Descendant renders and rejected proposals must retain the accepted tab stop.
    const observer = new MutationObserver(() => sync(root));
    observer.observe(root, { subtree: true, childList: true, attributes: true, attributeFilter: ['aria-checked', 'disabled'] });
    const state = { root, keydown, observer, connectionId };
    connections.set(root, state);
    if (connectionId) connectionIds.set(connectionId, state);
  }
  sync(root);
}
export function disconnect(root, connectionId) {
  const state = connections.get(root) ?? connectionIds.get(connectionId);
  if (!state) return;
  root = state.root;
  root.removeEventListener('keydown', state.keydown);
  state.observer.disconnect();
  connections.delete(root);
  connectionIds.delete(state.connectionId);
}
export function activeConnectionCount() { return connectionIds.size; }
