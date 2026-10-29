const controls = new WeakMap();
const connections = new Map();

function invoke(state, method, ...args) {
  if (state.disposed) return;
  return state.reference.invokeMethodAsync(method, ...args).catch(error => { if (!state.disposed) throw error; });
}

function carousel(state) {
  const root = state.root;
  const viewport = root.querySelector('[data-rbi-carousel-viewport]');
  const track = root.querySelector('.rbi-carousel__content');
  const slides = () => [...track.children].filter(element => element.hasAttribute('data-rbi-carousel-slide'));
  const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)');
  let index = 0, frame = 0, settleFrame = 0, moving = false, drag, nativeTouch = false, touchScrollStart = 0, touchStart = 0, resetLoop = false, motionTarget;
  let previews = [], previewSources = [];
  let previousShape;
  const vertical = () => state.settings.vertical;
  function refreshPreviews(force = false) {
    const items = slides();
    const sources = state.settings.loop && items.length > 1 ? [items.at(-1), items[0]] : [];
    if (!force && sources.length === previewSources.length && sources.every((element, position) => element === previewSources[position])) return;
    state.mutation?.disconnect();
    for (const preview of previews) preview.remove();
    previews = []; previewSources = sources;
    for (const [position, source] of sources.entries()) {
      // Read-only edge previews, not extra Razor instances or application subscriptions.
      const preview = source.cloneNode(true);
      preview.removeAttribute('data-rbi-carousel-slide');
      preview.removeAttribute('data-rbi-current');
      preview.dataset.rbiCarouselPreview = String(position === 0 ? -1 : items.length);
      preview.inert = true; preview.setAttribute('aria-hidden', 'true');
      for (const element of [preview, ...preview.querySelectorAll('*')]) {
        element.removeAttribute('id'); element.removeAttribute('autofocus');
        // An inert preview must never add values to an enclosing application's form.
        if (element.matches('input,select,textarea,button')) { element.removeAttribute('name'); element.removeAttribute('form'); }
        if (element.matches('video,audio')) element.removeAttribute('autoplay');
      }
      if (position === 0) track.prepend(preview); else track.append(preview);
      previews.push(preview);
    }
    state.mutation?.observe(track, { childList: true });
  }
  function offset(element) {
    const rect = element.getBoundingClientRect(), box = viewport.getBoundingClientRect();
    if (!vertical()) return rect.left - box.left + 16;
    const items = slides();
    // Source's center alignment measures the track, then trims the non-loop endpoints.
    const alignment = !state.settings.loop && element === items[0] ? 0
      : !state.settings.loop && element === items.at(-1) ? track.clientHeight - element.offsetHeight
        : (track.clientHeight - element.offsetHeight) / 2;
    return rect.top - box.top + 16 - alignment;
  }
  function show(value, notify = false, force = false) {
    refreshPreviews();
    const items = slides();
    if (!items.length) { index = 0; moving = false; resetLoop = false; motionTarget = null; viewport.scrollTo({ left: 0, top: 0, behavior: 'instant' }); return; }
    const next = state.settings.loop ? (value % items.length + items.length) % items.length : Math.max(0, Math.min(value, items.length - 1));
    if (moving && motionTarget?.isConnected && next === index && !notify && !force) return; // Parent reconciliation must not interrupt owned motion.
    index = next;
    const preview = previews.find(element => Number(element.dataset.rbiCarouselPreview) === value);
    const target = preview ?? items[index], delta = offset(target);
    motionTarget = target;
    resetLoop = !!preview;
    moving = Math.abs(delta) > .5;
    const smooth = notify && !reducedMotion.matches;
    viewport.scrollBy({ [vertical() ? 'top' : 'left']: delta, behavior: smooth ? 'smooth' : 'instant' });
    for (let item = 0; item < items.length; item++) items[item].dataset.rbiCurrent = String(item === index);
    root.querySelector('[data-rbi-carousel-previous]').disabled = items.length < 2 || !state.settings.loop && index === 0;
    root.querySelector('[data-rbi-carousel-next]').disabled = items.length < 2 || !state.settings.loop && index === items.length - 1;
    if (!smooth || !moving) {
      cancelAnimationFrame(settleFrame);
      settleFrame = requestAnimationFrame(settle);
    }
    if (notify) void invoke(state, 'NativeCarouselIndex', index);
  }
  function settle() {
    settleFrame = 0;
    if (state.disposed) return;
    moving = false;
    if (resetLoop) {
      resetLoop = false;
      const target = slides()[index];
      if (target) viewport.scrollBy({ [vertical() ? 'top' : 'left']: offset(target), behavior: 'instant' });
    }
  }
  function nearest() {
    return [...slides().map((slide, item) => ({ item, distance: Math.abs(offset(slide)) })), ...previews.map(slide => ({ item: Number(slide.dataset.rbiCarouselPreview), distance: Math.abs(offset(slide)) }))].sort((a, b) => a.distance - b.distance)[0]?.item ?? 0;
  }
  function gestureTarget(startIndex, displacement) {
    const last = slides().length - 1;
    if (state.settings.loop && Math.abs(displacement) > 40) {
      if (startIndex === 0 && displacement > 0) return -1;
      if (startIndex === last && displacement < 0) return last + 1;
    }
    return nearest();
  }
  state.click = event => {
    if (event.target.closest('.rbi-carousel') !== root) return;
    if (event.target.closest('[data-rbi-carousel-previous]')) show(index - 1, true);
    if (event.target.closest('[data-rbi-carousel-next]')) show(index + 1, true);
  };
  state.keydown = event => {
    if (event.target.closest('.rbi-carousel') !== root) return;
    if (event.target.matches('input,textarea,select,[contenteditable="true"]')) return;
    const direction = getComputedStyle(root).direction === 'rtl' ? -1 : 1;
    const delta = event.key === 'ArrowLeft' ? -direction : event.key === 'ArrowRight' ? direction : vertical() && event.key === 'ArrowUp' ? -1 : vertical() && event.key === 'ArrowDown' ? 1 : 0;
    if (!delta) return;
    event.preventDefault(); show(index + delta, true);
  };
  state.scroll = () => {
    if (moving || drag || nativeTouch || frame) return;
    frame = requestAnimationFrame(() => {
      frame = 0; const next = nearest();
      if (next !== index) show(next, true);
    });
  };
  state.pointerdown = event => {
    if (event.pointerType !== 'mouse' || event.button !== 0 || event.target.closest('a,button,input,select,textarea')) return;
    refreshPreviews(true); moving = false; resetLoop = false;
    drag = { id: event.pointerId, index, start: vertical() ? event.clientY : event.clientX, scroll: vertical() ? viewport.scrollTop : viewport.scrollLeft };
    viewport.setPointerCapture(event.pointerId); viewport.style.scrollSnapType = 'none';
  };
  state.pointermove = event => {
    if (!drag || drag.id !== event.pointerId) return;
    const value = drag.scroll + drag.start - (vertical() ? event.clientY : event.clientX);
    if (vertical()) viewport.scrollTop = value; else viewport.scrollLeft = value;
  };
  state.pointerup = event => {
    if (!drag || drag.id !== event.pointerId) return;
    const target = event.type === 'pointercancel' ? nearest() : gestureTarget(drag.index, (vertical() ? event.clientY : event.clientX) - drag.start);
    drag = null; viewport.style.removeProperty('scroll-snap-type');
    if (viewport.hasPointerCapture(event.pointerId)) viewport.releasePointerCapture(event.pointerId);
    show(target, true);
  };
  state.touchstart = event => { nativeTouch = true; touchScrollStart = vertical() ? viewport.scrollTop : viewport.scrollLeft; touchStart = vertical() ? event.touches[0].clientY : event.touches[0].clientX; };
  state.touchend = event => {
    if (touchScrollStart !== (vertical() ? viewport.scrollTop : viewport.scrollLeft)) return;
    nativeTouch = false;
    if (event.type === 'touchcancel' || !event.changedTouches.length) return;
    show(gestureTarget(index, (vertical() ? event.changedTouches[0].clientY : event.changedTouches[0].clientX) - touchStart), true);
  };
  state.scrollend = () => {
    if (nativeTouch) { nativeTouch = false; show(nearest(), true); }
    else if (!moving || !motionTarget?.isConnected || Math.abs(offset(motionTarget)) < 1) settle();
  };
  root.addEventListener('click', state.click); root.addEventListener('keydown', state.keydown);
  viewport.addEventListener('scroll', state.scroll); viewport.addEventListener('pointerdown', state.pointerdown);
  viewport.addEventListener('pointermove', state.pointermove); viewport.addEventListener('pointerup', state.pointerup); viewport.addEventListener('pointercancel', state.pointerup);
  viewport.addEventListener('touchstart', state.touchstart, { passive: true }); viewport.addEventListener('touchend', state.touchend, { passive: true }); viewport.addEventListener('touchcancel', state.touchend, { passive: true }); viewport.addEventListener('scrollend', state.scrollend);
  state.observer = new ResizeObserver(() => { if (!nativeTouch && !drag) show(index, false, true); }); state.observer.observe(viewport);
  state.mutation = new MutationObserver(() => { refreshPreviews(true); show(Math.min(index, slides().length - 1), true, true); });
  state.mutation.observe(track, { childList: true });
  state.sync = () => {
    const shape = `${state.settings.loop}:${state.settings.vertical}:${slides().length}`;
    if (!drag && !nativeTouch) show(state.settings.index, false, previousShape !== shape);
    previousShape = shape;
  };
  state.dispose = () => {
    cancelAnimationFrame(frame); cancelAnimationFrame(settleFrame); state.observer.disconnect(); state.mutation.disconnect();
    viewport.scrollTo({ left: viewport.scrollLeft, top: viewport.scrollTop, behavior: 'instant' });
    for (const preview of previews) preview.remove(); previews = []; previewSources = [];
    if (drag && viewport.hasPointerCapture(drag.id)) viewport.releasePointerCapture(drag.id);
    root.removeEventListener('click', state.click); root.removeEventListener('keydown', state.keydown);
    viewport.removeEventListener('scroll', state.scroll); viewport.removeEventListener('pointerdown', state.pointerdown);
    viewport.removeEventListener('pointermove', state.pointermove); viewport.removeEventListener('pointerup', state.pointerup); viewport.removeEventListener('pointercancel', state.pointerup);
    viewport.removeEventListener('touchstart', state.touchstart); viewport.removeEventListener('touchend', state.touchend); viewport.removeEventListener('touchcancel', state.touchend); viewport.removeEventListener('scrollend', state.scrollend);
  };
}

