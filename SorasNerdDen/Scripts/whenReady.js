///<reference path="definitions/definitions.d.ts" />
(function (w) {
    "use strict";
    var readyCallbacks = [];
    var isReady = false;
    w.whenReady = function (callback) {
        if (isReady) {
            setTimeout(callback, 1);
        }
        else {
            readyCallbacks.push(callback);
        }
    };
    function isNowReady() {
        document.removeEventListener("DOMContentLoaded", isNowReady);
        isReady = true;
        var callback;
        while (callback = readyCallbacks.pop()) {
            setTimeout(callback, 1);
        }
    }
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", isNowReady);
    }
    else {
        isNowReady();
    }
})(window);
