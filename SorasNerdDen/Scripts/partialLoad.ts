﻿///<reference path="definitions/definitions.d.ts" />
((w, d) => {
    "use strict";
    // Element.closest seems to be the 'lowest common function' needed here
    if (!Element.prototype.closest) { return; }
    // When the document is availible for interaction:
    w.whenReady(() => {
        // Find all the links that go places:
        d.querySelectorAll("a[href]").forEach((link) => {
            link.addEventListener("click", tryPartialLoad);
        });
        const mainHeading = d.querySelector("#main-content h1");
        if (mainHeading && mainHeading.textContent === "Loading") {
            //This is the loading page that the Service Worker returns - we need
            //to partial load the page so that the main area matches the location bar
            const newTarget = getPartialUrl(location.toString());
            // Then, fetch that page and load it into the main tag
            partialLoad(newTarget);
        }
        const mainContent = d.getElementById("main-content");
        mainContent.addEventListener("ContentModified", () => {
            mainContent.querySelectorAll("a[href]").forEach((link) => {
                link.addEventListener("click", tryPartialLoad);
            });
        });
    });
    d.addEventListener("LinkAdded", (e: CustomEvent) => {
        e.target.addEventListener("click", tryPartialLoad);
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
        d.getElementById("loading-indicator").style.display = "block";
        const start = Date.now();
        fetch(destination).then(function (response) {
            d.getElementById("loading-indicator").style.display = "none";

            return response.text().then(function (text) {
                const mainContent = d.getElementById("main-content");
                mainContent.innerHTML = text;
                //Dispatch a custom event so that other functions know the page has updated
                const partialLoadDetails: PartialLoadDetails = {
                    loadTime: Date.now() - start, destination: destination
                }; 
                mainContent.dispatchEvent(new CustomEvent("ContentModified", {
                    detail: partialLoadDetails
                }));
                //Close the mobile menu
                (document.getElementById("dropdownSwitch") as HTMLInputElement).checked = false;
                // Remove any existing 'current' markers
                document.querySelectorAll("a[aria-current]")
                    .forEach((a: HTMLAnchorElement) => a.removeAttribute("aria-current"));
                try {
                    const url = new URL(destination);
                    // Mark links to this address as current
                    document.querySelectorAll(`a[href='${url.pathname}']`)
                        .forEach((a: HTMLAnchorElement) => a.setAttribute("aria-current", "page"));
                } catch (e) {}
            });
        }).catch(() => {
            // Hide the loading indicator, even on error
            d.getElementById("loading-indicator").style.display = "none";
            // if we got an error, we are most likely offline
            if (!isOffline) { return partialLoad("/offline/?v=m", true); }
        });
    }
    // Make sure we read the back and forward buttons correctly
    w.addEventListener("popstate", (ev) => {
        // ev.state.target was were we stored the reference to the 'real' page
        let target: string = ev.state && ev.state.target;
        // if it doesn't exist, defualt to the URL
        if (!target) { target = getPartialUrl(location.toString()); }
        partialLoad(target);
    });
})(window, document);