function command(state) {
  state.keydown = event => {
    if (!event.target.closest('[role="combobox"]') || event.isComposing || event.keyCode === 229) return;
    let key = event.key;
    if (event.ctrlKey && ['n', 'j'].includes(key)) key = 'ArrowDown';
    if (event.ctrlKey && ['p', 'k'].includes(key)) key = 'ArrowUp';
    if (!['ArrowUp','ArrowDown','Home','End','Enter','Escape'].includes(key)) return;
    if (key !== 'Escape') { event.preventDefault(); event.stopPropagation(); }
    if (['ArrowUp', 'ArrowDown'].includes(key)) {
      if (event.metaKey) key = key === 'ArrowDown' ? 'End' : 'Home';
      else if (event.altKey) key = key === 'ArrowDown' ? 'GroupNext' : 'GroupPrevious';
    }
    void invoke(state, 'NativeCommandKey', key);
  };
  state.root.addEventListener('keydown', state.keydown);
  state.sync = () => {
    const option = state.root.querySelector(`[data-rbi-command-index="${state.settings.active}"]`);
    if (option && state.root.contains(document.activeElement)) option.scrollIntoView({ block: 'nearest', inline: 'nearest' });
  };
  state.dispose = () => state.root.removeEventListener('keydown', state.keydown);
}

