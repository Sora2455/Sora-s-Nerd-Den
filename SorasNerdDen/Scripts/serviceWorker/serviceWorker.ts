onerror = console.error.bind(console);

/**
 * Moves the contents of one named cached into another.
 * @param source The name of the cache to move from
 * @param destination The name of the cache to move into
 */
function cacheCopy(source: string, destination: string) {
    "use strict";
    return caches.delete(destination).then(() => {
        return Promise.all([
            caches.open(source),
            caches.open(destination)
        ]).then((results) => {
            const sourceCache = results[0];
            const destCache = results[1];

            return sourceCache.keys().then((requests: RequestInfo[]) => {
                return Promise.all(requests.map((request) => {
                    return sourceCache.match(request).then((response) => {
                        return destCache.put(request, response);
                    });
                }));
            });
        });
    });
}

/**
 * Return to the client a cached version of the page (if we have one), check for updates to the page,
 * and if it has updated, notify the client.
 * @param e The fetch event we are responding to
 * @param versioned True if this is a versioned file (like a stylesheet or a script), false otherwise
 */
function cacheUpdateRefresh(e: FetchEvent, versioned: boolean): void {
    "use strict";
    // Return the first response from the cache (if possible)
    e.respondWith(cacheFirst(e.request, versioned));
    // Afterwards, update our cache copy of this resource
    e.waitUntil(networkUpdate(e.request, versioned).then((updated) => {
        if (updated && !versioned) return refresh(e.request.url);
    }));
}

/**
 * Notify clients that an updated version of this page is availible
 * @param url The URL of the page that we have an updated copy of
 */
function refresh(url: string): Promise<void> {
    "use strict";
    const message = {
        type: "refresh",
        url: url
    };
    return (self as ServiceWorkerGlobalScope).clients.matchAll({ type: 'window' }).then((clients) => {
        // Notify each client
        clients.forEach((client: Client) => {
            client.postMessage(message);
        });
    });
}

/**
 * Try to update a cached request.
 * A server error or a 304 Not Modified response does not count as un update.
 * @param request The request for the resource we are trying to update
 * @param versioned True if the file is a versioned resource (like a script), false otherwise
 * @returns True if the response was updated, false otherwise.
 */
function networkUpdate(request: Request, versioned: boolean): Promise<boolean> {
    "use strict";
    return caches.open("core").then((core) => {
        return fetch(request.clone(), { mode: "no-cors" }).then((response) => {
            if (response.ok) {
                if (versioned) { core.delete(request.clone(), { ignoreSearch: true }); }
                core.put(request.clone(), response);
                return true;
            }
            return false;
        });
     });
}

/**
 * Retrieve a file from the cache if possible, the network if not
 * @param request The request of the resource we are seeking to fetch
 * @param versioned True if the file is a versioned resource (like a script), false otherwise
 */
function cacheFirst(request: Request, versioned: boolean): Promise<Response> {
    "use strict";
    return caches.open("core").then((core) => {
        // Look in the cache for this value
        return core.match(request.clone()).then((result) => {
            if (result) { return result; }
            // If we can't find that result in the cache, try and get it from the network
            return fetch(request.clone(), { mode: "no-cors" }).then((result) => {
                if (result || !versioned) { return result; }
                // If we can't get this file from the network and this is a versioned file, get the previous version
                return core.match(request.clone(), { ignoreSearch: true });
            });
        });
    });
}

/**
 * When the service worker is being intalled, download the required assets into a temporary cache
 * @param e The intall event
 */
function installHander(e: ExtendableEvent): void {
    "use strict";
    // Put updated resources in a new cache, so that currently running pages
    // get the current versions.
    e.waitUntil(caches.delete("core-waiting").then(() => {
        return caches.open("core-waiting").then((core) => {
            const resourceUrls = [
                "/loading/",
                // ?v=m means without the shared view (just the main content)
                "/?v=m",
                "/offline/?v=m",
                "/css/site.css",
                "/js/site.js"
            ];

            return Promise.all(resourceUrls.map((key) => {
                // Make sure to download fresh versions of the files!
                return fetch(key, { cache: "no-cache" })
                    .then((response) => core.put(key, response));
            }))
                // Don't wait for the client to refresh the page (as this site is designed not to refresh)
                .then(() => (self as ServiceWorkerGlobalScope).skipWaiting());
        });
    }));
}

/**
 * When the service worker is being activated, move our assets from the temporary cache to our main cache
 * @param e The install event
 */
function activationHander(e: ExtendableEvent): void {
    "use strict";
    // Copy the newly installed cache to the active cache
    e.waitUntil(cacheCopy("core-waiting", "core")
        // Declare that we'll be taking over now
        .then(() => (self as ServiceWorkerGlobalScope).clients.claim())
        // Delete the waiting cache afterward to save client memory space
        .then(() => caches.delete("core-waiting")));
}

/**
 * When the browser makes a GET request, return a result from cache if possible before
 * trying to update that page in said cache
 * @param e The fetch event
 */
function fetchHandler(e: FetchEvent): void {
    "use strict";
    const request = e.request;

    // If not a GET request, don't cache
    if (request.method !== "GET") {
        e.respondWith(fetch(request));
        return;
    }
    // If its the Atom feed, don't cache
    if (request.url.includes("/feed/")) {
        e.respondWith(fetch(request));
        return;
    }
    // If it's a 'main' page, use the loading page instead
    if (request.url.endsWith("/")) {
        e.respondWith(cacheFirst(new Request('/loading/'), false));
        return;
    }
    // TODO filter requests

    // If the URL ends with v=m, this is one of our 'minimal views'
    if (request.url.endsWith("v=m")) { return cacheUpdateRefresh(e, false); }

    return cacheUpdateRefresh(e, true);
}

addEventListener("install", installHander);
addEventListener("activate", activationHander);
addEventListener("fetch", fetchHandler);