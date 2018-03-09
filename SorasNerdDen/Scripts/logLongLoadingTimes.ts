///<reference path="definitions/definitions.d.ts" />
(function (w, d) {
    "use strict";
    if (!("performance" in w && "timing" in performance)) { return; }
    /**
     * Time the inital page load, and report any loading times longer than the goal time
     */
    function timeFullPageLoad() {
        const t = performance.timing;
        if ((t.domInteractive - t.navigationStart) > 1500) {
            reportTiming(
                ((t.domInteractive - t.navigationStart) / 1000),
                ((t.loadEventEnd - t.navigationStart) / 1000),
                location.href
            );
        }
    }
    /**
     * Report any partial page loads that took any longer than the goal time
     * @param partialLoadEvent The ContentModified custom event
     */
    function timePartialPageLoad(partialLoadEvent: CustomEvent) {
        const loadDetails = partialLoadEvent.detail as PartialLoadDetails;
        if (loadDetails && loadDetails.loadTime > 1000) {
            reportTiming(
                loadDetails.loadTime / 1000,
                loadDetails.loadTime / 1000,
                loadDetails.destination
            );
        }
    }
    /**
     * Report an unexpectedly long loading time to the server
     * @param interactive The time taken for the DOM structure to render (in seconds)
     * @param total The time taken for the entire page to have loaded (in seconds)
     * @param to The page being navigated to
     */
    function reportTiming(interactive: number, total: number, to: string) {
        const xhr = new XMLHttpRequest();
        xhr.open("POST", "/error/longloadingtime/", true);
        xhr.setRequestHeader('Content-Type', 'application/json; charset=utf-8');
        xhr.send(JSON.stringify({
            "Interactive": interactive.toFixed(2),
            "Total": total.toFixed(2),
            "To": to
        }));
    }
    if (d.readyState === "completed") {
        setTimeout(timeFullPageLoad, 1);
    } else {
        w.addEventListener("load", () => setTimeout(timeFullPageLoad, 1));
    }
    w.whenReady(() => {
        d.getElementById("main-content").addEventListener("ContentModified", timePartialPageLoad);
    });
})(window, document);