function resize(state) {
  const root = state.root;
  const panels = () => [...root.querySelectorAll(':scope > [data-rbi-resizable-panel]')];
  let sizes = [], drag;
  function paint() {
    const items = panels();
    items.forEach((panel, index) => panel.style.flex = `${sizes[index]} 1 0px`);
    for (const handle of root.querySelectorAll('[data-rbi-resizable-handle]')) {
      const index = Number(handle.dataset.rbiResizableHandle), pair = sizes[index] + sizes[index + 1];
      handle.setAttribute('aria-valuenow', String(sizes[index]));
      handle.setAttribute('aria-valuemin', String(Math.max(Number(items[index].dataset.rbiMin), pair - Number(items[index + 1].dataset.rbiMax))));
      handle.setAttribute('aria-valuemax', String(Math.min(Number(items[index].dataset.rbiMax), pair - Number(items[index + 1].dataset.rbiMin))));
    }
  }
  function adjust(index, value) {
    const items = panels(), pair = sizes[index] + sizes[index + 1];
    const min = Math.max(Number(items[index].dataset.rbiMin), pair - Number(items[index + 1].dataset.rbiMax));
    const max = Math.min(Number(items[index].dataset.rbiMax), pair - Number(items[index + 1].dataset.rbiMin));
    sizes[index] = Math.max(min, Math.min(value, max)); sizes[index + 1] = pair - sizes[index]; paint();
  }
  state.pointerdown = event => {
    const handle = event.target.closest('[data-rbi-resizable-handle]');
    if (!handle || event.button !== 0) return;
    event.preventDefault(); handle.focus();
    drag = { id: event.pointerId, handle, index: Number(handle.dataset.rbiResizableHandle), start: state.settings.vertical ? event.clientY : event.clientX, sizes: sizes.slice(), extent: state.settings.vertical ? root.clientHeight : root.clientWidth };
    handle.setPointerCapture(event.pointerId);
  };
  state.pointermove = event => {
    if (!drag || drag.id !== event.pointerId || !drag.extent) return;
    const direction = !state.settings.vertical && getComputedStyle(root).direction === 'rtl' ? -1 : 1;
    const delta = (state.settings.vertical ? event.clientY : event.clientX) - drag.start;
    adjust(drag.index, drag.sizes[drag.index] + delta / drag.extent * 100 * direction);
  };
  state.pointerup = event => {
    if (!drag || drag.id !== event.pointerId) return;
    if (event.type === 'pointercancel') { sizes = drag.sizes; paint(); }
    else void invoke(state, 'NativeResize', sizes.slice());
    const handle = drag.handle; drag = null;
    if (handle.hasPointerCapture(event.pointerId)) handle.releasePointerCapture(event.pointerId);
  };
  state.keydown = event => {
    const handle = event.target.closest('[data-rbi-resizable-handle]');
    if (!handle || !['ArrowLeft','ArrowRight','ArrowUp','ArrowDown','Home','End'].includes(event.key)) return;
    const vertical = state.settings.vertical;
    if (vertical && ['ArrowLeft','ArrowRight'].includes(event.key) || !vertical && ['ArrowUp','ArrowDown'].includes(event.key)) return;
    event.preventDefault();
    const index = Number(handle.dataset.rbiResizableHandle), backward = vertical ? 'ArrowUp' : getComputedStyle(root).direction === 'rtl' ? 'ArrowRight' : 'ArrowLeft';
    adjust(index, event.key === 'Home' ? -Infinity : event.key === 'End' ? Infinity : sizes[index] + (event.key === backward ? -1 : 1) * (event.shiftKey ? 10 : 1));
    void invoke(state, 'NativeResize', sizes.slice());
  };
  for (const name of ['pointerdown','pointermove','pointerup','pointercancel','keydown']) root.addEventListener(name, name === 'pointercancel' ? state.pointerup : state[name]);
  state.sync = () => { if (!drag) { sizes = state.settings.sizes.slice(); paint(); } };
  state.dispose = () => {
    if (drag && drag.handle.hasPointerCapture(drag.id)) drag.handle.releasePointerCapture(drag.id);
    for (const name of ['pointerdown','pointermove','pointerup','pointercancel','keydown']) root.removeEventListener(name, name === 'pointercancel' ? state.pointerup : state[name]);
    drag = null;
  };
}

