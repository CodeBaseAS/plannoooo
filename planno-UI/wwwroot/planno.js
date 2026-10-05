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
