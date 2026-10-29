const states = new WeakMap();
const menubars = new WeakMap();
const connections = new Map();
let anchorSequence = 0;

// Disposal may arrive after Blazor has removed the captured DOM element.
function rememberConnection(root, state, connectionId) {
  states.set(root, state);
  if (connectionId) { state.connectionId = connectionId; connections.set(connectionId, state); }
}

const viewportPadding = 12;
const anchorOffset = 8;

function surfaceFor(root) {
  return root.querySelector(":scope > [data-rbi-anchored-surface]");
}

function triggerFor(root, surface) {
  const triggers = [...root.children].filter((element) => element !== surface);
  return triggers.length === 1 ? triggers[0] : null;
}

function supportsPopover(surface) {
  return typeof surface.showPopover === "function" && typeof surface.hidePopover === "function";
}

function supportsAnchorPositioning() {
  return (
    typeof CSS !== "undefined" &&
    CSS.supports("anchor-name: --rbi-anchor") &&
    CSS.supports("position-area: bottom") &&
    CSS.supports("position-try-fallbacks: flip-block")
  );
}

function fallbackOrder(preferred) {
  switch (preferred) {
    case "top":
      return ["top", "bottom", "right", "left"];
    case "left":
      return ["left", "right", "bottom", "top"];
    case "right":
      return ["right", "left", "bottom", "top"];
    default:
      return ["bottom", "top", "right", "left"];
  }
}

function placementOrder(state) {
  // Nested commands stay on the horizontal axis, including collision fallback.
  // A vertical fallback detaches the child surface from its parent row.
  return state.type === "SubMenu"
    ? [state.preferred, state.preferred === "right" ? "left" : "right"]
    : fallbackOrder(state.preferred);
}

function configureAnchor(state) {
  state.preferred = state.preferred.toLowerCase();
  state.surface.dataset.rbiPreferredPlacement = state.preferred;
  if (!state.anchorSupported) return;

  state.positionSource.style.setProperty("anchor-name", state.anchorName);
  state.surface.style.setProperty("position-anchor", state.anchorName);
  const anchoredStart = state.align === "start";
  const horizontal = ["left", "right"].includes(state.preferred);
  state.surface.style.setProperty("position-area", anchoredStart ? horizontal ? `center ${state.preferred}` : `${state.preferred} center` : state.preferred);
  if (state.alignOffset) state.surface.style.setProperty("translate", horizontal ? `0 ${state.alignOffset}px` : `${state.alignOffset}px 0`);
  state.surface.style.setProperty(
    "position-try-fallbacks",
    anchoredStart
      ? placementOrder(state).slice(1)
        .map(side => ["left", "right"].includes(side) ? `center ${side}` : `${side} center`).join(", ")
      : fallbackOrder(state.preferred).slice(1).join(", "),
  );
  state.surface.dataset.rbiPositioning = "native-anchor";
}

function candidate(rect, width, height, placement, offset = anchorOffset, align = "center") {
  switch (placement) {
    case "top":
      return {
        placement,
        left: align === "start" ? rect.left : rect.left + (rect.width - width) / 2,
        top: rect.top - height - offset,
      };
    case "left":
      return {
        placement,
        left: rect.left - width - offset,
        top: align === "start" ? rect.top : rect.top + (rect.height - height) / 2,
      };
    case "right":
      return {
        placement,
        left: rect.right + offset,
        top: align === "start" ? rect.top : rect.top + (rect.height - height) / 2,
      };
    default:
      return {
        placement,
        left: align === "start" ? rect.left : rect.left + (rect.width - width) / 2,
        top: rect.bottom + offset,
      };
  }
}

function overflowScore(position, width, height, padding = viewportPadding) {
  return (
    Math.max(0, padding - position.left) +
    Math.max(0, padding - position.top) +
    Math.max(0, position.left + width + padding - window.innerWidth) +
    Math.max(0, position.top + height + padding - window.innerHeight)
  );
}

function positionFallback(state) {
  if (!state.open || state.anchorSupported) return;
  const rect = state.positionSource.getBoundingClientRect();
  const width = state.surface.offsetWidth;
  const height = state.surface.offsetHeight;
  // Radix collisionPadding defaults to 0: HoverCard and dropdown menus may sit flush with the viewport edge; other surfaces keep the 12px inset.
  const padding = state.viewportPadding ?? viewportPadding;
  const candidates = placementOrder(state).map((placement) => {
    const position = candidate(rect, width, height, placement, state.offset, state.align);
    // Only HoverCard exposes end alignment. Existing menu/tooltip positioning is unchanged.
    if ((state.type === "HoverCard" || state.type === "Menu") && state.hoverAlign === "end") {
      if (["top", "bottom"].includes(placement)) position.left = rect.right - width;
      else position.top = rect.bottom - height;
    }
    return position;
  });
  const selected =
    candidates.find((position) => overflowScore(position, width, height, padding) === 0) ??
    candidates.reduce((best, position) =>
      overflowScore(position, width, height, padding) < overflowScore(best, width, height, padding) ? position : best,
    );
  if (state.alignOffset) {
    if (["top", "bottom"].includes(selected.placement)) selected.left += state.alignOffset;
    else selected.top += state.alignOffset;
  }

  const maxLeft = Math.max(padding, window.innerWidth - width - padding);
  const maxTop = Math.max(padding, window.innerHeight - height - padding);
  state.surface.style.left = `${Math.min(Math.max(selected.left, padding), maxLeft)}px`;
  state.surface.style.top = `${Math.min(Math.max(selected.top, padding), maxTop)}px`;
  state.surface.dataset.rbiResolvedPlacement = selected.placement;
  state.surface.dataset.rbiPositioning = "fallback";
}

function requestPosition(state) {
  if (state.frame) return;
  state.frame = requestAnimationFrame(() => {
    state.frame = 0;
    positionFallback(state);
  });
}

function startFallbackListeners(state) {
  if (state.anchorSupported || state.listening) return;
  window.addEventListener("resize", state.reposition);
  document.addEventListener("scroll", state.reposition, true);
  state.listening = true;
}

function stopFallbackListeners(state) {
  if (!state.listening) return;
  window.removeEventListener("resize", state.reposition);
  document.removeEventListener("scroll", state.reposition, true);
  state.listening = false;
  if (state.frame) cancelAnimationFrame(state.frame);
  state.frame = 0;
}

function startPopoverListeners(state) {
  if (!["Popover", "Menu", "HoverCard"].includes(state.type) || state.interactionListening) return;
  document.addEventListener("keydown", state.keydown, true);
  document.addEventListener("pointerdown", state.outsidePointer, true);
  if (state.type === "Popover") document.addEventListener("focusin", state.outsideFocus);
  if (!state.nativePopover) {
    document.addEventListener("keydown", state.fallbackEscape);
  }
  state.interactionListening = true;
}

function stopPopoverListeners(state) {
  if (!["Popover", "Menu", "HoverCard"].includes(state.type) || !state.interactionListening) return;
  document.removeEventListener("keydown", state.keydown, true);
  document.removeEventListener("pointerdown", state.outsidePointer, true);
  if (state.type === "Popover") document.removeEventListener("focusin", state.outsideFocus);
  if (!state.nativePopover) {
    document.removeEventListener("keydown", state.fallbackEscape);
  }
  state.interactionListening = false;
}

function setVisualOpen(state, open) {
  if (state.open === open) return;
  state.open = open;
  state.surface.dataset.rbiOpen = String(open);

  if (open) {
    if (state.nativePopover) {
      if (!state.surface.matches(":popover-open")) state.surface.showPopover();
    } else {
      state.surface.dataset.rbiFallbackOpen = "true";
    }
    startFallbackListeners(state);
    startPopoverListeners(state);
    requestPosition(state);
    attachWaitingSubmenus(state);
    return;
  }

  closeSubmenus(state);
  window.clearTimeout(state.typeaheadTimer);
  state.typeahead = "";
  state.restoreFocusOnClose =
    (state.type === "Popover" || state.type === "Menu") &&
    state.surface.contains(document.activeElement);
  if (state.nativePopover && state.surface.matches(":popover-open")) state.surface.hidePopover();
  delete state.surface.dataset.rbiFallbackOpen;
  stopFallbackListeners(state);
  stopPopoverListeners(state);
  state.surface.style.removeProperty("left");
  state.surface.style.removeProperty("top");
}

function rememberAttribute(element, name) {
  return element.hasAttribute(name) ? element.getAttribute(name) : null;
}