function sidebar(state) {
  const media = matchMedia('(max-width: 767px)');
  let open = state.settings.open;
  const viewport = () => { void invoke(state, 'NativeSidebarViewport', media.matches); };
  const key = event => {
    if (event.key.toLowerCase() !== 'b' || !(event.ctrlKey || event.metaKey) || event.defaultPrevented) return;
    // Prefer the owning visible sidebar; one shortcut must not toggle unrelated nested layouts.
    const owner = event.target.closest?.('.rbi-sidebar-layout');
    if (owner && owner !== state.root || !state.root.getClientRects().length) return;
    event.preventDefault(); void invoke(state, 'NativeSidebarToggle');
  };
  media.addEventListener('change', viewport); document.addEventListener('keydown', key); viewport();
  state.sync = () => {
    if (open === state.settings.open) return;
    open = state.settings.open;
    if (state.settings.cookieName) document.cookie = `${state.settings.cookieName}=${open}; path=/; max-age=604800; SameSite=Lax${location.protocol === 'https:' ? '; Secure' : ''}`;
  };
  state.dispose = () => { media.removeEventListener('change', viewport); document.removeEventListener('keydown', key); };
}

// Drawers can share one caller-marked surface. Snapshot its styles once, and
// retain scale/body ownership until the last drawer releases it.
const drawerBackgrounds = new WeakMap();
let scaledBodyOwners = 0, savedBodyBackground = '', savedBodyBackgroundPriority = '';

