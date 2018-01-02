const log = console.log.bind(console);
const err = console.error.bind(console);
onerror = err;

// Moves the contents of one named cached into another.
function cacheCopy(source: string, destination: string) {
    "use strict";
    return caches.delete(destination).then(function () {
        return Promise.all([
            caches.open(source),
            caches.open(destination)
        ]).then(function (results) {
            const sourceCache = results[0];
            const destCache = results[1];

            return sourceCache.keys().then(function (requests: RequestInfo[]) {
                return Promise.all(requests.map(function (request) {
                    return sourceCache.match(request).then(function (response) {
                        return destCache.put(request, response);
                    });
                }));
            });
        });
    });
}

function fetchAndCache(request: RequestInfo, cache: Cache) {
    "use strict";
    if (!(request instanceof Request)) {
        request = new Request(request);
    }

    return fetch(request.clone()).then(function (response) {
        // if the response came back not okay (like a server error) try and get from cache
        if (!response.ok) { return cache.match(request); }
        // otherwise store the response for future use, and return the response to the client
        cache.put(request, response.clone());
        return response;
    }).catch(() => {
        // if there was an error (almost certainly network touble) try and get from cache
        return cache.match(request);
    });
}

addEventListener("install", function (e: ExtendableEvent) {
    "use strict";
    // Put updated resources in a new cache, so that currently running pages
    // get the current versions.
    e.waitUntil(caches.delete("core-waiting").then(function () {
        return caches.open("core-waiting").then(function (core) {
            const resourceUrls = [
                "/loading/",
                // ?v=m means without the shared view (just the main content)
                "/?v=m",
                "/offline/?v=m",
                "/css/site.css",
                "/css/font-awesome.css",
                "/js/jquery.js",
                "/js/bootstrap.js",
                "/js/site.js"
            ];

            return Promise.all(resourceUrls.map(function (key) {
                // Make sure to download fresh versions of the files!
                return fetch(key, { cache: "no-cache" })
                    .then((response) => core.put(key, response));
            }))
                // Don't wait for the client to refresh the page (as this site is designed not to refresh)
                .then(() => (self as ServiceWorkerGlobalScope).skipWaiting());
        });
    }));
});


addEventListener("activate", function (e: ExtendableEvent) {
    "use strict";
    // Copy the newly installed cache to the active cache
    e.waitUntil(cacheCopy("core-waiting", "core")
        // Declare that we'll be taking over now
        .then(() => (self as ServiceWorkerGlobalScope).clients.claim())
        // Delete the waiting cache afterward to save client memory space
        .then(() => caches.delete("core-waiting")));
});

addEventListener("fetch", function (e: FetchEvent) {
    "use strict";
    const request = e.request;

    // If not a GET request, don't cache
    if (request.method !== "GET") { return fetch(request); }
    // If it's a 'main' page, use the loading page instead
    if (request.url.endsWith("/")) {
        e.respondWith(caches.open("core").then(function (core) {
            // Get the loading page
            return fetchAndCache("/loading/", core);
        }));
        return;
    }
    // TODO filter requests

    // Basic read-through caching.
    e.respondWith(
        caches.open("core").then(function (core) {

            return core.match(request).then(function (response) {
                if (response) { return response; }
                // we didn't have it in the cache, so add it to the cache and return it
                log("runtime caching:", request.url);
                // delete any previous versions that might be in the cache already
                core.delete(request, { ignoreSearch: true });
                // now grab the file and add it to the cache
                return fetchAndCache(request, core);//TODO but what if user is offline on the second time? need site.js?v=1 and only have site.js...
            });
        })
    );
});