function restoreAttribute(element, name, value) {
  if (value === null) element.removeAttribute(name);
  else element.setAttribute(name, value);
}

function connectBase(root, type, preferred) {
  const surface = surfaceFor(root);
  const trigger = surface && (type === "Navigation" ? root.querySelector(':scope > [data-rbi-navigation-list]') : triggerFor(root, surface));
  if (!(surface instanceof HTMLElement) || !(trigger instanceof HTMLElement)) {
    throw new Error(`Rbi${type} requires exactly one HTML element as its trigger.`);
  }

  const state = {
    type,
    root,
    surface,
    trigger,
    positionSource: trigger,
    preferred,
    anchorName: `--rbi-anchor-${++anchorSequence}`,
    anchorSupported: supportsAnchorPositioning(),
    nativePopover: supportsPopover(surface),
    open: false,
    frame: 0,
    listening: false,
    interactionListening: false,
    original: {},
  };
  state.reposition = () => requestPosition(state);
  configureAnchor(state);
  return state;
}

function attachSubmenu(state) {
  const surface = state.root.parentElement?.closest('[role="menu"]');
  const parent = surface && states.get(surface.parentElement);
  if (!parent || state.parent === parent) return parent;
  state.parent?.children?.delete(state);
  state.parent = parent;
  (parent.children ??= new Set()).add(state);
  return parent;
}

function attachWaitingSubmenus(parent) {
  for (const host of parent.surface.querySelectorAll("[data-rbi-menu-sub-host]")) {
    const child = states.get(host);
    if (child && attachSubmenu(child) === parent) child.applyOpen(child.requestedOpen, false);
  }
}

function closeSubmenus(parent) {
  for (const child of parent.children ?? []) child.changeOpen(false, false);
}

function appendTokenAttribute(element, name, token) {
  const tokens = new Set((element.getAttribute(name) ?? "").split(/\s+/).filter(Boolean));
  tokens.add(token);
  element.setAttribute(name, [...tokens].join(" "));
}

// ---- Tooltip coordination ----------------------------------------------------------------------------------------------
// The Legacy source wraps every tooltip in one TooltipProvider: a single skip-delay window (300 ms) after a tooltip closes lets the next one open
// immediately, only one tooltip is open at a time, and the pointer-grace polygon between a trigger and its content blocks other triggers from
// opening while the pointer is still in transit. Native tooltips share this state at module level.
const tooltipSkipDelay = 300;
const tooltipGroup = { skipTimer: 0, openDelayed: true, transit: 0, current: null };

function tooltipOpened(state) {
  clearTimeout(tooltipGroup.skipTimer);
  tooltipGroup.openDelayed = false;
  const previous = tooltipGroup.current;
  tooltipGroup.current = state;
  if (previous && previous !== state) previous.close();
}

function tooltipClosed(state) {
  if (tooltipGroup.current === state) tooltipGroup.current = null;
  clearTimeout(tooltipGroup.skipTimer);
  tooltipGroup.skipTimer = window.setTimeout(() => { tooltipGroup.openDelayed = true; }, tooltipSkipDelay);
}

