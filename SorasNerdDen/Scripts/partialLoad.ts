interface NodeList {
    forEach: (callback: (node: Node, i: number, nodeList: NodeList) => void, thisArg?: any) => void;
}
// NodeList.forEach pollyfill
if ('NodeList' in window && !NodeList.prototype.forEach) {
    NodeList.prototype.forEach = function (callback: Function, thisArg: any) {
        "use strict";
        thisArg = thisArg || window;
        for (let i = 0; i < this.length; i++) {
            callback.call(thisArg, this[i], i, this);
        }
    };
}
(() => {
    "use strict";
    // When the document is availible for interaction:
    document.addEventListener("DOMContentLoaded", () => {
        // We require pushState, or else this isn't going to work
        if (history.pushState) {
            // Find all the links that go places:
            document.querySelectorAll("a[href]").forEach((link) => {
                link.addEventListener("click", tryPartialLoad);
            });
            //TODO some check that we are in the loading page
            const newTarget = getPartialUrl(location.toString());
            // Then, fetch that page and load it into the main tag
            partialLoad(newTarget);
        }
    });
    const partialLoadEvent = new CustomEvent("PartialyLoaded", {bubbles: true});
    document.addEventListener("PartialyLoaded", (ev: Event) => {
        (ev.target as Element).querySelectorAll("a[href]").forEach((link) => {
            link.addEventListener("click", tryPartialLoad);
        });
    });
    /**
     * Check if a link's destination can be simulated by a partial update of the page, and do so if so
     * @param e The onClick event of an anchor tag
     */
    function tryPartialLoad(e: Event): void {
        // Not that the event target can sometimes be a child element of the link
        const originalTarget = ((e.target as Element).closest("a[href]") as HTMLAnchorElement).href;
        if (!originalTarget) { return; }
        let desination: URL;
        try {
            desination = new URL(originalTarget);
        } catch (err) {
            // If this isn't a valid URL, return
            return;
        }
        // If the link doesn't go to a place on this website, skip fancy logic
        if (desination.host !== location.host) { return; }
        // Don't partial load any file links or the atom feed
        if (!originalTarget.endsWith("/") || originalTarget.includes("/feed/")) { return; }
        // Otherwise prevent normal link execution
        e.preventDefault();
        const newTarget = getPartialUrl(originalTarget);
        // Then, fetch that page and load it into the main tag
        partialLoad(newTarget);
        // Add a history entry to mention that we 'changed' pages
        const stateObject = { target: newTarget };
        history.pushState(stateObject, "", originalTarget);
    }
    /**
     * Add a 'v=m' paramater to the URL, which tells the view model only to send the page main content
     * @param originalUrl The URL that we want the partial view of
     */
    function getPartialUrl(originalUrl: string): string {
        if (originalUrl.includes("?")) {
            return originalUrl + "&v=m";
        } else {
            return originalUrl + "?v=m";
        }
    }
    /**
     * Replace the main content of the page with the main content from another page
     * @param destination The page to load from
     * @param isOffline True if we are trying to load the offline page
     */
    function partialLoad(destination: string, isOffline?: boolean) {
        document.getElementById("loading-indicator").style.display = "block";
        fetch(destination).then(function (response) {
            document.getElementById("loading-indicator").style.display = "none";

            return response.text().then(function (text) {
                const mainContent = document.getElementById("main-content");
                mainContent.innerHTML = text;
                mainContent.dispatchEvent(partialLoadEvent);
            });
        }).catch(() => {
            // Hide the loading indicator, even on error
            document.getElementById("loading-indicator").style.display = "none";
            // if we got an error, we are most likely offline
            if (!isOffline) { return partialLoad("/offline/?v=m", true); }
        });
    }
    // Make sure we read the back and forward buttons correctly
    window.addEventListener("popstate", (ev) => {
        // ev.state.target was were we stored the reference to the 'real' page
        let target: string = ev.state && ev.state.target;
        // if it doesn't exist, defualt to the home page
        if (!target) { target = location.origin; }
        partialLoad(target);
    });
})();
