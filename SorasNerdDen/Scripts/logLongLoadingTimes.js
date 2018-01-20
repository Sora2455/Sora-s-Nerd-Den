///<reference path="definitions/definitions.d.ts" />
(function () {
    "use strict";
    if (!("performance" in window && "timing" in performance)) {
        return;
    }
    /**
     * Time the inital page load, and report any loading times longer than the goal time
     */
    function timeFullPageLoad() {
        var t = performance.timing;
        if ((t.domInteractive - t.navigationStart) > 1500) {
            reportTiming(((t.domInteractive - t.navigationStart) / 1000), ((t.loadEventEnd - t.navigationStart) / 1000), location.href);
        }
    }
    /**
     * Report any partial page loads that took any longer than the goal time
     * @param partialLoadEvent The ContentModified custom event
     */
    function timePartialPageLoad(partialLoadEvent) {
        var loadDetails = partialLoadEvent.detail;
        if (loadDetails && loadDetails.loadTime > 1000) {
            reportTiming(loadDetails.loadTime / 1000, loadDetails.loadTime / 1000, loadDetails.destination);
        }
    }
    /**
     * Report an unexpectedly long loading time to the server
     * @param interactive The time taken for the DOM structure to render (in seconds)
     * @param total The time taken for the entire page to have loaded (in seconds)
     * @param to The page being navigated to
     */
    function reportTiming(interactive, total, to) {
        var xhr = new XMLHttpRequest();
        xhr.open("POST", "/error/longloadingtime/", true);
        xhr.setRequestHeader('Content-Type', 'application/json; charset=utf-8');
        xhr.send(JSON.stringify({
            "Interactive": interactive.toFixed(2),
            "Total": total.toFixed(2),
            "To": to
        }));
    }
    window.addEventListener("load", function () { return setTimeout(timeFullPageLoad, 1); });
    document.getElementById("main-content").addEventListener("ContentModified", timePartialPageLoad);
})();
