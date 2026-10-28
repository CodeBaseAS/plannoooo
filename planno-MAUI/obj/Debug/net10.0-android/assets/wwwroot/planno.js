// Gemeinsames JS-Modul für Planno (liegt in wwwroot, Import: "./planno.js")

export function scrollTo(element, top) {
    if (element) element.scrollTop = top;
}

// ---------- Tastenkürzel ----------
let keyHandler = null;

export function registerShortcuts(dotNet) {
    unregisterShortcuts();
    keyHandler = (e) => {
        if (e.ctrlKey || e.metaKey || e.altKey || e.repeat) return;
        const t = e.target;
        if (t && (t.tagName === "INPUT" || t.tagName === "TEXTAREA" || t.isContentEditable)) return;
        if (t && t.closest && t.closest("[data-no-shortcuts]")) return;   // z. B. im Termin-Fenster
        dotNet.invokeMethodAsync("OnShortcut", e.key);
    };
    document.addEventListener("keydown", keyHandler);
}

export function unregisterShortcuts() {
    if (keyHandler) document.removeEventListener("keydown", keyHandler);
    keyHandler = null;
}

// ---------- Seitenweises Blättern per Mausrad ----------
export function registerWheelPaging(element, dotNet) {
    let locked = false;
    let delta = 0;
    let resetTimer, unlockTimer;

    // Touch: horizontal wischen blättert
    let swipe = null;
    element.addEventListener("pointerdown", (e) => {
        if (e.pointerType === "touch" && !e.target.closest("[data-id]")) swipe = { id: e.pointerId, x: e.clientX, y: e.clientY };
    });
    element.addEventListener("pointerup", (e) => {
        if (!swipe || e.pointerId !== swipe.id) return;
        const dx = e.clientX - swipe.x;
        const dy = e.clientY - swipe.y;
        swipe = null;
        if (Math.abs(dx) > 60 && Math.abs(dx) > Math.abs(dy) * 1.5) {
            dotNet.invokeMethodAsync("Page", dx < 0 ? 1 : -1);
        }
    });
    element.addEventListener("pointercancel", () => { swipe = null; });

    element.addEventListener("wheel", (e) => {
        if (e.ctrlKey) return;                    // Browser-Zoom nicht blockieren
        e.preventDefault();

        // Nach dem Blättern erst wieder freigeben, wenn das Trägheits-Scrollen
        // des Trackpads ausgelaufen ist (200 ms ohne Wheel-Event)
        if (locked) {
            clearTimeout(unlockTimer);
            unlockTimer = setTimeout(() => { locked = false; delta = 0; }, 200);
            return;
        }

        // Dominante Achse zählt: Mausrad (vertikal) oder Trackpad-Wischen (horizontal)
        const raw = Math.abs(e.deltaX) > Math.abs(e.deltaY) ? e.deltaX : e.deltaY;
        delta += e.deltaMode === 1 ? raw * 16 : raw;
        clearTimeout(resetTimer);
        resetTimer = setTimeout(() => { delta = 0; }, 150);

        if (Math.abs(delta) < 50) return;

        const direction = delta > 0 ? 1 : -1;
        delta = 0;
        locked = true;
        unlockTimer = setTimeout(() => { locked = false; }, 200);
        dotNet.invokeMethodAsync("Page", direction);
    }, { passive: false });
}

// ---------- Fenster verschieben (Termin-Fenster) ----------
export function registerDrag(handle, dialog) {
    let startX = 0, startY = 0, originLeft = 0, originTop = 0, dragging = false;

    handle.addEventListener("pointerdown", (e) => {
        if (e.button !== 0) return;
        const r = dialog.getBoundingClientRect();
        originLeft = r.left; originTop = r.top;
        startX = e.clientX; startY = e.clientY;
        dragging = true;

        // von der CSS-Startposition auf feste Pixelwerte umstellen
        dialog.style.left = r.left + "px";
        dialog.style.top = r.top + "px";

        handle.setPointerCapture(e.pointerId);
        e.preventDefault();
    });

    handle.addEventListener("pointermove", (e) => {
        if (!dragging) return;
        const w = dialog.offsetWidth;
        // Mindestens 80 px bleiben sichtbar, die Kopfzeile bleibt im Fenster
        const left = Math.min(Math.max(originLeft + e.clientX - startX, 80 - w), window.innerWidth - 80);
        const top = Math.min(Math.max(originTop + e.clientY - startY, 0), window.innerHeight - 48);
        dialog.style.left = left + "px";
        dialog.style.top = top + "px";
    });

    const end = (e) => {
        dragging = false;
        if (handle.hasPointerCapture(e.pointerId)) handle.releasePointerCapture(e.pointerId);
    };
    handle.addEventListener("pointerup", end);
    handle.addEventListener("pointercancel", end);
}

