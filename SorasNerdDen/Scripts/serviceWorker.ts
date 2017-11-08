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

function fetchAndCache(request: Request, cache: Cache) {
    "use strict";
    return fetch(request.clone()).then(function (response) {
        cache.put(request, response.clone());
        return response;
    });
}

addEventListener("install", function (e: ExtendableEvent) {
    "use strict";
    // Put updated resources in a new cache, so that currently running pages
    // get the current versions.
    e.waitUntil(caches.delete("core-waiting").then(function () {
        return caches.open("core-waiting").then(function (core) {
            const resourceUrls = [
                "/",
                // TODO /offline.html
                "/css/site.css",
                "/css/font-awesome.css",
                "/js/jquery.js",
                "/js/bootstrap.js",
                "/js/site.js"
            ];

            return core.addAll(resourceUrls)
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
    // TODO filter requests

    // Basic read-through caching.
    e.respondWith(
        caches.open("core").then(function (core) {
            return core.match(request).then(function (response) {
                if (response) {
                    return response;
                }

                // we didn't have it in the cache, so add it to the cache and return it
                log("runtime caching:", request.url);

                return fetchAndCache(request, core);
            });
        })
    );
});