function hullOf(points) {
  const sorted = points.map(point => ({ x: point.x, y: point.y })).sort((a, b) => a.x - b.x || a.y - b.y);
  if (sorted.length <= 1) return sorted;
  const cross = (o, a, b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
  const build = list => {
    const hull = [];
    for (const point of list) {
      while (hull.length >= 2 && cross(hull[hull.length - 2], hull[hull.length - 1], point) <= 0) hull.pop();
      hull.push(point);
    }
    hull.pop();
    return hull;
  };
  return [...build(sorted), ...build([...sorted].reverse())];
}

function insidePolygon(point, polygon) {
  let inside = false;
  for (let index = 0, previous = polygon.length - 1; index < polygon.length; previous = index++) {
    const a = polygon[index], b = polygon[previous];
    if (a.y > point.y !== b.y > point.y && point.x < ((b.x - a.x) * (point.y - a.y)) / (b.y - a.y) + a.x) inside = !inside;
  }
  return inside;
}

function graceAreaBetween(exit, leftRect, targetRect) {
  const distances = [
    [Math.abs(leftRect.top - exit.y), "top"], [Math.abs(leftRect.bottom - exit.y), "bottom"],
    [Math.abs(leftRect.right - exit.x), "right"], [Math.abs(leftRect.left - exit.x), "left"],
  ].sort((a, b) => a[0] - b[0]);
  const padding = 5;
  const padded = {
    top: [{ x: exit.x - padding, y: exit.y + padding }, { x: exit.x + padding, y: exit.y + padding }],
    bottom: [{ x: exit.x - padding, y: exit.y - padding }, { x: exit.x + padding, y: exit.y - padding }],
    left: [{ x: exit.x + padding, y: exit.y - padding }, { x: exit.x + padding, y: exit.y + padding }],
    right: [{ x: exit.x - padding, y: exit.y - padding }, { x: exit.x - padding, y: exit.y + padding }],
  }[distances[0][1]];
  const corners = [
    { x: targetRect.left, y: targetRect.top }, { x: targetRect.right, y: targetRect.top },
    { x: targetRect.right, y: targetRect.bottom }, { x: targetRect.left, y: targetRect.bottom },
  ];
  return hullOf([...padded, ...corners]);
}

export function connectTooltip(root, preferred, delay, enabled = true, connectionId) {
  if (states.has(root)) return syncTooltip(root, preferred, delay, enabled);
  const state = connectBase(root, "Tooltip", preferred);
  state.offset = 4;
  state.delay = delay;
  state.enabled = enabled;
  state.focusInside = false;
  state.moveOpened = false;
  state.pointerDown = false;
  state.openTimer = 0;
  state.grace = null;
  state.original.describedBy = rememberAttribute(state.trigger, "aria-describedby");
  if (enabled) appendTokenAttribute(state.trigger, "aria-describedby", state.surface.id);

  state.endGrace = () => {
    if (!state.grace) return;
    document.removeEventListener("pointermove", state.trackGrace, true);
    state.grace = null;
    tooltipGroup.transit = Math.max(0, tooltipGroup.transit - 1);
  };
  // Opening is immediate inside the skip-delay window and after a keyboard focus, delayed otherwise.
  state.openTooltip = immediate => {
    clearTimeout(state.openTimer);
    state.openTimer = 0;
    if (!state.enabled || state.open) return;
    const open = () => {
      state.openTimer = 0;
      if (!state.enabled) return;
      setVisualOpen(state, true);
      tooltipOpened(state);
    };
    if (immediate || state.delay === 0 || !tooltipGroup.openDelayed) open();
    else state.openTimer = window.setTimeout(open, state.delay);
  };
  state.close = () => {
    clearTimeout(state.openTimer);
    state.openTimer = 0;
    state.endGrace();
    if (!state.open) return;
    setVisualOpen(state, false);
    tooltipClosed(state);
  };
  state.trackGrace = event => {
    const grace = state.grace;
    if (!grace) return;
    if (state.root.contains(event.target)) { state.endGrace(); return; }
    if (!insidePolygon({ x: event.clientX, y: event.clientY }, grace)) state.close();
  };
  state.startGrace = (event, leftElement, targetElement) => {
    state.endGrace();
    if (!state.open) return;
    state.grace = graceAreaBetween({ x: event.clientX, y: event.clientY }, leftElement.getBoundingClientRect(), targetElement.getBoundingClientRect());
    tooltipGroup.transit++;
    document.addEventListener("pointermove", state.trackGrace, true);
  };
  state.pointermove = event => {
    if (event.pointerType === "touch") return;
    if (!state.moveOpened && !tooltipGroup.transit) {
      state.moveOpened = true;
      state.openTooltip(false);
    }
  };
  state.pointerleave = event => {
    if (event.pointerType === "touch") return;
    state.moveOpened = false;
    clearTimeout(state.openTimer);
    state.openTimer = 0;
    state.startGrace(event, state.trigger, state.surface);
  };
  state.surfaceleave = event => {
    if (event.pointerType === "touch") return;
    state.startGrace(event, state.surface, state.trigger);
  };
  state.pointerup = () => { state.pointerDown = false; };
  state.pointerdown = () => {
    state.close();
    state.pointerDown = true;
    document.addEventListener("pointerup", state.pointerup, { once: true });
  };
  state.click = () => state.close();
  state.focusin = () => {
    state.focusInside = true;
    if (!state.pointerDown) state.openTooltip(true);
  };
  state.focusout = event => {
    if (state.trigger.contains(event.relatedTarget)) return;
    state.focusInside = false;
    state.close();
  };
  state.keydown = event => {
    if (event.key !== "Escape" || !state.open) return;
    state.close();
  };
  // Scrolling any ancestor of the trigger (or the page) dismisses the tooltip, as the source does.
  state.scroll = event => {
    if (state.open && event.target instanceof Node && event.target.contains(state.trigger)) state.close();
  };

  state.trigger.addEventListener("pointermove", state.pointermove);
  state.trigger.addEventListener("pointerleave", state.pointerleave);
  state.trigger.addEventListener("pointerdown", state.pointerdown);
  state.trigger.addEventListener("click", state.click);
  state.trigger.addEventListener("focusin", state.focusin);
  state.trigger.addEventListener("focusout", state.focusout);
  state.trigger.addEventListener("keydown", state.keydown);
  state.surface.addEventListener("pointerleave", state.surfaceleave);
  window.addEventListener("scroll", state.scroll, true);
  rememberConnection(root, state, connectionId);
}

export function syncTooltip(root, preferred, delay, enabled = true) {
  const state = states.get(root);
  if (!state) return;
  const wasEnabled = state.enabled;
  state.preferred = preferred;
  state.delay = delay;
  state.enabled = enabled;
  if (!enabled) {
    state.close();
    const tokens = (state.trigger.getAttribute('aria-describedby') ?? '').split(/\s+/).filter(token => token && token !== state.surface.id);
    if (tokens.length) state.trigger.setAttribute('aria-describedby', tokens.join(' ')); else state.trigger.removeAttribute('aria-describedby');
  } else appendTokenAttribute(state.trigger, 'aria-describedby', state.surface.id);
  configureAnchor(state);
  requestPosition(state);
  if (enabled && !wasEnabled && state.focusInside) state.openTooltip(true);
}

function configureHoverAlignment(state, align) {
  stopFallbackListeners(state);
  state.hoverAlign = align;
  state.align = align === "center" ? "center" : "start";
  // The existing measured fallback handles non-center alignment, including
  // collision scoring before clamping. Keep the default native anchor path.
  state.anchorSupported = state.hoverAnchorSupported && align === "center";
  if (!state.anchorSupported) {
    state.trigger.style.removeProperty("anchor-name");
    for (const name of ["position-anchor", "position-area", "position-try-fallbacks"]) state.surface.style.removeProperty(name);
    state.surface.dataset.rbiPositioning = "fallback";
  } else {
    state.surface.style.removeProperty("left"); state.surface.style.removeProperty("top");
  }
  configureAnchor(state);
  if (state.open) startFallbackListeners(state);
}

export function connectHoverCard(root, dotNetReference, open, preferred, openDelay, closeDelay, connectionId, align = "center") {
  if (states.has(root)) return syncHoverCard(root, open, preferred, openDelay, closeDelay, align);
  const state = connectBase(root, "HoverCard", preferred);
  state.viewportPadding = 0;
  state.hoverAnchorSupported = state.anchorSupported;
  configureHoverAlignment(state, align);
  state.offset = 4;
  state.dotNetReference = dotNetReference;
  state.notifiedOpen = open;
  state.openDelay = openDelay;
  state.closeDelay = closeDelay;
  state.pointerInside = false;
  state.focusInside = false;
  state.touchFocusSuppressedUntil = 0;
  state.original.details = rememberAttribute(state.trigger, "aria-details");
  appendTokenAttribute(state.trigger, "aria-details", state.surface.id);
  state.clearTimers = () => {
    clearTimeout(state.openTimer);
    clearTimeout(state.closeTimer);
    state.openTimer = state.closeTimer = 0;
  };
  state.changeOpen = (value) => {
    state.clearTimers();
    setVisualOpen(state, value);
    if (state.notifiedOpen === value) return;
    state.notifiedOpen = value;
    void state.dotNetReference.invokeMethodAsync("NativeOpenChanged", value).catch(error => {
      if (!state.disconnected) throw error;
    });
  };
  state.enter = () => {
    state.clearTimers();
    if (!state.open) state.openTimer = window.setTimeout(() => state.changeOpen(true), state.openDelay);
  };
  // Text selected inside the card keeps it open while the pointer is away, so a selection drag that leaves the card can still be copied.
  const selectionInside = () => {
    const selection = window.getSelection();
    return !!selection && !selection.isCollapsed && !!selection.anchorNode && state.surface.contains(selection.anchorNode);
  };
  state.leave = () => {
    state.clearTimers();
    if (!state.pointerInside && !state.focusInside)
      state.closeTimer = window.setTimeout(() => { if (!selectionInside()) state.changeOpen(false); }, state.closeDelay);
  };
  state.pointerenter = event => {
    if (event.pointerType === "touch") return;
    state.pointerInside = true;
    state.enter();
  };
  state.pointerleave = event => {
    if (event.pointerType === "touch") return;
    state.pointerInside = root.contains(event.relatedTarget);
    if (!state.pointerInside) state.leave();
  };
  state.pointerdown = event => {
    if (event.pointerType === "touch") state.touchFocusSuppressedUntil = performance.now() + 800;
  };
  state.focusin = () => {
    if (performance.now() < state.touchFocusSuppressedUntil) return;
    state.focusInside = true;
    if (state.suppressNextFocus) {
      state.suppressNextFocus = false;
      return;
    }
    state.enter();
  };
  state.focusout = event => {
    state.focusInside = root.contains(event.relatedTarget);
    if (!state.focusInside) {
      state.suppressNextFocus = false;
      state.leave();
    }
  };
  state.keydown = event => {
    if (event.key !== "Escape" || !state.open) return;
    event.preventDefault();
    const restore = state.surface.contains(document.activeElement);
    state.pointerInside = state.focusInside = false;
    state.suppressNextFocus = restore;
    state.changeOpen(false);
    if (restore) state.trigger.focus({ preventScroll: true });
  };
  state.outsidePointer = event => {
    if (root.contains(event.target)) return;
    state.pointerInside = state.focusInside = false;
    state.changeOpen(false);
  };
  state.fallbackEscape = state.keydown;
  for (const element of [state.trigger, state.surface]) {
    element.addEventListener("pointerenter", state.pointerenter);
    element.addEventListener("pointerleave", state.pointerleave);
    element.addEventListener("focusin", state.focusin);
    element.addEventListener("focusout", state.focusout);
  }
  state.trigger.addEventListener("pointerdown", state.pointerdown);
  rememberConnection(root, state, connectionId);
  setVisualOpen(state, open);
}

export function syncHoverCard(root, open, preferred, openDelay, closeDelay, align = "center") {
  const state = states.get(root);
  if (!state) return;
  state.preferred = preferred;
  state.openDelay = openDelay;
  state.closeDelay = closeDelay;
  state.notifiedOpen = open;
  configureHoverAlignment(state, align);
  if (state.open !== open) {
    state.clearTimers();
    setVisualOpen(state, open);
  }
  requestPosition(state);
}

export function connectPopover(root, dotNetReference, open, preferred, connectionId) {
  if (states.has(root)) return syncPopover(root, open, preferred);
  const state = connectBase(root, "Popover", preferred);
  state.offset = 4;
  state.focusFrame = 0;
  state.dotNetReference = dotNetReference;
  state.notifiedOpen = open;
  state.escapePressed = false;
  state.restoreFocusOnClose = false;
  state.original.expanded = rememberAttribute(state.trigger, "aria-expanded");
  state.original.controls = rememberAttribute(state.trigger, "aria-controls");
  state.original.hasPopup = rememberAttribute(state.trigger, "aria-haspopup");
  state.original.surfaceTabIndex = rememberAttribute(state.surface, "tabindex");
  state.trigger.setAttribute("aria-controls", state.surface.id);
  state.trigger.setAttribute("aria-haspopup", "dialog");
  state.trigger.setAttribute("aria-expanded", String(open));

  state.notify = (value) => {
    if (state.notifiedOpen === value) return;
    state.notifiedOpen = value;
    void state.dotNetReference.invokeMethodAsync("NativeOpenChanged", value).catch(error => {
      if (!state.disconnected) throw error;
    });
  };
  state.click = () => {
    setVisualOpen(state, !state.open);
    state.trigger.setAttribute("aria-expanded", String(state.open));
    if (!state.nativePopover && (state.open || state.restoreFocusOnClose)) state.queueFocus(state.open);
    state.notify(state.open);
  };
  // Popover alone owns a nonmodal, looping focus scope. Menus and HoverCard
  // keep their independent focus models and event handlers.
  const focusable = () => [...state.surface.querySelectorAll('button, input, select, textarea, a[href], [tabindex]')]
    .filter(element => !element.matches(':disabled, [hidden], input[type="hidden"]') && element.tabIndex >= 0 && element.getClientRects().length);
  state.queueFocus = (opening) => {
    cancelAnimationFrame(state.focusFrame);
    state.focusFrame = requestAnimationFrame(() => {
      state.focusFrame = 0;
      if (state.disconnected || !state.root.isConnected || state.open !== opening) return;
      if (opening) {
        if (state.surface.contains(document.activeElement)) return;
        const target = focusable()[0] ?? state.surface;
        if (target === state.surface) state.surface.tabIndex = -1;
        target.focus({ preventScroll: true });
      } else state.trigger.focus({ preventScroll: true });
    });
  };
  state.keydown = (event) => {
    if (event.key === "Escape" && state.open) state.escapePressed = true;
    if (event.key !== "Tab" || !state.open || !state.surface.contains(document.activeElement)) return;
    const items = focusable();
    const edge = event.shiftKey ? items[0] : items.at(-1);
    if (!edge || document.activeElement === edge) {
      event.preventDefault();
      (event.shiftKey ? items.at(-1) : items[0])?.focus({ preventScroll: true });
    }
  };
  state.toggle = (event) => {
    const isOpen = event.newState === "open";
    state.open = isOpen;
    state.surface.dataset.rbiOpen = String(isOpen);
    state.trigger.setAttribute("aria-expanded", String(isOpen));
    if (isOpen) {
      startFallbackListeners(state);
      startPopoverListeners(state);
      requestPosition(state);
      state.queueFocus(true);
    } else {
      stopFallbackListeners(state);
      stopPopoverListeners(state);
      if (
        state.escapePressed ||
        state.restoreFocusOnClose ||
        state.surface.contains(document.activeElement)
      ) {
        state.queueFocus(false);
      }
      state.escapePressed = false;
      state.restoreFocusOnClose = false;
    }
    state.notify(isOpen);
  };
  state.outsidePointer = (event) => {
    if (!state.open || state.root.contains(event.target)) return;
    state.restoreFocusOnClose = false;
    if (state.nativePopover) return;
    setVisualOpen(state, false);
    // Closing sees focus still inside during pointerdown. Do not let that
    // recomputed flag steal the outside target's focus on a later .NET sync.
    state.restoreFocusOnClose = false;
    state.trigger.setAttribute("aria-expanded", "false");
    state.notify(false);
  };
  state.outsideFocus = event => {
    if (!state.open || state.root.contains(event.target)) return;
    state.escapePressed = state.restoreFocusOnClose = false;
    setVisualOpen(state, false);
    state.restoreFocusOnClose = false;
    state.trigger.setAttribute("aria-expanded", "false");
    state.notify(false);
  };
  state.fallbackEscape = (event) => {
    if (event.key !== "Escape" || !state.open || state.nativePopover) return;
    event.preventDefault();
    state.escapePressed = true;
    setVisualOpen(state, false);
    state.trigger.setAttribute("aria-expanded", "false");
    state.trigger.focus({ preventScroll: true });
    state.escapePressed = false;
    state.notify(false);
  };
  state.surfaceInteraction = () => {
    state.restoreFocusOnClose = true;
  };

  state.trigger.addEventListener("click", state.click);
  state.surface.addEventListener("pointerdown", state.surfaceInteraction);
  state.surface.addEventListener("focusin", state.surfaceInteraction);
  state.surface.addEventListener("toggle", state.toggle);
  rememberConnection(root, state, connectionId);
  setVisualOpen(state, open);
  if (open && !state.nativePopover) state.queueFocus(true);
  if (open) state.trigger.setAttribute("aria-expanded", "true");
}

export function syncPopover(root, open, preferred) {
  const state = states.get(root);
  if (!state) return;
  state.preferred = preferred;
  state.notifiedOpen = open;
  configureAnchor(state);
  setVisualOpen(state, open);
  if (!state.nativePopover && (open || state.restoreFocusOnClose)) state.queueFocus(open);
  state.trigger.setAttribute("aria-expanded", String(open));
}

function isNaturallyFocusable(element) {
  return element.matches(
    "button, input, select, textarea, a[href], area[href], summary, [contenteditable='true']",
  );
}

function menuItems(state) {
  return Array.from(state.surface.querySelectorAll("[data-rbi-menu-item]"))
    .filter(item => item.closest('[role="menu"]') === state.surface);
}

function enabledMenuItems(state) {
  return menuItems(state).filter((item) => item.getAttribute("aria-disabled") !== "true");
}

function setMenuTabStop(state, item, moveFocus = false) {
  for (const menuItem of menuItems(state)) menuItem.tabIndex = menuItem === item ? 0 : -1;
  if (moveFocus) item?.focus({ preventScroll: true });
}

function pointerMenuFocus(state, event) {
  if (event.target.closest?.('[role="menu"]') !== state.surface) return;
  const item = event.target.closest?.("[data-rbi-menu-item]");
  if (!item || item.getAttribute("aria-disabled") === "true") return;
  for (const child of state.children ?? []) {
    if (!child.open || child.trigger === item) continue;
    if (child.inGrace(event.clientX, event.clientY)) return;
    child.changeOpen(false, false);
  }
  setMenuTabStop(state, item, true);
}

function moveMenuFocus(state, step) {
  const items = enabledMenuItems(state);
  if (!items.length) return;
  const current = items.indexOf(document.activeElement);
  const next =
    current < 0
      ? step > 0
        ? 0
        : items.length - 1
      : state.loop === false || state.surface.dataset.rbiMenuLoop !== "true"
        ? Math.max(0, Math.min(items.length - 1, current + step))
        : (current + step + items.length) % items.length;
  setMenuTabStop(state, items[next], true);
}

function runMenuTypeahead(state, character) {
  state.typeahead = `${state.typeahead ?? ""}${character.toLocaleLowerCase()}`;
  window.clearTimeout(state.typeaheadTimer);
  state.typeaheadTimer = window.setTimeout(() => {
    state.typeahead = "";
  }, 1000);

  const query = [...state.typeahead].every((value) => value === state.typeahead[0])
    ? state.typeahead[0]
    : state.typeahead;
  const items = enabledMenuItems(state);
  if (!items.length) return;
  const current = items.indexOf(document.activeElement);
  for (let offset = 1; offset <= items.length; offset++) {
    const candidate = items[(Math.max(current, -1) + offset) % items.length];
    if (
      (candidate.dataset.rbiMenuText ?? candidate.textContent ?? "")
        .trim()
        .toLocaleLowerCase()
        .startsWith(query)
    ) {
      setMenuTabStop(state, candidate, true);
      return;
    }
  }
}

function createContextAnchor(state) {
  const anchor = document.createElement("span");
  anchor.className = "rbi-context-menu__anchor";
  anchor.setAttribute("aria-hidden", "true");
  state.root.append(anchor);
  state.contextAnchor = anchor;
  state.positionSource = anchor;
  const bounds = state.trigger.getBoundingClientRect();
  moveContextAnchor(state, bounds.left, bounds.bottom);
  configureAnchor(state);
}

function moveContextAnchor(state, x, y) {
  if (!state.contextAnchor) return;
  state.contextAnchor.style.left = `${Math.min(Math.max(x, 0), window.innerWidth)}px`;
  state.contextAnchor.style.top = `${Math.min(Math.max(y, 0), window.innerHeight)}px`;
  requestPosition(state);
}

export function connectMenu(root, dotNetReference, open, preferred, invocationMode, connectionId, align = "center") {
  if (states.has(root)) return syncMenu(root, open, preferred, align);
  const state = connectBase(root, "Menu", preferred);
  state.hoverAnchorSupported = state.anchorSupported;
  // The source dropdown content uses sideOffset 4 and the default collision padding of 0; menubar popups and context menus keep their own geometry.
  if (invocationMode === "dropdown" && !state.surface.hasAttribute("data-rbi-menubar-popup")) { state.offset = 4; state.viewportPadding = 0; }
  // The source context menu opens with its top-left corner just right of the pointer (side right, offset 2) and no collision padding.
  if (invocationMode === "context") { state.offset = 2; state.viewportPadding = 0; }
  if (state.surface.hasAttribute("data-rbi-menubar-popup")) {
    state.align = "start";
    state.alignOffset = -4;
    configureAnchor(state);
  } else if (align !== "center") configureHoverAlignment(state, align);
  state.dotNetReference = dotNetReference;
  state.invocationMode = invocationMode;
  state.notifiedOpen = open;
  state.escapePressed = false;
  state.restoreFocusOnClose = false;
  state.original.expanded = rememberAttribute(state.trigger, "aria-expanded");
  state.original.controls = rememberAttribute(state.trigger, "aria-controls");
  state.original.hasPopup = rememberAttribute(state.trigger, "aria-haspopup");
  state.original.tabIndex = rememberAttribute(state.trigger, "tabindex");
  if (invocationMode === "dropdown") {
    state.trigger.setAttribute("aria-controls", state.surface.id);
    state.trigger.setAttribute("aria-haspopup", "menu");
    state.trigger.setAttribute("aria-expanded", String(open));
  }

  if (invocationMode === "context") {
    createContextAnchor(state);
    if (!isNaturallyFocusable(state.trigger) && !state.trigger.hasAttribute("tabindex"))
      state.trigger.tabIndex = 0;
  }

  state.notify = (value, focusTarget = "none") => {
    if (state.notifiedOpen === value) return;
    state.notifiedOpen = value;
    const bar = menubars.get(root.closest('[role="menubar"]'));
    if (bar) {
      bar.menuChanged(state.trigger, value);
      return;
    }
    void state.dotNetReference.invokeMethodAsync("NativeOpenChanged", value, focusTarget).catch(error => {
      if (!state.disconnected) throw error;
    });
  };
  state.setMenuOpen = (value, focusTarget = "first") => {
    setVisualOpen(state, value);
    if (value && focusTarget === "menu") focusMenuSurface(state);
    else if (value && focusTarget !== "none") {
      const items = enabledMenuItems(state);
      setMenuTabStop(state, focusTarget === "last" ? items.at(-1) : items[0], true);
    }
    if (state.invocationMode === "dropdown")
      state.trigger.setAttribute("aria-expanded", String(state.open));
    state.notify(state.open, focusTarget);
  };
  // A pointer-initiated open focuses the menu itself (the first arrow key then enters the items); a keyboard-initiated click keeps the first-item focus.
  state.click = event => {
    if (state.invocationMode === "dropdown") state.setMenuOpen(!state.open, event.detail > 0 ? "menu" : "first");
  };
  state.triggerKeydown = (event) => {
    // Keyboard invocation of a context menu (ContextMenu key, Shift+F10) arrives as a contextmenu event positioned by the browser, handled below.
    if (state.invocationMode === "context") return;

    if (!["Enter", " ", "ArrowDown", "ArrowUp"].includes(event.key)) return;
    event.preventDefault();
    if (!state.open) state.setMenuOpen(true, event.key === "ArrowUp" ? "last" : "first");
  };
  state.contextmenu = (event) => {
    if (state.invocationMode !== "context") return;
    event.preventDefault();
    state.trigger.focus({ preventScroll: true });
    moveContextAnchor(state, event.clientX, event.clientY);
    // A right-click focuses the menu itself, a keyboard-initiated contextmenu focuses the first item.
    state.setMenuOpen(true, event.button === 2 ? "menu" : "first");
  };
  state.keydown = (event) => {
    if (event.key === "Escape" && state.open && event.target.closest?.('[role="menu"]') === state.surface) state.escapePressed = true;
  };
  state.toggle = (event) => {
    const isOpen = event.newState === "open";
    state.open = isOpen;
    state.surface.dataset.rbiOpen = String(isOpen);
    if (state.invocationMode === "dropdown")
      state.trigger.setAttribute("aria-expanded", String(isOpen));
    if (isOpen) {
      startFallbackListeners(state);
      startPopoverListeners(state);
      requestPosition(state);
    } else {
      closeSubmenus(state);
      stopFallbackListeners(state);
      stopPopoverListeners(state);
      if (
        state.escapePressed ||
        state.restoreFocusOnClose ||
        state.surface.contains(document.activeElement)
      ) {
        requestAnimationFrame(
          () => state.trigger.isConnected && state.trigger.focus({ preventScroll: true }),
        );
      }
      state.escapePressed = false;
      state.restoreFocusOnClose = false;
    }
    state.notify(isOpen, "none");
  };
  state.outsidePointer = (event) => {
    if (!state.open || state.root.contains(event.target)) return;
    // The source returns focus to the trigger after an outside press. A press on something focusable keeps its own focus; a press on empty space
    // hands focus back to the trigger instead of dropping it to the document.
    state.restoreFocusOnClose = !state.surface.hasAttribute("data-rbi-menubar-popup") && !(event.target instanceof Element && event.target.closest('a[href], button, input, select, textarea, summary, [tabindex]:not([tabindex="-1"]), [contenteditable="true"]'));
    if (state.nativePopover) return;
    state.setMenuOpen(false, "none");
  };
  state.fallbackEscape = (event) => {
    if (event.key !== "Escape" || !state.open || state.nativePopover) return;
    event.preventDefault();
    state.escapePressed = true;
    state.setMenuOpen(false, "none");
    state.trigger.focus({ preventScroll: true });
    state.escapePressed = false;
  };
  state.surfaceKeydown = (event) => {
    if (event.target.closest?.('[role="menu"]') !== state.surface) return;
    // Menus are navigated with arrows, not Tab: the source prevents Tab, so focus stays inside the open menu.
    if (event.key === "Tab") { event.preventDefault(); return; }
    const items = enabledMenuItems(state);
    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      moveMenuFocus(state, event.key === "ArrowDown" ? 1 : -1);
      return;
    }
    if (event.key === "Home" || event.key === "End") {
      event.preventDefault();
      const item = event.key === "Home" ? items[0] : items.at(-1);
      if (item) setMenuTabStop(state, item, true);
      return;
    }
    if (event.key === "Escape") {
      event.preventDefault();
      state.restoreFocusOnClose = true;
      state.setMenuOpen(false, "none");
      return;
    }
    if (
      event.key.length === 1 &&
      !event.ctrlKey &&
      !event.altKey &&
      !event.metaKey &&
      !event.shiftKey &&
      !/\s/.test(event.key)
    ) {
      event.preventDefault();
      runMenuTypeahead(state, event.key);
    }
  };
  state.surfaceInteraction = () => {
    state.restoreFocusOnClose = true;
  };
  state.surfaceFocusIn = (event) => {
    if (event.target.closest?.('[role="menu"]') !== state.surface) return;
    state.restoreFocusOnClose = true;
    const item = event.target.closest?.("[data-rbi-menu-item]");
    if (item && item.getAttribute("aria-disabled") !== "true") setMenuTabStop(state, item);
  };
  state.surfacePointerOver = (event) => {
    pointerMenuFocus(state, event);
  };
  // Leaving an item with the mouse hands focus back to the menu itself, so no item stays highlighted away from the pointer.
  state.surfacePointerOut = (event) => {
    if (event.pointerType !== "mouse" || state.surface.hasAttribute("data-rbi-menubar-popup")) return;
    const item = event.target.closest?.("[data-rbi-menu-item]");
    if (!item || item.contains(event.relatedTarget) || item.closest('[role="menu"]') !== state.surface) return;
    if ([...(state.children ?? [])].some(child => child.open && (child.trigger === item || child.surface.contains(event.relatedTarget)))) return;
    if (item.contains(document.activeElement)) focusMenuSurface(state);
  };

  state.trigger.addEventListener("click", state.click);
  state.trigger.addEventListener("keydown", state.triggerKeydown);
  state.trigger.addEventListener("contextmenu", state.contextmenu);
  state.surface.addEventListener("keydown", state.surfaceKeydown);
  state.surface.addEventListener("pointerdown", state.surfaceInteraction);
  state.surface.addEventListener("focusin", state.surfaceFocusIn);
  state.surface.addEventListener("pointerover", state.surfacePointerOver);
  state.surface.addEventListener("pointerout", state.surfacePointerOut);
  state.surface.addEventListener("toggle", state.toggle);
  rememberConnection(root, state, connectionId);
  const firstItem = enabledMenuItems(state)[0];
  if (firstItem) setMenuTabStop(state, firstItem);
  setVisualOpen(state, open);
  attachWaitingSubmenus(state);
  if (open && state.invocationMode === "dropdown")
    state.trigger.setAttribute("aria-expanded", "true");
}

