﻿(() => {
    "use strict";

    /**
     * Show the user which offline pages they have saved locally
     */
    function setUp(partialLoadEvent: CustomEvent) {
        const offlineList = document.getElementById("offline-nav-items") as HTMLUListElement;

        //If this isn't the offline page, don't show the cached content
        if (!offlineList) { return; }

        if (!("caches" in window)) {
            offlineList.parentElement.innerHTML = "<div class=\"alert alert-warning\" role=\"alert\">" +
                "<p>Unfortunately, JavaScript caching is not available in your browser. " +
                "Please <a class=\"alert-link\" href=\"https://browsehappy.com/\">upgrade your browser</a> " +
                "to cache pages for future offline use.</p></div>";
        } else {
            caches.open("core").then(cache => {
                cache.keys().then((keys: Request[]) => {
                    keys.forEach((request) => {
                        //Filter out non-page navigations
                        if (!request.url.endsWith("/?v=m")) { return; }
                        //TODO other filtering logic
                        const listItem = document.createElement("li");
                        const link = document.createElement("a");
                        let linkDestination = request.url.replace("?v=m", "").replace(`${location.protocol}//${location.host}`, "");
                        link.setAttribute("href", linkDestination);
                        if (linkDestination === "/") { linkDestination = "Home"; }
                        else if (linkDestination === "/offline/") { return; }
                        link.textContent = linkDestination;
                        listItem.appendChild(link);
                        offlineList.appendChild(listItem);
                        link.dispatchEvent(new CustomEvent("LinkAdded", { bubbles: true }));
                    });
                });
            });
        }
    }

    document.addEventListener("DOMContentLoaded", setUp);
    document.getElementById("main-content").addEventListener("ContentModified", setUp);
})();