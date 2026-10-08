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
const SNAP = 15;            // Minuten pro Raster-Schritt
const MIN_PX = 48 / 60;     // 48 px pro Stunde
const clamp = (v, lo, hi) => Math.min(Math.max(v, lo), hi);

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
 *  - Klick auf freie Fläche   -> Termin über 1 Stunde (OnCreateRange)
 *  - Klicken + Ziehen         -> beliebig langer Zeitraum (OnCreateRange), Vorschau während des Ziehens
 *  - Klick auf einen Termin   -> OnEventClick
 *  - Termin ziehen            -> OnMoveTimed (andere Uhrzeit und/oder anderer Tag)
 */
export function registerGrid(grid, dotNet) {
    let state = null;

    const columnAt = (x) => Array.from(grid.querySelectorAll(".day-col")).find((c) => {
        const r = c.getBoundingClientRect();
        return x >= r.left && x < r.right;
    });
    const minutesAt = (col, y) => (y - col.getBoundingClientRect().top) / MIN_PX;

    function finish() {
        if (!state) return;
        state.ghost?.remove();
        state.box?.classList.remove("dim");
        grid.classList.remove("dragging");
        if (grid.hasPointerCapture(state.pointerId)) grid.releasePointerCapture(state.pointerId);
        state = null;
    }

    function updateCreate(e) {
        const slot = clamp(Math.floor(minutesAt(state.col, e.clientY) / SNAP) * SNAP, 0, 1440 - SNAP);
        const start = Math.min(state.anchor, slot);
        const end = Math.max(state.anchor, slot) + SNAP;
        state.range = [start, end];

        const g = state.ghost;
        g.style.top = `${start * MIN_PX}px`;
        g.style.height = `${Math.max((end - start) * MIN_PX - 2, 18)}px`;
        g.classList.toggle("compact", end - start < 60);
        g.querySelector(".event-time").textContent = timeText(start, end, end - start < 60);
    }

    function updateMove(e) {
        const target = columnAt(e.clientX) ?? state.target;
        state.target = target;

        const start = clamp(Math.round((minutesAt(target, e.clientY) - state.grab) / SNAP) * SNAP, 0, 1440 - state.length);
        state.start = start;

        const g = state.ghost;
        if (g.parentElement !== target) target.appendChild(g);
        g.style.top = `${start * MIN_PX}px`;
        g.style.height = `${Math.max(state.length * MIN_PX - 2, 18)}px`;
        const t = g.querySelector(".event-time");
        if (t) t.textContent = timeText(start, start + state.length, g.classList.contains("compact"));
    }

    grid.addEventListener("pointerdown", (e) => {
        if (e.button !== 0 || state) return;
        const col = e.target.closest(".day-col");
        if (!col) return;

        const box = e.target.closest(".event-box[data-id]");
        if (box) {
            const rect = box.getBoundingClientRect();
            state = {
                kind: "move", pointerId: e.pointerId, box, col, target: col,
                id: Number(box.dataset.id),
                start: Number(box.dataset.start),
                length: Number(box.dataset.end) - Number(box.dataset.start),
                grab: (e.clientY - rect.top) / MIN_PX,
                x: e.clientX, y: e.clientY, moved: false, ghost: null,
            };
        } else {
            const anchor = clamp(Math.floor(minutesAt(col, e.clientY) / SNAP) * SNAP, 0, 1440 - SNAP);
            state = {
                kind: "create", pointerId: e.pointerId, col, anchor,
                range: [anchor, Math.min(anchor + 60, 1440)],   // nur klicken = 1 Stunde
                x: e.clientX, y: e.clientY, moved: false, ghost: null,
            };
        }

        grid.setPointerCapture(e.pointerId);
        e.preventDefault();
    });

    grid.addEventListener("pointermove", (e) => {
        if (!state || e.pointerId !== state.pointerId) return;

        if (!state.moved) {
            if (Math.hypot(e.clientX - state.x, e.clientY - state.y) < 5) return;
            state.moved = true;
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
        }

        if (state.kind === "move") updateMove(e); else updateCreate(e);
    });

    grid.addEventListener("pointerup", (e) => {
        if (!state || e.pointerId !== state.pointerId) return;
        const s = state;
        finish();

        if (s.kind === "create") {
            dotNet.invokeMethodAsync("OnCreateRange", s.col.dataset.date, s.range[0], s.range[1]);
        } else if (!s.moved) {
            dotNet.invokeMethodAsync("OnEventClick", s.id);
        } else {
            dotNet.invokeMethodAsync("OnMoveTimed", s.id, s.target.dataset.date, s.start);
        }
    });

    grid.addEventListener("pointercancel", finish);

    // Escape bricht das Ziehen ab
    const onKey = (e) => {
        if (!grid.isConnected) { document.removeEventListener("keydown", onKey); return; }
        if (e.key === "Escape" && state) finish();
    };
    document.addEventListener("keydown", onKey);
}

/**
 * Termine auf einen anderen Tag ziehen (ganztägige Chips in der Wochenansicht, Einträge im Monat).
 * Klick ohne Ziehen -> OnEventClick, Loslassen auf einem Tag -> OnMoveToDay.
 */
export function registerDayDrag(root, dotNet, itemSelector, targetSelector) {
    let state = null;

    const targetAt = (x, y) => document.elementFromPoint(x, y)?.closest(targetSelector) ?? null;

    function finish() {
        if (!state) return;
        state.item.classList.remove("dim");
        state.target?.classList.remove("drop-target");
        root.classList.remove("dragging");
        if (root.hasPointerCapture(state.pointerId)) root.releasePointerCapture(state.pointerId);
        state = null;
    }

    root.addEventListener("pointerdown", (e) => {
        if (e.button !== 0 || state) return;
        const item = e.target.closest(itemSelector);
        if (!item) return;

        state = { item, id: Number(item.dataset.id), pointerId: e.pointerId, x: e.clientX, y: e.clientY, moved: false, target: null };
        root.setPointerCapture(e.pointerId);
        e.preventDefault();
    });

    root.addEventListener("pointermove", (e) => {
        if (!state || e.pointerId !== state.pointerId) return;

        if (!state.moved) {
            if (Math.hypot(e.clientX - state.x, e.clientY - state.y) < 5) return;
            state.moved = true;
            state.item.classList.add("dim");
            root.classList.add("dragging");
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

        if (!s.moved) dotNet.invokeMethodAsync("OnEventClick", s.id);
        else if (date) dotNet.invokeMethodAsync("OnMoveToDay", s.id, date);
    });

    root.addEventListener("pointercancel", finish);
}