export function syncMenu(root, open, preferred, align = "center") {
  const state = states.get(root);
  if (!state) return;
  state.preferred = preferred;
  state.notifiedOpen = open;
  if (!state.surface.hasAttribute("data-rbi-menubar-popup") && state.hoverAlign !== align && !(align === "center" && state.hoverAlign === undefined)) configureHoverAlignment(state, align);
  else configureAnchor(state);
  setVisualOpen(state, open);
  if (state.invocationMode === "dropdown")
    state.trigger.setAttribute("aria-expanded", String(open));
}

export function focusMenu(root, target) {
  const state = states.get(root);
  // Delayed server reconciliation must not overwrite subsequent native navigation.
  if (!state || !state.open || state.surface.contains(document.activeElement)) return;
  if (target === "menu") { focusMenuSurface(state); return; }
  const items = enabledMenuItems(state);
  const item = target === "last" ? items.at(-1) : items[0];
  if (item) setMenuTabStop(state, item, true);
}

function focusMenuSurface(state) {
  if (!state.surface.hasAttribute("tabindex")) { state.original.surfaceTabIndex ??= null; state.surface.tabIndex = -1; }
  state.surface.focus({ preventScroll: true });
}

function insideRect(x, y, rect) {
  return x >= rect.left && x <= rect.right && y >= rect.top && y <= rect.bottom;
}

