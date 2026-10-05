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