function finishDrawerBackground(record) {
  if (record.restored) return;
  record.restored = true; record.version++;
  record.restore(false);
  drawerBackgrounds.delete(record.target);
  for (const subscriber of record.subscribers) subscriber.drawerBackgroundRecords.delete(record);
  record.subscribers.clear();
  if (--scaledBodyOwners === 0) {
    if (savedBodyBackground) document.body.style.setProperty('background', savedBodyBackground, savedBodyBackgroundPriority);
    else document.body.style.removeProperty('background');
  }
}

function releaseDrawerBackground(record, state, immediate = false) {
  if (record.restored) return;
  record.owners.delete(state);
  if (record.owners.size) {
    record.subscribers.delete(state); state.drawerBackgroundRecords.delete(record);
    return;
  }
  if (immediate) { finishDrawerBackground(record); return; }
  if (record.closing) return;
  record.closing = true;
  const version = ++record.version;
  record.restore(true);
  // Await only the owned transform/radius transitions, not consumer animations.
  const animations = record.target.getAnimations().filter(animation => animation instanceof CSSTransition && (animation.transitionProperty === 'transform' || animation.transitionProperty.endsWith('radius')));
  Promise.all(animations.map(animation => animation.finished.catch(error => {
    if (error.name !== 'AbortError') throw error;
  }))).then(() => {
    if (!record.restored && !record.owners.size && record.version === version) finishDrawerBackground(record);
  });
}