function insideTriangle(x, y, first, second, third) {
  const cross = (a, b) => (x - b.x) * (a.y - b.y) - (a.x - b.x) * (y - b.y);
  const values = [cross(first, second), cross(second, third), cross(third, first)];
  return !(values.some(value => value < 0) && values.some(value => value > 0));
}

export function connectSubMenu(root, reference, open, connectionId) {
  if (states.has(root)) return syncSubMenu(root, open);
  const preferred = getComputedStyle(root).direction === "rtl" ? "left" : "right";
  const state = connectBase(root, "SubMenu", preferred);
  state.offset = 0;
  state.align = "start";
  configureAnchor(state);
  state.loop = false;
  state.requestedOpen = state.notifiedOpen = open;
  state.original.expanded = rememberAttribute(state.trigger, "aria-expanded");
  state.clearTimers = () => {
    clearTimeout(state.openTimer);
    clearTimeout(state.closeTimer);
    clearTimeout(state.typeaheadTimer);
    state.openTimer = state.closeTimer = 0;
  };
  state.inGrace = (x, y) => {
    if (!state.grace || performance.now() > state.grace.expires) return false;
    const box = state.surface.getBoundingClientRect();
    const left = box.left >= state.trigger.getBoundingClientRect().right - 1;
    const edge = left ? box.left : box.right;
    return insideTriangle(x, y, state.grace.origin, { x: edge, y: box.top }, { x: edge, y: box.bottom });
  };
  state.applyOpen = (value, focus = false, restore = false) => {
    const parent = attachSubmenu(state);
    const actual = value && !!parent?.open && state.trigger.getAttribute("aria-disabled") !== "true";
    const restoreTrigger = !actual && restore && state.surface.contains(document.activeElement);
    setVisualOpen(state, actual);
    state.trigger.setAttribute("aria-expanded", String(actual));
    if (actual) {
      document.addEventListener("pointermove", state.trackPointer, true);
      if (focus) setMenuTabStop(state, enabledMenuItems(state)[0], true);
    } else {
      state.clearTimers();
      state.grace = null;
      document.removeEventListener("pointermove", state.trackPointer, true);
      if (restoreTrigger) state.trigger.focus({ preventScroll: true });
    }
  };
  state.changeOpen = (value, focus = false, restore = false) => {
    state.clearTimers();
    state.preferred = getComputedStyle(root).direction === "rtl" ? "left" : "right";
    configureAnchor(state);
    state.requestedOpen = value;
    state.applyOpen(value, focus, restore);
    if (state.notifiedOpen === state.open || state.disconnected) return;
    state.notifiedOpen = state.open;
    void reference.invokeMethodAsync("NativeOpenChanged", state.open).catch(error => {
      if (!state.disconnected) throw error;
    });
  };
  state.trackPointer = event => {
    if (event.pointerType !== "mouse" || !state.open) return;
    if (insideRect(event.clientX, event.clientY, state.trigger.getBoundingClientRect()) || insideRect(event.clientX, event.clientY, state.surface.getBoundingClientRect()) || state.inGrace(event.clientX, event.clientY)) return;
    // Nested descendants remain inside the same command scope even in the top layer.
    const target = document.elementFromPoint(event.clientX, event.clientY);
    if (target && state.root.contains(target)) return;
    state.changeOpen(false, false);
  };
  state.click = event => {
    if (state.trigger.getAttribute("aria-disabled") === "true") return;
    state.trigger.focus({ preventScroll: true });
    state.changeOpen(true, event.detail === 0);
  };
  state.triggerKeydown = event => {
    if (state.trigger.getAttribute("aria-disabled") === "true") return;
    const openingKey = getComputedStyle(root).direction === "rtl" ? "ArrowLeft" : "ArrowRight";
    if (![openingKey, "Enter", " "].includes(event.key)) return;
    event.preventDefault();
    event.stopPropagation();
    state.changeOpen(true, true);
  };
  state.pointermove = event => {
    if (event.pointerType !== "mouse" || state.open || state.openTimer || state.trigger.getAttribute("aria-disabled") === "true") return;
    state.openTimer = window.setTimeout(() => state.changeOpen(true, false), 100);
  };
  state.pointerleave = event => {
    if (event.pointerType !== "mouse") return;
    clearTimeout(state.openTimer);
    state.openTimer = 0;
    if (!state.open) return;
    const box = state.surface.getBoundingClientRect();
    state.grace = { origin: { x: event.clientX + (box.left > event.clientX ? -5 : 5), y: event.clientY }, expires: performance.now() + 300 };
  };
  state.focusout = event => {
    if (root.contains(event.relatedTarget)) return;
    state.changeOpen(false, false);
  };
  state.surfaceKeydown = event => {
    if (event.target.closest?.('[role="menu"]') !== state.surface) return;
    const closingKey = getComputedStyle(root).direction === "rtl" ? "ArrowRight" : "ArrowLeft";
    if (event.key === closingKey) {
      event.preventDefault(); event.stopPropagation();
      state.changeOpen(false, false, true);
    } else if (event.key === "Escape") {
      event.preventDefault(); event.stopPropagation();
      let top = attachSubmenu(state);
      while (top?.type === "SubMenu") top = attachSubmenu(top);
      top?.setMenuOpen(false, "none");
      top?.trigger.focus({ preventScroll: true });
    } else if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault(); event.stopPropagation();
      moveMenuFocus(state, event.key === "ArrowDown" ? 1 : -1);
    } else if (event.key === "Home" || event.key === "End") {
      event.preventDefault(); event.stopPropagation();
      const items = enabledMenuItems(state);
      setMenuTabStop(state, event.key === "Home" ? items[0] : items.at(-1), true);
    } else if (event.key.length === 1 && !event.ctrlKey && !event.altKey && !event.metaKey && !event.shiftKey && !/\s/.test(event.key)) {
      event.preventDefault(); event.stopPropagation();
      runMenuTypeahead(state, event.key);
    }
  };
  state.surfaceFocusIn = event => {
    if (event.target.closest?.('[role="menu"]') !== state.surface) return;
    const item = event.target.closest?.("[data-rbi-menu-item]");
    if (item && item.getAttribute("aria-disabled") !== "true") setMenuTabStop(state, item);
  };
  state.surfacePointerOver = event => pointerMenuFocus(state, event);
  state.trigger.addEventListener("click", state.click);
  state.trigger.addEventListener("keydown", state.triggerKeydown);
  state.trigger.addEventListener("pointermove", state.pointermove);
  state.trigger.addEventListener("pointerleave", state.pointerleave);
  root.addEventListener("focusout", state.focusout);
  state.surface.addEventListener("keydown", state.surfaceKeydown);
  state.surface.addEventListener("focusin", state.surfaceFocusIn);
  state.surface.addEventListener("pointerover", state.surfacePointerOver);
  rememberConnection(root, state, connectionId);
  state.applyOpen(open);
}

