// Handy-Seite /remote: Bildschirm wach halten (Screen Wake Lock API) und kurzes Vibrieren als Bestätigung.
window.dartRemote = (function () {
    let lock = null;
    let wanted = false;

    async function request() {
        if (!wanted || !("wakeLock" in navigator) || document.visibilityState !== "visible") {
            return;
        }

        try {
            lock = await navigator.wakeLock.request("screen");
            lock.addEventListener("release", function () { lock = null; });
        } catch (e) {
            // Nicht unterstützt oder verweigert (z. B. Energiesparmodus): Seite funktioniert trotzdem.
        }
    }

    // Der Browser gibt die Sperre beim Wechsel in den Hintergrund frei: beim Zurückkehren neu anfordern.
    document.addEventListener("visibilitychange", function () {
        if (document.visibilityState === "visible" && lock === null) {
            request();
        }
    });

    return {
        keepAwake: function () {
            wanted = true;
            return request();
        },
        release: function () {
            wanted = false;
            if (lock !== null) {
                lock.release();
                lock = null;
            }
        },
        vibrate: function (ms) {
            if ("vibrate" in navigator) {
                navigator.vibrate(ms);
            }
        },
        getToken: function () {
            try { return sessionStorage.getItem("dartmetrics.remote.token"); } catch (e) { return null; }
        },
        setToken: function (token) {
            try {
                if (token) { sessionStorage.setItem("dartmetrics.remote.token", token); }
                else { sessionStorage.removeItem("dartmetrics.remote.token"); }
            } catch (e) { }
        }
    };
})();