// ---------- Zeit-Liste: gewählten Eintrag nach oben scrollen ----------
export function scrollSelectedToTop(list) {
    if (!list) return;
    const selected = list.querySelector(".selected");
    if (selected) list.scrollTop = selected.offsetTop;
}

// ---------- Termine im Zeitraster: erstellen (klicken/ziehen) und verschieben ----------
const SNAP = 15;               // Minuten pro Raster-Schritt
const MIN_PX = 48 / 60;        // 48 px pro Stunde
const LONG_PRESS_MS = 350;     // Touch: so lange halten, bis das Ziehen beginnt
const MOVE_TOLERANCE = 10;     // Touch: so weit darf der Finger beim Halten wandern (sonst wird gescrollt)
const clamp = (v, lo, hi) => Math.min(Math.max(v, lo), hi);

function vibrate() {
    try { navigator.vibrate?.(15); } catch { /* nicht überall erlaubt */ }
}

export function timeLabel(minutes) {
    minutes = ((minutes % 1440) + 1440) % 1440;
    const h = Math.floor(minutes / 60);
    const m = minutes % 60;
    const h12 = h % 12 === 0 ? 12 : h % 12;
    return `${h12}:${String(m).padStart(2, "0")}${h < 12 ? "AM" : "PM"}`;
}

const timeText = (start, end, compact) =>
    compact ? timeLabel(start) : `${timeLabel(start)} – ${timeLabel(end)}`;

/**
 * Wochen-/Tagesraster:
 *  - Klick/Tipp auf freie Fläche  -> Termin über 1 Stunde (OnCreateRange)
 *  - Klicken + Ziehen (Maus)      -> beliebig langer Zeitraum, Vorschau während des Ziehens
 *  - Klick/Tipp auf einen Termin  -> OnEventClick
 *  - Termin ziehen (Maus)         -> OnMoveTimed (andere Uhrzeit und/oder anderer Tag)
 *  - Touch: erst LANG DRÜCKEN, dann ziehen (sonst wird normal gescrollt)
 *  - Nahe am oberen/unteren Rand scrollt das Raster beim Ziehen automatisch
 */