export function syncSubMenu(root, open) {
  const state = states.get(root);
  if (!state) return;
  state.requestedOpen = state.notifiedOpen = open;
  state.preferred = getComputedStyle(root).direction === "rtl" ? "left" : "right";
  configureAnchor(state);
  state.applyOpen(open, false, !open);
}

function barTriggers(state) {
  return [...state.root.querySelectorAll("[data-rbi-menubar-trigger]")].filter(trigger => !trigger.disabled);
}

function barMenu(trigger) {
  return states.get(trigger.closest("[data-rbi-menu-host]"));
}

function barTabStop(state, trigger) {
  for (const item of barTriggers(state)) item.tabIndex = item === trigger ? 0 : -1;
}

export function connectMenubar(root, reference, activeId, connectionId) {
  if (menubars.has(root)) return syncMenubar(root, activeId);
  const state = { root, reference, activeId, type: "Menubar", connectionId, suppress: false, disposed: false };
  state.menuChanged = (trigger, open) => {
    if (state.suppress || state.disposed) return;
    const id = trigger.getAttribute("data-rbi-menubar-trigger");
    const proposed = open ? id : state.activeId === id ? null : state.activeId;
    if (state.activeId === proposed) return;
    state.activeId = proposed;
    void reference.invokeMethodAsync("NativeActiveChanged", proposed).catch(error => { if (!state.disposed) throw error; });
  };
  state.activate = (trigger, focusTarget = "first") => {
    const id = trigger?.getAttribute("data-rbi-menubar-trigger");
    if (!id) return;
    state.suppress = true;
    for (const item of barTriggers(state)) {
      const menu = barMenu(item);
      if (menu) menu.setMenuOpen(item === trigger, item === trigger ? focusTarget : "none");
    }
    state.suppress = false;
    barTabStop(state, trigger);
    state.menuChanged(trigger, true);
  };
  state.keydown = event => {
    const items = barTriggers(state);
    const trigger = event.target.closest?.("[data-rbi-menubar-trigger]");
    const popup = event.target.closest?.("[data-rbi-menubar-popup]");
    const current = trigger ?? items.find(item => barMenu(item)?.surface === popup);
    if (!current || !items.length) return;
    const rtl = getComputedStyle(root).direction === "rtl";
    const forward = rtl ? "ArrowLeft" : "ArrowRight";
    const backward = rtl ? "ArrowRight" : "ArrowLeft";
    if ([forward, backward, "Home", "End"].includes(event.key) && (trigger || [forward, backward].includes(event.key))) {
      // Submenu triggers own the forward arrow; nested surfaces own their keys.
      if (!trigger && (event.target.getAttribute("aria-haspopup") === "menu" || event.target.closest('[role="menu"]') !== popup)) return;
      event.preventDefault(); event.stopPropagation();
      const index = items.indexOf(current);
      const next = event.key === "Home" ? items[0] : event.key === "End" ? items.at(-1) : items[(index + (event.key === forward ? 1 : -1) + items.length) % items.length];
      const wasOpen = !!state.activeId;
      barTabStop(state, next);
      next.focus({ preventScroll: true });
      // Switching menus with an arrow leaves focus on the new menu itself, as in the source.
      if (wasOpen) state.activate(next, "menu");
    }
  };
  state.pointerover = event => {
    const trigger = event.target.closest?.("[data-rbi-menubar-trigger]");
    if (!trigger || trigger.disabled || !state.activeId || state.activeId === trigger.dataset.rbiMenubarTrigger) return;
    trigger.focus({ preventScroll: true });
    state.activate(trigger, "menu");
  };
  state.focusin = event => {
    const trigger = event.target.closest?.("[data-rbi-menubar-trigger]");
    if (trigger && !trigger.disabled) barTabStop(state, trigger);
  };
  root.addEventListener("keydown", state.keydown);
  root.addEventListener("pointerover", state.pointerover);
  root.addEventListener("focusin", state.focusin);
  menubars.set(root, state);
  if (connectionId) connections.set(connectionId, state);
  syncMenubar(root, activeId);
}

