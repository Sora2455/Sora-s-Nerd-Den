var log = console.log.bind(console);
var err = console.error.bind(console);
onerror = err;
// Moves the contents of one named cached into another.
function cacheCopy(source, destination) {
    "use strict";
    return caches.delete(destination).then(function () {
        return Promise.all([
            caches.open(source),
            caches.open(destination)
        ]).then(function (results) {
            var sourceCache = results[0];
            var destCache = results[1];
            return sourceCache.keys().then(function (requests) {
                return Promise.all(requests.map(function (request) {
                    return sourceCache.match(request).then(function (response) {
                        return destCache.put(request, response);
                    });
                }));
            });
        });
    });
}
function fetchAndCache(request, cache, versioned) {
    "use strict";
    if (!(request instanceof Request)) {
        request = new Request(request);
    }
    //TODO handle 404s intelligently
    return fetch(request.clone(), { mode: "no-cors" }).then(function (response) {
        // if the response came back as a server error try and get from cache
        if (response.status === 500) {
            return findInCache(request, cache, versioned);
        }
        // otherwise delete any previous versions that might be in the cache already (if a versioned file),
        if (versioned) {
            cache.delete(request, { ignoreSearch: true });
        }
        // then store the response for future use, and return the response to the client
        cache.put(request, response.clone());
        return response;
    }).catch(function () {
        // if there was an error (almost certainly network touble) try and get from cache
        return findInCache(request, cache, versioned);
    });
}
function findInCache(request, cache, versioned) {
    "use strict";
    return cache.match(request).then(function (result) {
        if (result || !versioned) {
            return result;
        }
        return cache.match(request, { ignoreSearch: true });
    });
}
addEventListener("install", function (e) {
    "use strict";
    // Put updated resources in a new cache, so that currently running pages
    // get the current versions.
    e.waitUntil(caches.delete("core-waiting").then(function () {
        return caches.open("core-waiting").then(function (core) {
            var resourceUrls = [
                "/loading/",
                // ?v=m means without the shared view (just the main content)
                "/?v=m",
                "/offline/?v=m",
                "/css/site.css",
                "/css/font-awesome.css",
                "/js/site.js"
            ];
            return Promise.all(resourceUrls.map(function (key) {
                // Make sure to download fresh versions of the files!
                return fetch(key, { cache: "no-cache" })
                    .then(function (response) { return core.put(key, response); });
            }))
                .then(function () { return self.skipWaiting(); });
        });
    }));
});
addEventListener("activate", function (e) {
    "use strict";
    // Copy the newly installed cache to the active cache
    e.waitUntil(cacheCopy("core-waiting", "core")
        .then(function () { return self.clients.claim(); })
        .then(function () { return caches.delete("core-waiting"); }));
});
addEventListener("fetch", function (e) {
    "use strict";
    var request = e.request;
    // If not a GET request, don't cache
    if (request.method !== "GET") {
        return fetch(request);
    }
    // If its the Atom feed, don't cache
    if (request.url.includes("/feed/")) {
        return fetch(request);
    }
    // If it's a 'main' page, use the loading page instead
    if (request.url.endsWith("/")) {
        e.respondWith(caches.open("core").then(function (core) {
            // Get the loading page
            return fetchAndCache("/loading/", core, false);
        }));
        return;
    }
    // TODO filter requests
    // Basic read-through caching.
    e.respondWith(caches.open("core").then(function (core) {
        return core.match(request).then(function (response) {
            if (response) {
                return response;
            }
            // we didn't have it in the cache, so add it to the cache and return it
            log("runtime caching:", request.url);
            // now grab the file and add it to the cache
            return fetchAndCache(request, core, true);
        });
    }));
});
