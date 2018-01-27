///<reference path="definitions/definitions.d.ts" />
((w) => {
    "use strict";

    /**
     * Show the user which offline pages they have saved locally
     */
    function setUp() {
        const offlineList = document.getElementById("offline-nav-items") as HTMLUListElement;

        //If this isn't the offline page, don't show the cached content
        if (!offlineList) { return; }

        if (!("caches" in w)) {
            offlineList.parentElement.innerHTML = "<div class=\"alert alert-warning\" role=\"alert\">" +
                "<p>Unfortunately, JavaScript caching is not available in your browser. " +
                "Please <a class=\"alert-link\" rel=\"external\" href=\"https://outdatedbrowser.com/\">upgrade your browser</a> " +
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
                        if (linkDestination === "/offline/") { return; }
                        let titleJSON = localStorage.getItem(linkDestination);
                        if (titleJSON) {
                            const titleAndSecription = JSON.parse(titleJSON) as PageTitleAndDescription;
                            link.textContent = titleAndSecription.title;
                        } else {
                            link.textContent = linkDestination;
                        }
                        listItem.appendChild(link);
                        offlineList.appendChild(listItem);
                        link.dispatchEvent(new CustomEvent("LinkAdded", { bubbles: true }));
                    });
                });
            });
        }
    }
    w.whenReady(() => {
        document.getElementById("main-content").addEventListener("ContentModified", setUp);
        setUp();
    });
})(window);