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
function fetchAndCache(request, cache) {
    "use strict";
    if (!(request instanceof Request)) {
        request = new Request(request);
    }
    return fetch(request.clone()).then(function (response) {
        // if the response came back not okay (like a server error) try and get from cache
        if (!response.ok) {
            return cache.match(request);
        }
        // otherwise store the response for future use, and return the response to the client
        cache.put(request, response.clone());
        return response;
    }).catch(function () {
        // if there was an error (almost certainly network touble) try and get from cache
        return cache.match(request);
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
                "/js/jquery.js",
                "/js/bootstrap.js",
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
    // If it's a 'main' page, use the loading page instead
    if (request.url.endsWith("/")) {
        return bigPageLoad(e);
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
            return fetchAndCache(request, core);
        });
    }));
});
function bigPageLoad(e) {
    "use strict";
    // Add a 'v=m' paramater to the URL, which tells the view model only to send the page main content
    var newTarget = "";
    if (e.request.url.includes("?")) {
        newTarget = e.request.url + "&v=m";
    }
    else {
        newTarget = e.request.url + "?v=m";
    }
    e.respondWith(caches.open("core").then(function (core) {
        // Get the loading page
        return fetchAndCache("/loading/", core);
    }));
}