function acquireDrawerBackground(target, state) {
  let record = drawerBackgrounds.get(target);
  if (!record) {
    const properties = ['transform-origin', 'transition-property', 'transition-duration', 'transition-timing-function', 'border-radius', 'overflow', 'transform'];
    const saved = properties.map(property => [property, target.style.getPropertyValue(property), target.style.getPropertyPriority(property)]);
    record = {
      target, owners: new Set(), subscribers: new Set(), version: 0, closing: false, restored: false,
      restore(geometryOnly) {
        for (const [property, value, priority] of saved) {
          if (geometryOnly && !['transform', 'border-radius', 'overflow'].includes(property)) continue;
          if (value) target.style.setProperty(property, value, priority); else target.style.removeProperty(property);
        }
      },
    };
    drawerBackgrounds.set(target, record);
    if (scaledBodyOwners++ === 0) {
      savedBodyBackground = document.body.style.getPropertyValue('background');
      savedBodyBackgroundPriority = document.body.style.getPropertyPriority('background');
      document.body.style.background = 'black';
    }
  }
  record.owners.add(state); record.subscribers.add(state);
  state.drawerBackgroundRecords.add(record);
  record.version++; record.closing = false;
  target.style.transformOrigin = 'top'; target.style.transitionProperty = 'transform, border-radius';
  target.style.transitionDuration = matchMedia('(prefers-reduced-motion: reduce)').matches ? '0s' : '.5s';
  target.style.transitionTimingFunction = 'cubic-bezier(.32,.72,0,1)';
  target.style.borderRadius = '8px'; target.style.overflow = 'hidden';
  target.style.transform = `scale(${(innerWidth - 26) / innerWidth}) translate3d(0, calc(env(safe-area-inset-top) + 14px), 0)`;
  return record;
}

function drawer(state) {
  const root = state.root;
  let drag, background;
  state.drawerBackgroundRecords = new Set();
  function scaleBackground() {
    const target = state.settings.open && state.settings.scaleBackground ? document.querySelector('[data-rbi-drawer-background]') : null;
    if (background && background.target !== target) releaseDrawerBackground(background, state);
    if (target) background = acquireDrawerBackground(target, state);
  }
  function paint() {
    if (state.disposed) return;
    scaleBackground();
    const points = state.settings.snapPoints;
    if (points.length) root.style.height = `${Math.min(innerHeight - 96, innerHeight * points[state.settings.index])}px`;
    else root.style.removeProperty('height');
    root.style.removeProperty('translate');
  }
  const down = event => {
    if (!state.settings.open || !state.settings.dismissible || event.button !== 0 || event.target.closest('button,a,input,textarea,select,[contenteditable="true"]') || window.getSelection()?.toString()) return;
    const body = root.querySelector('.rbi-drawer__body');
    if (!event.target.closest('[data-rbi-drawer-handle]') && body.scrollTop > 0) return;
    event.preventDefault();
    drag = { id: event.pointerId, y: event.clientY, height: root.getBoundingClientRect().height, time: performance.now(), delta: 0 };
    root.dataset.rbiDrawerDragging = 'true';
    root.setPointerCapture(event.pointerId);
  };
  const move = event => {
    if (!drag || event.pointerId !== drag.id) return;
    drag.delta = event.clientY - drag.y;
    if (state.settings.snapPoints.length) root.style.height = `${Math.max(0, Math.min(innerHeight - 96, drag.height - drag.delta))}px`;
    else root.style.translate = `0 ${Math.max(0, drag.delta)}px`;
  };
  const release = event => {
    if (!drag || event.pointerId !== drag.id) return;
    const current = drag; drag = null;
    delete root.dataset.rbiDrawerDragging;
    if (root.hasPointerCapture(current.id)) root.releasePointerCapture(current.id);
    if (event.type === 'pointercancel') { paint(); return; }
    const points = state.settings.snapPoints;
    const velocity = current.delta / Math.max(1, performance.now() - current.time);
    if (!points.length) {
      if (current.delta > current.height * .25 || current.delta > 2 && velocity > .4) {
        root.style.setProperty('--rbi-drawer-close-start', `${Math.max(0, current.delta)}px`);
        void invoke(state, 'RequestClose', 'drag').then(paint);
      }
      else paint();
      return;
    }
    const height = current.height - current.delta;
    if (height < innerHeight * points[0] * .5) { void invoke(state, 'RequestClose', 'drag').then(paint); return; }
    const heights = points.map(point => Math.min(innerHeight - 96, innerHeight * point));
    const index = heights.reduce((best, value, index) => Math.abs(value - height) < Math.abs(heights[best] - height) ? index : best, 0);
    void invoke(state, 'NativeDrawerSnap', index).then(paint);
  };
  root.addEventListener('pointerdown', down); root.addEventListener('pointermove', move); root.addEventListener('pointerup', release); root.addEventListener('pointercancel', release);
  window.addEventListener('resize', paint);
  state.sync = paint;
  state.dispose = () => {
    root.removeEventListener('pointerdown', down); root.removeEventListener('pointermove', move); root.removeEventListener('pointerup', release); root.removeEventListener('pointercancel', release); window.removeEventListener('resize', paint);
    if (drag && root.hasPointerCapture(drag.id)) root.releasePointerCapture(drag.id);
    drag = null; delete root.dataset.rbiDrawerDragging; root.style.removeProperty('translate'); root.style.removeProperty('height');
    root.style.removeProperty('--rbi-drawer-close-start');
    for (const record of [...state.drawerBackgroundRecords]) releaseDrawerBackground(record, state, true);
    state.drawerBackgroundRecords.clear(); background = null;
  };
}

