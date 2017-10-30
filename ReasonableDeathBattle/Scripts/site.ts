﻿// When the document is availible for interaction:
$(document).ready(() => {
    "use strict";
    if (history.pushState) {
        // Find all the links that go places:
        $("a[href]").on("click", tryPartialLoad);
    }    
});
/**
 * Check if a link's destination can be simulated by a partial update of the page, and do so if so
 * @param e The onClick event of an anchor tag
 */
function tryPartialLoad(e: JQuery.Event): void {
    "use strict";
    // Not that the event target can sometimes be a child element of the link
    const originalTarget = $(e.target).closest("a[href]").prop("href") as string;
    if (!originalTarget) { return; }
    let desination: URL;
    try {
        desination = new URL(originalTarget);
    } catch (e) {
        // If this isn't a valid URL, return
        return;
    }
    // If the link doesn't go to a place on this website, skip fancy logic
    if (desination.host !== location.host) { return; }
    // Don't partial load any file links or the atom feed
    if (!originalTarget.endsWith("/") || originalTarget.includes("/feed/")) { return; }
    // Otherwise prevent normal link execution
    e.preventDefault();
    // Add a 'v=m' paramater to the URL, which tells the view model only to send the page main content
    let newTarget = "";
    if (originalTarget.includes("?")) {
        newTarget = originalTarget + "&v=m";
    } else {
        newTarget = originalTarget + "?v=m";
    }
    // Then, fetch that page and load it into the main tag
    partialLoad(newTarget);
    // Add a history entry to mention that we 'changed' pages
    const stateObject = { target: newTarget };
    history.pushState(stateObject, "", originalTarget);
}
/**
 * Replace the main content of the page with the main content from another page
 * @param destination The page to load from
 */
function partialLoad(destination: string) {
    "use strict";
    $("#loading-indicator").show();
    $("#main-content").load(destination, () => {
        $("#loading-indicator").hide();
        // Don't forget to apply the tryPartialLoad function to the links we just loaded!
        $("#main-content a[href]").on("click", tryPartialLoad);
    });
}
// Make sure we read the back and forward buttons correctly
window.onpopstate = (ev) => {
    "use strict";
    // ev.state.target was were we stored the reference to the 'real' page
    let target: string = ev.state && ev.state.target;
    // if it doesn't exist, defualt to the home page
    if (!target) { target = location.origin; }
    partialLoad(target);
};