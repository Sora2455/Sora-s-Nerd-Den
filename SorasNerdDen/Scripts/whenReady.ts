///<reference path="definitions/definitions.d.ts" />
(function (w, d) {
    "use strict";
    const readyCallbacks = [] as Function[];
    let isReady = false;
    const loadCallbacks = [] as Function[];
    let isLoaded = false;

    w.whenReady = function (callback: Function): void {
        if (isReady) { setTimeout(callback, 1); }
        else { readyCallbacks.push(callback); }
    };
    w.whenLoaded = function (callback: Function): void {
        if (isLoaded) { setTimeout(callback, 1); }
        else { loadCallbacks.push(callback); }
    }

    function isNowReady() {
        d.removeEventListener("DOMContentLoaded", isNowReady);
        isReady = true;
        let callback;
        while (callback = readyCallbacks.pop()) {
            setTimeout(callback, 1);
        }
    }
    function isNowLoaded() {
        w.removeEventListener("load", isNowLoaded);
        isLoaded = true;
        let callback;
        while (callback = loadCallbacks.pop()) {
            setTimeout(callback, 1);
        }
    }
    if (d.readyState === "loading") {
        d.addEventListener("DOMContentLoaded", isNowReady);
    } else {
        isNowReady();
    }
    if (d.readyState !== "complete") {
        w.addEventListener("load", isNowLoaded);
    } else {
        isNowLoaded();
    }
})(window, document);