export function registerGrid(grid, dotNet) {
    let state = null;
    let raf = 0;

    const scroller = grid.closest(".week-scroll");
    const columnAt = (x) => Array.from(grid.querySelectorAll(".day-col")).find((c) => {
        const r = c.getBoundingClientRect();
        return x >= r.left && x < r.right;
    });
    const minutesAt = (col, y) => (y - col.getBoundingClientRect().top) / MIN_PX;

    function stopAutoScroll() {
        cancelAnimationFrame(raf);
        raf = 0;
    }

    function autoScrollStep() {
        raf = 0;
        if (!state || !state.dragging || !scroller) return;

        const area = scroller.getBoundingClientRect();
        const head = scroller.querySelector(".week-head");
        const top = (head ? head.getBoundingClientRect().bottom : area.top) + 24;
        const bottom = area.bottom - 40;

        let dy = 0;
        if (state.lastY < top) dy = -Math.min(18, (top - state.lastY) / 4 + 2);
        else if (state.lastY > bottom) dy = Math.min(18, (state.lastY - bottom) / 4 + 2);

        if (dy !== 0) {
            scroller.scrollTop += dy;
            update(state.lastX, state.lastY);
        }
        raf = requestAnimationFrame(autoScrollStep);
    }

    function finish() {
        if (!state) return;
        clearTimeout(state.timer);
        state.ghost?.remove();
        state.box?.classList.remove("dim");
        grid.classList.remove("dragging");
        stopAutoScroll();
        try {
            if (grid.hasPointerCapture(state.pointerId)) grid.releasePointerCapture(state.pointerId);
        } catch { /* Pointer existiert nicht mehr */ }
        state = null;
    }

    function renderCreate() {
        const [start, end] = state.range;
        const g = state.ghost;
        g.style.top = `${start * MIN_PX}px`;
        g.style.height = `${Math.max((end - start) * MIN_PX - 2, 18)}px`;
        g.classList.toggle("compact", end - start < 60);
        g.querySelector(".event-time").textContent = timeText(start, end, end - start < 60);
    }

    function updateCreate(x, y) {
        const slot = clamp(Math.floor(minutesAt(state.col, y) / SNAP) * SNAP, 0, 1440 - SNAP);
        state.range = [Math.min(state.anchor, slot), Math.max(state.anchor, slot) + SNAP];
        renderCreate();
    }

    function updateMove(x, y) {
        const target = columnAt(x) ?? state.target;
        state.target = target;

        const start = clamp(Math.round((minutesAt(target, y) - state.grab) / SNAP) * SNAP, 0, 1440 - state.length);
        state.start = start;

        const g = state.ghost;
        if (g.parentElement !== target) target.appendChild(g);
        g.style.top = `${start * MIN_PX}px`;
        g.style.height = `${Math.max(state.length * MIN_PX - 2, 18)}px`;
        const t = g.querySelector(".event-time");
        if (t) t.textContent = timeText(start, start + state.length, g.classList.contains("compact"));
    }

    const update = (x, y) => (state.kind === "move" ? updateMove(x, y) : updateCreate(x, y));

    function beginDrag() {
        state.dragging = true;
        grid.classList.add("dragging");

        if (state.kind === "move") {
            const g = state.box.cloneNode(true);
            g.removeAttribute("data-id");
            g.classList.add("ghost");
            g.style.left = "2px";
            g.style.width = "calc(100% - 4px)";
            state.box.classList.add("dim");
            state.ghost = g;
        } else {
            const g = document.createElement("div");
            g.className = "event-box draft";
            g.style.left = "2px";
            g.style.width = "calc(100% - 4px)";
            g.innerHTML = '<div class="event-title">(Kein Titel)</div><div class="event-time"></div>';
            state.col.appendChild(g);
            state.ghost = g;
        }

        if (!raf) raf = requestAnimationFrame(autoScrollStep);
    }

    grid.addEventListener("pointerdown", (e) => {
        if (e.button !== 0 || state) return;
        const col = e.target.closest(".day-col");
        if (!col) return;

        const touch = e.pointerType === "touch";
        const box = e.target.closest(".event-box[data-id]");

        if (box) {
            const rect = box.getBoundingClientRect();
            state = {
                kind: "move", box, col, target: col,
                id: Number(box.dataset.id),
                start: Number(box.dataset.start),
                length: Number(box.dataset.end) - Number(box.dataset.start),
                grab: (e.clientY - rect.top) / MIN_PX,
            };
        } else {
            const anchor = clamp(Math.floor(minutesAt(col, e.clientY) / SNAP) * SNAP, 0, 1440 - SNAP);
            state = {
                kind: "create", col, anchor,
                range: [anchor, Math.min(anchor + 60, 1440)],   // nur klicken/tippen = 1 Stunde
            };
        }

        Object.assign(state, {
            pointerId: e.pointerId, touch, dragging: false, ghost: null, timer: 0,
            x: e.clientX, y: e.clientY, lastX: e.clientX, lastY: e.clientY,
        });

        if (touch) {
            // Erst nach langem Drücken beginnt das Ziehen – vorher darf der Finger normal scrollen
            const s = state;
            s.timer = setTimeout(() => {
                if (state !== s) return;
                vibrate();
                try { grid.setPointerCapture(s.pointerId); } catch { /* ignorieren */ }
                beginDrag();
                if (s.kind === "move") update(s.lastX, s.lastY); else renderCreate();
            }, LONG_PRESS_MS);
        } else {
            grid.setPointerCapture(e.pointerId);
            e.preventDefault();
        }
    });

    grid.addEventListener("pointermove", (e) => {
        if (!state || e.pointerId !== state.pointerId) return;
        state.lastX = e.clientX;
        state.lastY = e.clientY;

        if (!state.dragging) {
            const distance = Math.hypot(e.clientX - state.x, e.clientY - state.y);
            if (state.touch) {
                // Finger bewegt sich vor dem langen Drücken -> der Nutzer scrollt, wir übernehmen die Geste nicht
                if (distance > MOVE_TOLERANCE) {
                    clearTimeout(state.timer);
                    state = null;
                }
                return;
            }
            if (distance < 5) return;
            beginDrag();
        }

        update(e.clientX, e.clientY);
    });

    grid.addEventListener("pointerup", (e) => {
        if (!state || e.pointerId !== state.pointerId) return;
        const s = state;
        finish();

        if (s.kind === "create") {
            dotNet.invokeMethodAsync("OnCreateRange", s.col.dataset.date, s.range[0], s.range[1]);
        } else if (!s.dragging) {
            dotNet.invokeMethodAsync("OnEventClick", s.id);
        } else {
            dotNet.invokeMethodAsync("OnMoveTimed", s.id, s.target.dataset.date, s.start);
        }
    });

    grid.addEventListener("pointercancel", finish);

    // Beim Ziehen mit dem Finger darf die Seite nicht mitscrollen
    grid.addEventListener("touchmove", (e) => {
        if (state?.touch && state.dragging) e.preventDefault();
    }, { passive: false });

    // Langes Drücken würde sonst das Kontextmenü öffnen
    grid.addEventListener("contextmenu", (e) => {
        if (state?.touch) e.preventDefault();
    });

    // Escape bricht das Ziehen ab
    const onKey = (e) => {
        if (!grid.isConnected) { document.removeEventListener("keydown", onKey); return; }
        if (e.key === "Escape" && state) finish();
    };
    document.addEventListener("keydown", onKey);
}

