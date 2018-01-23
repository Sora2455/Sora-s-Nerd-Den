///<reference path="definitions/definitions.d.ts" />
(function (w) {
    "use strict";
    /**
     * Show the user which offline pages they have saved locally
     */
    function setUp() {
        var offlineList = document.getElementById("offline-nav-items");
        //If this isn't the offline page, don't show the cached content
        if (!offlineList) {
            return;
        }
        if (!("caches" in w)) {
            offlineList.parentElement.innerHTML = "<div class=\"alert alert-warning\" role=\"alert\">" +
                "<p>Unfortunately, JavaScript caching is not available in your browser. " +
                "Please <a class=\"alert-link\" rel=\"external\" href=\"https://browsehappy.com/\">upgrade your browser</a> " +
                "to cache pages for future offline use.</p></div>";
        }
        else {
            caches.open("core").then(function (cache) {
                cache.keys().then(function (keys) {
                    keys.forEach(function (request) {
                        //Filter out non-page navigations
                        if (!request.url.endsWith("/?v=m")) {
                            return;
                        }
                        //TODO other filtering logic
                        var listItem = document.createElement("li");
                        var link = document.createElement("a");
                        var linkDestination = request.url.replace("?v=m", "").replace(location.protocol + "//" + location.host, "");
                        link.setAttribute("href", linkDestination);
                        if (linkDestination === "/") {
                            linkDestination = "Home";
                        }
                        else if (linkDestination === "/offline/") {
                            return;
                        }
                        link.textContent = linkDestination;
                        listItem.appendChild(link);
                        offlineList.appendChild(listItem);
                        link.dispatchEvent(new CustomEvent("LinkAdded", { bubbles: true }));
                    });
                });
            });
        }
    }
    w.whenReady(function () {
        document.getElementById("main-content").addEventListener("ContentModified", setUp);
        setUp();
    });
})(window);
