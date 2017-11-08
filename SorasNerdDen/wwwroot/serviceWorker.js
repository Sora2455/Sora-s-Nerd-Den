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
    return fetch(request.clone()).then(function (response) {
        cache.put(request, response.clone());
        return response;
    });
}
addEventListener("install", function (e) {
    "use strict";
    // Put updated resources in a new cache, so that currently running pages
    // get the current versions.
    e.waitUntil(caches.delete("core-waiting").then(function () {
        return caches.open("core-waiting").then(function (core) {
            var resourceUrls = [
                "/",
                // TODO /offline.html
                "/css/site.css",
                "/css/font-awesome.css",
                "/js/jquery.js",
                "/js/bootstrap.js",
                "/js/site.js"
            ];
            return core.addAll(resourceUrls)
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

//# sourceMappingURL=serviceWorker.js.map
