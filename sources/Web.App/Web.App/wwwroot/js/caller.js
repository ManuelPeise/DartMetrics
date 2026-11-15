// Caller für Turnier-Matches: sagt die Summe einer Aufnahme an (Web Speech API des Browsers, keine Aufnahmen).
window.dartCaller = {
    isSupported: function () {
        return "speechSynthesis" in window;
    },
    say: function (text) {
        if (!("speechSynthesis" in window)) {
            return;
        }

        // Nicht abbrechen: mehrere Ansagen (Spieler, dann Bot) laufen nacheinander.
        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = "en-GB";
        utterance.rate = 0.95;
        window.speechSynthesis.speak(utterance);
    }
};