/**
 * Termine auf einen anderen Tag ziehen (ganztägige Chips in der Wochenansicht, Einträge im Monat).
 * Tipp/Klick -> OnEventClick, Loslassen auf einem Tag -> OnMoveToDay.
 * Touch: erst lang drücken, dann ziehen.
 */
export function registerDayDrag(root, dotNet, itemSelector, targetSelector) {
    let state = null;

    const targetAt = (x, y) => document.elementFromPoint(x, y)?.closest(targetSelector) ?? null;

    function finish() {
        if (!state) return;
        clearTimeout(state.timer);
        state.item.classList.remove("dim");
        state.target?.classList.remove("drop-target");
        root.classList.remove("dragging");
        try {
            if (root.hasPointerCapture(state.pointerId)) root.releasePointerCapture(state.pointerId);
        } catch { /* Pointer existiert nicht mehr */ }
        state = null;
    }

    function begin() {
        state.dragging = true;
        state.item.classList.add("dim");
        root.classList.add("dragging");
    }

    root.addEventListener("pointerdown", (e) => {
        if (e.button !== 0 || state) return;
        const item = e.target.closest(itemSelector);
        if (!item) return;

        const touch = e.pointerType === "touch";
        state = {
            item, id: Number(item.dataset.id), pointerId: e.pointerId, touch,
            x: e.clientX, y: e.clientY, dragging: false, target: null, timer: 0,
        };

        if (touch) {
            const s = state;
            s.timer = setTimeout(() => {
                if (state !== s) return;
                vibrate();
                try { root.setPointerCapture(s.pointerId); } catch { /* ignorieren */ }
                begin();
            }, LONG_PRESS_MS);
        } else {
            root.setPointerCapture(e.pointerId);
            e.preventDefault();
        }
    });

    root.addEventListener("pointermove", (e) => {
        if (!state || e.pointerId !== state.pointerId) return;

        if (!state.dragging) {
            const distance = Math.hypot(e.clientX - state.x, e.clientY - state.y);
            if (state.touch) {
                if (distance > MOVE_TOLERANCE) {
                    clearTimeout(state.timer);
                    state = null;
                }
                return;
            }
            if (distance < 5) return;
            begin();
        }

        const target = targetAt(e.clientX, e.clientY);
        if (target !== state.target) {
            state.target?.classList.remove("drop-target");
            target?.classList.add("drop-target");
            state.target = target;
        }
    });

    root.addEventListener("pointerup", (e) => {
        if (!state || e.pointerId !== state.pointerId) return;
        const s = state;
        const date = s.target?.dataset.date;
        finish();

        if (!s.dragging) dotNet.invokeMethodAsync("OnEventClick", s.id);
        else if (date) dotNet.invokeMethodAsync("OnMoveToDay", s.id, date);
    });

    root.addEventListener("pointercancel", finish);

    root.addEventListener("touchmove", (e) => {
        if (state?.touch && state.dragging) e.preventDefault();
    }, { passive: false });

    root.addEventListener("contextmenu", (e) => {
        if (state?.touch) e.preventDefault();
    });
}

// ---------- Kleine Bildschirme (Handy) ----------
export function isCompact() {
    return window.matchMedia("(max-width: 800px)").matches;
}
