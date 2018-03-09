///<reference path="definitions/definitions.d.ts" />
(function (w, d) {
    "use strict";
    const readyCallbacks = [] as Function[];
    let isReady = false;

    w.whenReady = function (callback: Function): void {
        if (isReady) { setTimeout(callback, 1); }
        else { readyCallbacks.push(callback); }
    };

    function isNowReady() {
        d.removeEventListener("DOMContentLoaded", isNowReady);
        isReady = true;
        let callback;
        while (callback = readyCallbacks.pop()) {
            setTimeout(callback, 1);
        }
    }

    if (d.readyState === "loading") {
        d.addEventListener("DOMContentLoaded", isNowReady);
    } else {
        isNowReady();
    }
})(window, document);