export function syncMenubar(root, activeId) {
  const state = menubars.get(root);
  if (!state) return;
  state.activeId = activeId;
  const items = barTriggers(state);
  const active = items.find(item => item.dataset.rbiMenubarTrigger === activeId);
  const focused = items.includes(document.activeElement) ? document.activeElement : null;
  barTabStop(state, focused ?? active ?? items[0]);
}

function refreshNavigationGeometry(state) {
  if (state.disconnected || !state.activeId) return;
  const root = state.root;
  const trigger = root.querySelector('[data-rbi-navigation-trigger="' + CSS.escape(state.activeId) + '"]');
  if (!trigger) return;
  const content = state.surface.firstElementChild;
  if (content !== state.observedContent) {
    if (state.observedContent) state.navigationResize.unobserve(state.observedContent);
    state.observedContent = content;
    if (content) state.navigationResize.observe(content);
  }
  if (content) {
    const css = getComputedStyle(state.surface);
    const mobileBorder = state.navigationMedia.matches ? 0 : parseFloat(css.borderLeftWidth) + parseFloat(css.borderRightWidth);
    state.surface.style.width = `${content.offsetWidth + mobileBorder}px`;
    state.surface.style.height = `${content.offsetHeight}px`;
  }
  root.style.setProperty('--rbi-navigation-indicator-x', `${trigger.getBoundingClientRect().left - root.getBoundingClientRect().left + trigger.offsetWidth / 2}px`);
  root.style.setProperty('--rbi-navigation-indicator-width', `${trigger.offsetWidth}px`);
  if (!state.anchorSupported) requestPosition(state);
}

export function connectNavigation(root, reference, activeId, connectionId) {
  if (states.has(root)) return syncNavigation(root, activeId);
  const state = connectBase(root, "Navigation", "bottom");
  state.connectionId = connectionId;
  state.positionSource = root; state.align = "start"; state.offset = 6;
  state.activeId = activeId; state.pendingFocus = null;
  state.navigationMedia = matchMedia('(min-width: 768px)');
  state.navigationReflow = () => refreshNavigationGeometry(state);
  state.navigationMedia.addEventListener('change', state.navigationReflow);
  state.navigationResize = new ResizeObserver(() => refreshNavigationGeometry(state));
  state.navigationResize.observe(root);
  const triggers = () => [...root.querySelectorAll('[data-rbi-navigation-list] button:not(:disabled), [data-rbi-navigation-list] a[href]')];
  const current = () => root.querySelector('[data-rbi-navigation-trigger="' + CSS.escape(state.activeId ?? '') + '"]');
  state.request = (id, focus = false) => {
    clearTimeout(state.openTimer); clearTimeout(state.closeTimer);
    state.pendingFocus = focus ? id : null;
    void reference.invokeMethodAsync('NativeNavigationChanged', id).catch(error => { if (!state.disconnected) throw error; });
  };
  state.click = event => {
    const trigger = event.target.closest('[data-rbi-navigation-trigger]');
    if (trigger && !trigger.disabled) state.request(state.activeId === trigger.dataset.rbiNavigationTrigger ? null : trigger.dataset.rbiNavigationTrigger);
    else if (event.target.closest('a[href]') && state.open) state.request(null);
  };
  state.keydown = event => {
    const items = triggers(), item = event.target.closest('[data-rbi-navigation-list] button, [data-rbi-navigation-list] a');
    if (event.key === 'Escape' && state.open && root.contains(event.target)) {
      event.preventDefault(); const trigger = current(); state.request(null); trigger?.focus({ preventScroll: true }); return;
    }
    if (state.surface.contains(event.target) && ['ArrowUp','ArrowDown','ArrowLeft','ArrowRight','Home','End'].includes(event.key)) {
      const links = [...state.surface.querySelectorAll('a[href],button:not(:disabled),[tabindex="0"]')];
      const index = links.indexOf(document.activeElement);
      if (index >= 0 && links.length) {
        event.preventDefault();
        const next = event.key === 'Home' ? 0 : event.key === 'End' ? links.length - 1 : Math.max(0, Math.min(index + (['ArrowUp','ArrowLeft'].includes(event.key) ? -1 : 1), links.length - 1));
        links[next].focus({ preventScroll: true });
      }
      return;
    }
    if (!item || !items.includes(item)) return;
    if (event.key === 'ArrowDown' && item.dataset.rbiNavigationTrigger) {
      event.preventDefault(); state.request(item.dataset.rbiNavigationTrigger, true); return;
    }
    if (!['ArrowLeft','ArrowRight','Home','End'].includes(event.key)) return;
    event.preventDefault();
    const rtl = getComputedStyle(root).direction === 'rtl';
    const index = items.indexOf(item);
    const next = event.key === 'Home' ? 0 : event.key === 'End' ? items.length - 1 : (index + (event.key === (rtl ? 'ArrowLeft' : 'ArrowRight') ? 1 : -1) + items.length) % items.length;
    items[next].focus({ preventScroll: true });
  };
  state.pointerover = event => {
    if (event.pointerType === 'touch') return;
    clearTimeout(state.closeTimer);
    const trigger = event.target.closest('[data-rbi-navigation-trigger]');
    if (trigger && !trigger.disabled && trigger.dataset.rbiNavigationTrigger !== state.activeId) {
      clearTimeout(state.openTimer); state.openTimer = setTimeout(() => state.request(trigger.dataset.rbiNavigationTrigger), state.open ? 0 : 200);
    }
  };
  state.pointerleave = event => {
    if (event.pointerType === 'touch' || root.contains(event.relatedTarget)) return;
    clearTimeout(state.openTimer); clearTimeout(state.closeTimer);
    state.closeTimer = setTimeout(() => state.request(null), 150);
  };
  state.focusout = event => { if (event.relatedTarget && !root.contains(event.relatedTarget)) state.request(null); };
  state.outsidePointer = event => { if (state.open && !root.contains(event.target)) state.request(null); };
  root.addEventListener('click', state.click); root.addEventListener('keydown', state.keydown);
  root.addEventListener('pointerover', state.pointerover); root.addEventListener('pointerleave', state.pointerleave); root.addEventListener('focusout', state.focusout);
  document.addEventListener('pointerdown', state.outsidePointer, true);
  rememberConnection(root, state, connectionId); syncNavigation(root, activeId);
}

