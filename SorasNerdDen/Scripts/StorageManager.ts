///<reference path="definitions/definitions.d.ts" />
((w) => {
    "use strict";

    if (typeof Promise === "undefined") {
        return;
    }

    function localStorageFallback() {
        w.storePageDetails = function (pageDetails): Promise<void> {
            localStorage.setItem(`pageDetails.${pageDetails.url}`, JSON.stringify(pageDetails));
            return Promise.resolve();
        }
        w.retreivePageDetails = function (url): Promise<PageTitleAndDescription> {
            let titleJSON = localStorage.getItem(`pageDetails.${url}`);
            if (titleJSON) {
                return Promise.resolve(JSON.parse(titleJSON) as PageTitleAndDescription);
            } else {
                Promise.reject("No details found");
            }
        }
    }
    w.dbReady = new Promise((dbReady, dbSetupFailed) => {
        if (!w.indexedDB) {
            localStorageFallback();
            //Database fallback in place - we're ready for reads and writes now
            dbReady();
        }
        let db: IDBDatabase;
        const req = indexedDB.open("Offline storage", 1);
        req.onsuccess = function (evt) {
            db = (evt.target as IDBOpenDBRequest).result;
            w.storePageDetails = function (pageDetails): Promise<void> {
                const transaction = db.transaction("pageDetails", "readwrite");
                const pageDetailsStore = transaction.objectStore("pageDetails");
                return new Promise((writeCompleted, writeFailed) => {
                    transaction.oncomplete = (ev) => {
                        writeCompleted();
                    };
                    transaction.onerror = (ev) => {
                        writeFailed(ev);
                    };
                    pageDetailsStore.put(pageDetails);
                });
            }
            w.retreivePageDetails = function (url): Promise<PageTitleAndDescription> {
                const transaction = db.transaction("pageDetails");
                const pageDetailsStore = transaction.objectStore("pageDetails");
                const result = pageDetailsStore.get(url);
                return new Promise((readCompleted, readFailed) => {
                    result.onsuccess = (ev) => {
                        readCompleted((ev.target as IDBRequest).result);
                    };
                    result.onerror = (ev) => {
                        readFailed(ev);
                    };
                });
            }
            //Database set up - we're ready for reads and writes now
            dbReady();
        };
        req.onerror = function (evt) {
            localStorageFallback();
            //Database fallback in place - we're ready for reads and writes now
            dbReady();
        };
        req.onupgradeneeded = function (evt) {
            // Save the IDBDatabase interface
            const dbu = (evt.target as IDBOpenDBRequest).result as IDBDatabase;
            if (!evt.oldVersion) {
                const pageDetails = dbu.createObjectStore("pageDetails", { keyPath: "url" });
                const pendingLoads = dbu.createObjectStore("pendingLoads", { autoIncrement: true });
                const pendingComments = dbu.createObjectStore("pendingComments", { autoIncrement: true });
            }
        }
    });
})(window);