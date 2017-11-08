// NodeList.forEach pollyfill
if ('NodeList' in window && !NodeList.prototype.forEach) {
    NodeList.prototype.forEach = function (callback, thisArg) {
        "use strict";
        thisArg = thisArg || window;
        for (var i = 0; i < this.length; i++) {
            callback.call(thisArg, this[i], i, this);
        }
    };
}
(function () {
    "use strict";
    // When the document is availible for interaction:
    document.addEventListener("DOMContentLoaded", function () {
        // We require pushState, or else this isn't going to work
        if (history.pushState) {
            // Find all the links that go places:
            document.querySelectorAll("a[href]").forEach(function (link) {
                link.addEventListener("click", tryPartialLoad);
            });
        }
    });
    var partialLoadEvent = new CustomEvent("PartialyLoaded", { bubbles: true });
    document.addEventListener("PartialyLoaded", function (ev) {
        ev.target.querySelectorAll("a[href]").forEach(function (link) {
            link.addEventListener("click", tryPartialLoad);
        });
    });
    /**
     * Check if a link's destination can be simulated by a partial update of the page, and do so if so
     * @param e The onClick event of an anchor tag
     */
    function tryPartialLoad(e) {
        // Not that the event target can sometimes be a child element of the link
        var originalTarget = e.target.closest("a[href]").href;
        if (!originalTarget) {
            return;
        }
        var desination;
        try {
            desination = new URL(originalTarget);
        }
        catch (err) {
            // If this isn't a valid URL, return
            return;
        }
        // If the link doesn't go to a place on this website, skip fancy logic
        if (desination.host !== location.host) {
            return;
        }
        // Don't partial load any file links or the atom feed
        if (!originalTarget.endsWith("/") || originalTarget.includes("/feed/")) {
            return;
        }
        // Otherwise prevent normal link execution
        e.preventDefault();
        // Add a 'v=m' paramater to the URL, which tells the view model only to send the page main content
        var newTarget = "";
        if (originalTarget.includes("?")) {
            newTarget = originalTarget + "&v=m";
        }
        else {
            newTarget = originalTarget + "?v=m";
        }
        // Then, fetch that page and load it into the main tag
        partialLoad(newTarget);
        // Add a history entry to mention that we 'changed' pages
        var stateObject = { target: newTarget };
        history.pushState(stateObject, "", originalTarget);
    }
    /**
     * Replace the main content of the page with the main content from another page
     * @param destination The page to load from
     */
    function partialLoad(destination) {
        document.getElementById("loading-indicator").style.display = "block";
        fetch(destination).then(function (response) {
            document.getElementById("loading-indicator").style.display = "none";
            return response.text().then(function (text) {
                var mainContent = document.getElementById("main-content");
                mainContent.innerHTML = text;
                mainContent.dispatchEvent(partialLoadEvent);
            });
        }).catch(function () {
            // Hide the loading indicator, even on error
            document.getElementById("loading-indicator").style.display = "none";
        });
    }
    // Make sure we read the back and forward buttons correctly
    window.addEventListener("popstate", function (ev) {
        // ev.state.target was were we stored the reference to the 'real' page
        var target = ev.state && ev.state.target;
        // if it doesn't exist, defualt to the home page
        if (!target) {
            target = location.origin;
        }
        partialLoad(target);
    });
})();
