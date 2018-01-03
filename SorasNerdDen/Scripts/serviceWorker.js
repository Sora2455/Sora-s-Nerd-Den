var log = console.log.bind(console);
var err = console.error.bind(console);
onerror = err;
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
    return fetch(request.clone()).then(function (response) {
        if (!response.ok) {
            return findInCache(request, cache, versioned);
        }
        if (versioned) {
            cache.delete(request, { ignoreSearch: true });
        }
        cache.put(request, response.clone());
        return response;
    }).catch(function () {
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
    e.waitUntil(caches.delete("core-waiting").then(function () {
        return caches.open("core-waiting").then(function (core) {
            var resourceUrls = [
                "/loading/",
                "/?v=m",
                "/offline/?v=m",
                "/css/site.css",
                "/css/font-awesome.css",
                "/js/jquery.js",
                "/js/bootstrap.js",
                "/js/site.js"
            ];
            return Promise.all(resourceUrls.map(function (key) {
                return fetch(key, { cache: "no-cache" })
                    .then(function (response) { return core.put(key, response); });
            }))
                .then(function () { return self.skipWaiting(); });
        });
    }));
});
addEventListener("activate", function (e) {
    "use strict";
    e.waitUntil(cacheCopy("core-waiting", "core")
        .then(function () { return self.clients.claim(); })
        .then(function () { return caches.delete("core-waiting"); }));
});
addEventListener("fetch", function (e) {
    "use strict";
    var request = e.request;
    if (request.method !== "GET") {
        return fetch(request);
    }
    if (request.url.endsWith("/")) {
        e.respondWith(caches.open("core").then(function (core) {
            return fetchAndCache("/loading/", core, false);
        }));
        return;
    }
    e.respondWith(caches.open("core").then(function (core) {
        return core.match(request).then(function (response) {
            if (response) {
                return response;
            }
            log("runtime caching:", request.url);
            return fetchAndCache(request, core, true);
        });
    }));
});
