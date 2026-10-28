const states = new WeakMap();
const connections = new Map();
let lockCount = 0;
let previousOverflow = "";
let previousPaddingRight = "";

function lockDocument() {
  if (lockCount++ > 0) return;
  previousOverflow = document.body.style.overflow;
  previousPaddingRight = document.body.style.paddingRight;
  const scrollbarWidth = Math.max(0, window.innerWidth - document.documentElement.clientWidth);
  const currentPadding = Number.parseFloat(getComputedStyle(document.body).paddingRight) || 0;
  document.body.style.overflow = "hidden";
  if (scrollbarWidth > 0) document.body.style.paddingRight = `${currentPadding + scrollbarWidth}px`;
}

function unlockDocument() {
  if (lockCount === 0 || --lockCount > 0) return;
  document.body.style.overflow = previousOverflow;
  document.body.style.paddingRight = previousPaddingRight;
}

function isBackdropPointer(dialog, event) {
  if (event.target !== dialog) return false;
  const bounds = dialog.getBoundingClientRect();
  return (
    event.clientX < bounds.left ||
    event.clientX > bounds.right ||
    event.clientY < bounds.top ||
    event.clientY > bounds.bottom
  );
}

function focusInitialElement(dialog, preferredSelector) {
  const target =
    (preferredSelector && dialog.querySelector(preferredSelector)) ||
    dialog.querySelector("[autofocus]") ||
    focusableElements(dialog)[0] ||
    dialog;
  target.focus({ preventScroll: true });
}

function focusableElements(dialog) {
  const selector = [
    "button:not([disabled])",
    "[href]",
    "input:not([disabled])",
    "select:not([disabled])",
    "textarea:not([disabled])",
    "[tabindex]:not([tabindex='-1'])",
  ].join(",");
  return [...dialog.querySelectorAll(selector)].filter(
    (element) => element.getClientRects().length > 0,
  );
}

function scheduleFocus(dialog, state, callback) {
  cancelAnimationFrame(state.focusFrame);
  state.focusFrame = requestAnimationFrame(() => {
    state.focusFrame = 0;
    if (!state.disposed && states.get(dialog) === state) callback();
  });
}

function requestClose(state, reason) {
  if (state.disposed || state.closeRequested) return;
  state.closeRequested = true;
  void state.dotNetReference.invokeMethodAsync("RequestClose", reason).catch(error => {
    if (!state.disposed) throw error;
  });
}

function finishClose(dialog, state) {
  dialog.close();
  delete dialog.dataset.rbiDrawerClosing;
  dialog.style.removeProperty('--rbi-drawer-close-start');
  state.closing = false;
  if (state.locked) { unlockDocument(); state.locked = false; }
  const opener = state.opener;
  state.opener = null;
  state.closeRequested = false;
  scheduleFocus(dialog, state, () => {
    if (!dialog.open && opener?.isConnected) opener.focus();
  });
}

function beginClose(dialog, state) {
  if (state.closing) return;
  if (!dialog.classList.contains('rbi-drawer')) { finishClose(dialog, state); return; }
  state.closing = true;
  const version = ++state.closeVersion;
  cancelAnimationFrame(state.focusFrame);
  dialog.style.removeProperty('translate');
  dialog.dataset.rbiDrawerClosing = 'true';
  // Resolve on actual CSS completion (empty for reduced motion), not a timer.
  const animations = dialog.getAnimations().filter(animation => animation instanceof CSSAnimation && animation.animationName === 'rbi-drawer-exit');
  Promise.all(animations.map(animation => animation.finished.catch(error => {
    if (error.name !== 'AbortError') throw error;
  }))).then(() => {
    if (!state.disposed && state.closing && state.closeVersion === version) finishClose(dialog, state);
  });
}

export function connect(dialog, dotNetReference, dismissOnBackdrop, dismissOnEscape, connectionId) {
  if (states.has(dialog)) return;
  const state = {
    dialog,
    connectionId,
    dotNetReference,
    dismissOnBackdrop,
    dismissOnEscape,
    opener: null,
    locked: false,
    pointerStartedOnBackdrop: false,
    closeRequested: false,
    disposed: false,
    focusFrame: 0,
    closing: false,
    closeVersion: 0,
  };

  state.cancel = (event) => {
    event.preventDefault();
    if (state.dismissOnEscape) requestClose(state, "escape");
  };
  state.pointerdown = (event) => {
    state.pointerStartedOnBackdrop = isBackdropPointer(dialog, event);
  };
  state.click = (event) => {
    const onBackdrop = state.pointerStartedOnBackdrop && isBackdropPointer(dialog, event);
    state.pointerStartedOnBackdrop = false;
    if (onBackdrop && state.dismissOnBackdrop) requestClose(state, "backdrop");
  };
  state.keydown = (event) => {
    if (event.key !== "Tab") return;
    const focusable = focusableElements(dialog);
    if (focusable.length === 0) {
      event.preventDefault();
      dialog.focus();
      return;
    }
    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (!dialog.contains(document.activeElement)) {
      event.preventDefault();
      (event.shiftKey ? last : first).focus();
    } else if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  };

  dialog.addEventListener("cancel", state.cancel);
  dialog.addEventListener("pointerdown", state.pointerdown);
  dialog.addEventListener("click", state.click);
  dialog.addEventListener("keydown", state.keydown);
  states.set(dialog, state);
  if (connectionId) connections.set(connectionId, state);
}

export function sync(dialog, open, initialFocusSelector, dismissOnBackdrop, dismissOnEscape) {
  const state = states.get(dialog);
  if (!state) return;
  state.dismissOnBackdrop = dismissOnBackdrop;
  state.dismissOnEscape = dismissOnEscape;

  if (open) {
    state.closeVersion++;
    state.closing = false;
    delete dialog.dataset.rbiDrawerClosing;
    dialog.style.removeProperty('--rbi-drawer-close-start');
  }

  // A controlled owner may reject dismissal and render the same open value again.
  if (open && dialog.open) state.closeRequested = false;

  if (open && !dialog.open) {
    state.opener = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    dialog.showModal();
    state.closeRequested = false;
    lockDocument();
    state.locked = true;
    scheduleFocus(dialog, state, () => { if (dialog.open) focusInitialElement(dialog, initialFocusSelector); });
    return;
  }

  if (!open && dialog.open) {
    beginClose(dialog, state);
  }
}

export function disconnect(dialog, connectionId) {
  const state = states.get(dialog) ?? connections.get(connectionId);
  if (!state) return;
  dialog = state.dialog;
  state.disposed = true;
  state.closeVersion++;
  cancelAnimationFrame(state.focusFrame);
  dialog.removeEventListener("cancel", state.cancel);
  dialog.removeEventListener("pointerdown", state.pointerdown);
  dialog.removeEventListener("click", state.click);
  dialog.removeEventListener("keydown", state.keydown);
  if (dialog.open) dialog.close();
  delete dialog.dataset.rbiDrawerClosing;
  dialog.style.removeProperty('--rbi-drawer-close-start');
  if (state.locked) { unlockDocument(); state.locked = false; }
  states.delete(dialog);
  connections.delete(state.connectionId);
}

export function activeConnectionCount() { return connections.size; }