function toaster(state) {
  const root = state.root;
  let pointer = false, paused = false, previousFocus, drag, queueEpoch;
  const list = root.querySelector('.rbi-toaster__list');
  let observed = new Set();
  function measure() {
    // A toast in its 200 ms exit keeps the layout it left with; only live toasts take part in the stack.
    const items = [...list.children].filter(item => item.dataset.rbiRemoved !== 'true');
    for (const item of observed) if (!items.includes(item)) observer.unobserve(item);
    let offset = 0;
    for (const [index, item] of items.entries()) {
      if (!observed.has(item)) observer.observe(item);
      item.style.height = 'auto';
      const height = parseFloat(getComputedStyle(item).height);
      item.style.setProperty('--rbi-toast-offset', `${offset}px`);
      item.style.setProperty('--rbi-toast-index', String(index));
      item.style.setProperty('--rbi-toast-height', `${height}px`);
      if (index === 0) root.style.setProperty('--rbi-toast-front-height', `${height}px`);
      item.style.removeProperty('height');
      item.inert = index > 0 && root.dataset.rbiToastExpanded !== 'true';
      offset += height + 14;
    }
    observed = new Set(items);
  }
  const observer = new ResizeObserver(measure);
  const mutation = new MutationObserver(measure);
  mutation.observe(list, { childList: true, subtree: true, characterData: true });
  function syncPause(focused = root.contains(document.activeElement)) {
    const expanded = pointer || focused;
    const value = expanded || document.hidden;
    root.dataset.rbiToastExpanded = expanded ? 'true' : 'false';
    measure();
    if (paused !== value || queueEpoch !== state.settings.queueEpoch) { paused = value; queueEpoch = state.settings.queueEpoch; void invoke(state, 'NativeToastPause', value); }
  }
  const enter = () => { pointer = true; syncPause(); };
  const leave = () => { pointer = false; syncPause(); };
  const focus = () => syncPause();
  const blur = event => {
    if (!root.contains(event.relatedTarget)) {
      // focusout precedes activeElement changing; use its known outside target.
      syncPause(false);
    }
  };
  const key = event => {
    if (event.defaultPrevented) return;
    if (event.altKey && event.code === 'KeyT' && root.querySelector('.rbi-toast')) {
      // Keep an already focused host as owner; otherwise the first listener
      // claims the shortcut. Other hosts must not move focus a second time.
      const focusedHost = document.activeElement?.closest('.rbi-toaster');
      if (focusedHost && focusedHost !== root && controls.has(focusedHost)) return;
      event.preventDefault();
      if (!root.contains(document.activeElement)) previousFocus = document.activeElement;
      root.querySelector('.rbi-toaster__list').focus();
      root.dataset.rbiToastExpanded = 'true';
      measure();
    } else if (event.key === 'Escape' && root.contains(document.activeElement)) {
      event.preventDefault();
      root.dataset.rbiToastExpanded = 'false';
      measure();
    }
  };
  const down = event => {
    const item = event.target.closest('.rbi-toast');
    if (!item || event.button !== 0 || event.target.closest('button,a,input') || window.getSelection()?.toString()) return;
    drag = { item, id: event.pointerId, x: event.clientX, y: event.clientY, start: performance.now(), delta: 0, axis: null };
    item.setPointerCapture(event.pointerId);
  };
  const move = event => {
    if (!drag || event.pointerId !== drag.id) return;
    const x = event.clientX - drag.x, y = event.clientY - drag.y;
    drag.axis ??= Math.abs(x) + Math.abs(y) > 2 ? Math.abs(x) > Math.abs(y) ? 'x' : 'y' : null;
    if (!drag.axis) return;
    drag.delta = Math.max(0, drag.axis === 'x' ? x : y);
    drag.item.style.translate = drag.axis === 'x' ? `${drag.delta}px 0` : `0 ${drag.delta}px`;
  };
  const up = event => {
    if (!drag || event.pointerId !== drag.id) return;
    const { item, id, delta, start } = drag;
    const dismiss = event.type !== 'pointercancel' && delta > 2 && (delta >= 45 || delta / Math.max(1, performance.now() - start) > .11);
    drag = null; item.style.removeProperty('translate');
    if (item.hasPointerCapture(id)) item.releasePointerCapture(id);
    if (dismiss) void invoke(state, 'NativeToastDismiss', item.dataset.rbiToastId);
  };
  const visibility = () => syncPause();
  root.addEventListener('pointerenter', enter); root.addEventListener('pointerleave', leave);
  root.addEventListener('focusin', focus); root.addEventListener('focusout', blur); document.addEventListener('keydown', key);
  document.addEventListener('visibilitychange', visibility);
  root.addEventListener('pointerdown', down); root.addEventListener('pointermove', move); root.addEventListener('pointerup', up); root.addEventListener('pointercancel', up);
  state.sync = syncPause;
  state.dispose = () => {
    root.removeEventListener('pointerenter', enter); root.removeEventListener('pointerleave', leave);
    root.removeEventListener('focusin', focus); root.removeEventListener('focusout', blur); document.removeEventListener('keydown', key);
    document.removeEventListener('visibilitychange', visibility);
    root.removeEventListener('pointerdown', down); root.removeEventListener('pointermove', move); root.removeEventListener('pointerup', up); root.removeEventListener('pointercancel', up);
    observer.disconnect(); mutation.disconnect(); observed.clear();
    if (drag) { drag.item.style.removeProperty('translate'); if (drag.item.hasPointerCapture(drag.id)) drag.item.releasePointerCapture(drag.id); drag = null; }
    if (root.contains(document.activeElement) && previousFocus?.isConnected) previousFocus.focus();
  };
}

export function connectControls(root, reference, kind, settings, connectionId) {
  if (controls.has(root)) return syncControls(root, settings);
  const state = { root, reference, settings, disposed: false, connectionId };
  ({ carousel, command, resize, toaster, drawer, sidebar })[kind](state);
  controls.set(root, state); if (connectionId) connections.set(connectionId, state); state.sync(); root.dataset.rbiControlsReady = 'true';
}
export function syncControls(root, settings) { const state = controls.get(root); if (state && !state.disposed) { state.settings = settings; state.sync(); } }
export function disconnect(root, connectionId) {
  const state = controls.get(root) ?? connections.get(connectionId);
  if (!state) return;
  state.disposed = true; state.dispose(); controls.delete(state.root); connections.delete(state.connectionId); delete state.root.dataset.rbiControlsReady;
}
export function activeConnectionCount() { return connections.size; }