export function syncNavigation(root, activeId) {
  const state = states.get(root);
  if (!state) return;
  const wasOpen = state.open;
  state.activeId = activeId; configureAnchor(state); setVisualOpen(state, activeId !== null);
  if (wasOpen && activeId === null && document.activeElement === document.body)
    root.querySelector('[data-rbi-navigation-list] button:not(:disabled),[data-rbi-navigation-list] a[href]')?.focus({ preventScroll: true });
  if (activeId === null && state.observedContent) { state.navigationResize.unobserve(state.observedContent); state.observedContent = null; }
  const trigger = [...root.querySelectorAll('[data-rbi-navigation-trigger]')].find(item => item.dataset.rbiNavigationTrigger === activeId);
  if (trigger) {
    refreshNavigationGeometry(state);
    if (state.pendingFocus === activeId) { state.pendingFocus = null; state.surface.querySelector('a[href],button:not(:disabled),[tabindex="0"]')?.focus({ preventScroll: true }); }
  }
}

export function activeNavigationConnectionCount() { return [...connections.values()].filter(state => state.type === "Navigation").length; }
export function activeConnectionCount() { return connections.size; }

export function disconnect(root, connectionId) {
  const bar = menubars.get(root) ?? connections.get(connectionId);
  if (bar?.type === "Menubar") {
    root = bar.root;
    bar.disposed = true;
    root.removeEventListener("keydown", bar.keydown);
    root.removeEventListener("pointerover", bar.pointerover);
    root.removeEventListener("focusin", bar.focusin);
    menubars.delete(root);
    connections.delete(bar.connectionId);
    return;
  }
  const state = states.get(root) ?? connections.get(connectionId);
  if (!state) return;
  root = state.root;
  state.disconnected = true;
  closeSubmenus(state);
  clearTimeout(state.openTimer);
  clearTimeout(state.closeTimer);
  stopFallbackListeners(state);
  stopPopoverListeners(state);
  if (state.type === "Navigation") {
    state.navigationResize.disconnect(); state.observedContent = null;
    state.navigationMedia.removeEventListener('change', state.navigationReflow);
    root.removeEventListener('click', state.click); root.removeEventListener('keydown', state.keydown);
    root.removeEventListener('pointerover', state.pointerover); root.removeEventListener('pointerleave', state.pointerleave); root.removeEventListener('focusout', state.focusout);
    document.removeEventListener('pointerdown', state.outsidePointer, true);
    root.style.removeProperty('--rbi-navigation-indicator-x');
    root.style.removeProperty('--rbi-navigation-indicator-width');
    root.style.removeProperty('anchor-name');
    state.surface.style.removeProperty('width'); state.surface.style.removeProperty('height');
  } else if (state.type === "Tooltip") {
    state.endGrace();
    state.trigger.removeEventListener("pointermove", state.pointermove);
    state.trigger.removeEventListener("pointerleave", state.pointerleave);
    state.trigger.removeEventListener("pointerdown", state.pointerdown);
    state.trigger.removeEventListener("click", state.click);
    state.trigger.removeEventListener("focusin", state.focusin);
    state.trigger.removeEventListener("focusout", state.focusout);
    state.trigger.removeEventListener("keydown", state.keydown);
    state.surface.removeEventListener("pointerleave", state.surfaceleave);
    document.removeEventListener("pointerup", state.pointerup);
    window.removeEventListener("scroll", state.scroll, true);
    if (tooltipGroup.current === state) tooltipGroup.current = null;
    restoreAttribute(state.trigger, "aria-describedby", state.original.describedBy);
  } else if (state.type === "SubMenu") {
    state.clearTimers();
    state.parent?.children?.delete(state);
    document.removeEventListener("pointermove", state.trackPointer, true);
    state.trigger.removeEventListener("click", state.click);
    state.trigger.removeEventListener("keydown", state.triggerKeydown);
    state.trigger.removeEventListener("pointermove", state.pointermove);
    state.trigger.removeEventListener("pointerleave", state.pointerleave);
    root.removeEventListener("focusout", state.focusout);
    state.surface.removeEventListener("keydown", state.surfaceKeydown);
    state.surface.removeEventListener("focusin", state.surfaceFocusIn);
    state.surface.removeEventListener("pointerover", state.surfacePointerOver);
    restoreAttribute(state.trigger, "aria-expanded", state.original.expanded);
  } else if (state.type === "HoverCard") {
    for (const element of [state.trigger, state.surface]) {
      element.removeEventListener("pointerenter", state.pointerenter);
      element.removeEventListener("pointerleave", state.pointerleave);
      element.removeEventListener("focusin", state.focusin);
      element.removeEventListener("focusout", state.focusout);
    }
    state.trigger.removeEventListener("pointerdown", state.pointerdown);
    restoreAttribute(state.trigger, "aria-details", state.original.details);
  } else if (state.type === "Popover") {
    cancelAnimationFrame(state.focusFrame);
    state.trigger.removeEventListener("click", state.click);
    state.surface.removeEventListener("pointerdown", state.surfaceInteraction);
    state.surface.removeEventListener("focusin", state.surfaceInteraction);
    state.surface.removeEventListener("toggle", state.toggle);
    restoreAttribute(state.trigger, "aria-expanded", state.original.expanded);
    restoreAttribute(state.trigger, "aria-controls", state.original.controls);
    restoreAttribute(state.trigger, "aria-haspopup", state.original.hasPopup);
    restoreAttribute(state.surface, "tabindex", state.original.surfaceTabIndex);
  } else {
    state.trigger.removeEventListener("click", state.click);
    state.trigger.removeEventListener("keydown", state.triggerKeydown);
    state.trigger.removeEventListener("contextmenu", state.contextmenu);
    state.surface.removeEventListener("keydown", state.surfaceKeydown);
    state.surface.removeEventListener("pointerdown", state.surfaceInteraction);
    state.surface.removeEventListener("focusin", state.surfaceFocusIn);
    state.surface.removeEventListener("pointerover", state.surfacePointerOver);
    state.surface.removeEventListener("pointerout", state.surfacePointerOut);
    state.surface.removeEventListener("toggle", state.toggle);
    window.clearTimeout(state.typeaheadTimer);
    restoreAttribute(state.trigger, "aria-expanded", state.original.expanded);
    restoreAttribute(state.trigger, "aria-controls", state.original.controls);
    restoreAttribute(state.trigger, "aria-haspopup", state.original.hasPopup);
    restoreAttribute(state.trigger, "tabindex", state.original.tabIndex);
  }
  if (state.nativePopover && state.surface.matches(":popover-open")) state.surface.hidePopover();
  state.positionSource.style.removeProperty("anchor-name");
  state.surface.style.removeProperty("translate");
  state.contextAnchor?.remove();
  states.delete(root);
  connections.delete(state.connectionId);
}
