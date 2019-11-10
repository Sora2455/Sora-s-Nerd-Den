///<reference path="definitions/definitions.d.ts" />
((w) => {
    "use strict";

    if (typeof Promise === "undefined") {
        return;
    }

    function localStorageFallback() {
        w.storeJsonData = function<T> (store: StoreName, keyFunc: (obj: T) => string | number, data: T): Promise<void> {
            localStorage.setItem(`${store}.${keyFunc(data)}`, JSON.stringify(data));
            return Promise.resolve();
        }
        w.retrieveJsonData = function (store: StoreName, key: string): Promise<any> {
            let json = localStorage.getItem(`${store}.${key}`);
            if (json) {
                return Promise.resolve(JSON.parse(json));
            } else {
                return Promise.reject(null);
            }
        }
    }
    w.dbReady = new Promise((dbReady) => {
        if (!w.indexedDB) {
            localStorageFallback();
            //Database fallback in place - we're ready for reads and writes now
            dbReady();
        }
        let db: IDBDatabase;
        const req = indexedDB.open("Offline storage", 1);
        req.onsuccess = function (evt) {
            db = (evt.target as IDBOpenDBRequest).result;
            w.storeJsonData = function<T> (store: StoreName, _: (obj: T) => string | number, data: T): Promise<void> {
                const transaction = db.transaction(store, "readwrite");
                return new Promise((writeCompleted, writeFailed) => {
                    transaction.oncomplete = () => {
                        writeCompleted();
                    };
                    transaction.onerror = (ev) => {
                        writeFailed(ev);
                    };
                    transaction.objectStore(store).put(data);
                });
            }
            w.retrieveJsonData = function (store: StoreName, key: string | number): Promise<any> {
                const transaction = db.transaction(store);
                const result = transaction.objectStore(store).get(key);
                return new Promise((readCompleted, readFailed) => {
                    transaction.oncomplete = () => {
                        readCompleted(result.result);
                    };
                    transaction.onerror = (ev) => {
                        readFailed(ev);
                    };
                });
            }
            //Database set up - we're ready for reads and writes now
            dbReady();
        };
        req.onerror = function () {
            localStorageFallback();
            //Database fallback in place - we're ready for reads and writes now
            dbReady();
        };
        req.onupgradeneeded = function (evt) {
            // Save the IDBDatabase interface
            const dbu = (evt.target as IDBOpenDBRequest).result as IDBDatabase;
            if (!evt.oldVersion) {
                dbu.createObjectStore("pageDetails", { keyPath: "url" });
                dbu.createObjectStore("pendingLoads", { autoIncrement: true });
                dbu.createObjectStore("pendingComments", { autoIncrement: true });
                dbu.createObjectStore("environmentVariables", { keyPath: "name" });
            }
        }
    });
})(window);