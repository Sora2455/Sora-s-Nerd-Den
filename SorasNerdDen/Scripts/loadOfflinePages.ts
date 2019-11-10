///<reference path="definitions/definitions.d.ts" />
((w, d) => {
    "use strict";

    /**
     * Show the user which offline pages they have saved locally
     */
    function setUp() {
        const offlineList = d.getElementById("offline-nav-items") as HTMLUListElement;

        //If this isn't the offline page, don't show the cached content
        if (!offlineList) { return; }

        if (!w.caches) {
            offlineList.parentElement.innerHTML = "<div class=\"alert alert-warning\" role=\"alert\">" +
                "<p>Unfortunately, JavaScript caching is not available in your browser. " +
                "Please <a class=\"alert-link\" rel=\"external\" " +
                        "href=\"http://outdatedbrowser.com/\">upgrade your browser</a> " +
                "to cache pages for future offline use.</p></div>";
        } else {
            caches.open("core").then(cache => {
                w.dbReady.then(() => {
                    cache.keys().then((keys: Request[]) => {
                        const cachedPagesData = [] as Promise<PageTitleAndDescription>[];
                        keys.forEach((request) => {
                            //Filter out non-page navigations
                            if (!request.url.endsWith("/?v=m")) { return; }
                            //TODO other filtering logic
                            let linkDestination = request.url.replace("?v=m", "")
                                .replace(`${location.protocol}//${location.host}`, "");
                            if (linkDestination === "/offline/") { return; }
                            cachedPagesData.push(
                                w.retrieveJsonData("pageDetails", linkDestination).then((page: PageTitleAndDescription) => {
                                    return page || { url: linkDestination } as PageTitleAndDescription;
                                })
                            );
                        });
                        Promise.all(cachedPagesData).then((cachedPages) => {
                            cachedPages.sort((a, b) => {
                                if (a.url < b.url) return -1;
                                if (a.url > b.url) return 1;
                                return 0;
                            });
                            cachedPages.forEach((page) => {
                                const listItem = d.createElement("li");
                                const link = d.createElement("a");
                                link.setAttribute("href", page.url);
                                link.textContent = page.title || page.url;
                                listItem.appendChild(link);
                                offlineList.appendChild(listItem);
                                link.dispatchEvent(new CustomEvent("LinkAdded", { bubbles: true }));
                            });
                            //Remove the 'Loading' line item
                            offlineList.removeChild(offlineList.firstChild);
                            //If no pages appear in the cache
                            if (!cachedPages.length) {
                                const listItem = d.createElement("li");
                                listItem.textContent = "Unfortunately, you have no pages in cache";
                                offlineList.appendChild(listItem);
                            }
                        });
                    });
                })
            });
        }
    }
    w.whenReady(() => {
        d.getElementById("main-content").addEventListener("ContentModified", setUp);
        setUp();
    });
})(window, document);