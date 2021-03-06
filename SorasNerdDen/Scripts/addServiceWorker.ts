﻿///<reference path="definitions/definitions.d.ts" />
((w, d, n, l) => {
    "use strict";
    const notificationPermissionKey = "notification-permission";
    if ('serviceWorker' in n) {
        w.whenLoaded(() => {
            n.serviceWorker.register('/serviceWorker.js');
        });
        w.whenReady(() => {
            const updateMessage = d.getElementById('update');
            const recieveMessage = (messageEvent: MessageEvent) => {
                if (messageEvent.data && messageEvent.data.type === "refresh") {
                    const url = messageEvent.data.url as string;
                    if (url === l.href || url === l.pathname ||
                        url === l.pathname.substring(1)) {
                        updateMessage.removeAttribute('hidden');
                    }
                }
            }
            updateMessage.getElementsByTagName('a')[0].addEventListener('click', () => {
                l.reload();
            });
            n.serviceWorker.addEventListener("message", recieveMessage);
        })
    }
    // Set up the 'live updates' checkbox
    w.whenReady(() => {
        const notificationCheckbox = d.getElementById('notificationsCheckbox') as HTMLInputElement;
        // If it is possible to use push notifications...
        // (In Firefox private windows, serviceWorker can be missing while PushManager is present)
        if ("serviceWorker" in n && "PushManager" in w && typeof Notification === "function") {
            (w as unknown as Window).dbReady.then(() => {
                notificationCheckbox.removeAttribute("title");
                notificationCheckbox.disabled = false;
                setCheckboxState(notificationCheckbox);
                // Handle changes to the checkbox
                notificationCheckbox.addEventListener("change", toggleNotificationPermissions);
            });
        } else {
            notificationCheckbox.title = "Your browser does not support live updates";
        }
    });
    /**
     * Turn notification permissions 'on' or 'off'
     */
    function toggleNotificationPermissions(ev: Event) {
        const permissionCheckbox = ev.currentTarget as HTMLInputElement;
        if (Notification.permission === 'granted') {
            // We can't "un-request" Notification permission,
            // so set our localstorage option to the new checkbox value
            w.storeJsonData<EnvironmentVariable>("environmentVariables", (obj) => obj.name, {
                name: notificationPermissionKey,
                value: permissionCheckbox.checked ? 'granted' : 'denied'
            }).then(() => {
                setCheckboxState(permissionCheckbox);
            });
        } else if (Notification.permission === 'denied') {
            // If the user has denied Notification permissions, we should have picked that up on load
            // But just in case, remove the permission now and lock the checkbox
            w.storeJsonData<EnvironmentVariable>("environmentVariables", (obj) => obj.name, {
                name: notificationPermissionKey,
                value: 'denied'
            }).then(() => {
                setCheckboxState(permissionCheckbox);
            });
        } else if (Notification.permission === 'default' && permissionCheckbox.checked) {
            // If we haven't asked them before and they want to enable updates, ask them formally
            const handleNotificationChoice = (choice: NotificationPermission) => {
                w.storeJsonData<EnvironmentVariable>("environmentVariables", (obj) => obj.name, {
                    name: notificationPermissionKey,
                    value: choice
                }).then(() => {
                    setCheckboxState(permissionCheckbox);
                });
            }
            try {
                // Old browsers use a callback, new browsers use a promise
                Notification.requestPermission().then(handleNotificationChoice);
            } catch {
                Notification.requestPermission(handleNotificationChoice);
            }
        }
    }
    /**
     * Set the state of the notification checkbox (checked, enabled, tooltip)
     * based on the current permission state
     */
    function setCheckboxState(permissionCheckbox: HTMLInputElement) {
        let permissionPromise: Promise<NotificationPermission>;
        if (Notification.permission === 'granted') {
            // Becuase we can't "un-request" Notification permision,
            // we use a storage toggle to mark where the user doesn't want
            // to recieve notifications anymore
            permissionPromise = w.retrieveJsonData<EnvironmentVariable>("environmentVariables", notificationPermissionKey)
                .then(variable => variable.value as NotificationPermission);
        } else {
            permissionPromise = Promise.resolve(Notification.permission);
        }
        permissionPromise.then(permission => {
            if (permission === "granted") {
                permissionCheckbox.checked = true;
                ensureSubscribedToPushNotifications();
            } else {
                permissionCheckbox.checked = false;
                ensureNotSubscribedToPushNotifications();
            }
            // If permission has been denied, we can't re-enable it with JavaScript,
            // so disable the checkbox
            if (Notification.permission === "denied") {
                permissionCheckbox.disabled = true;
                permissionCheckbox.title =
                    "To enable this feature, you must un-block notifications then reload this page.";
            }
        });
    }
    /**
     * Subscribe to push notifications if we aren't already
     */
    function ensureSubscribedToPushNotifications() {
        n.serviceWorker.ready.then(registration => {
            registration.pushManager.getSubscription()
                .then(existingSubscription => {
                    if (!existingSubscription) {
                        // We have permission, but no subscription - fix that now
                        fetch("push/publicKey").then((response) => response.text())
                            .then((publicKey) => {
                                const convertedVapidKey = urlBase64ToUint8Array(publicKey);

                                return registration.pushManager.subscribe({
                                    userVisibleOnly: true,
                                    applicationServerKey: convertedVapidKey
                                });
                            })
                            .then(newSubscription => {
                                return Promise.all([
                                    fetch("/push/subscribe", {
                                        method: "post",
                                        headers: {
                                            "Content-type": "application/json"
                                        },
                                        body: JSON.stringify(newSubscription)
                                    }),
                                    w.storeJsonData("environmentVariables",
                                        (obj) => obj.name,
                                        { name: "pushEndpoint", value: newSubscription.endpoint })
                                ]);
                            }).then(() => {
                                console.log("Push subscription suceeded!");
                            }, (err) => {
                                console.error("Push subscription failed: ", err);
                            });
                    }
                });
        });
    }
    /**
     * Unsubscribe to push notifications if we have a subscription
     */
    function ensureNotSubscribedToPushNotifications() {
        n.serviceWorker.ready.then(registration => {
            return registration.pushManager.getSubscription();
        }).then(existingSubscription => {
            if (existingSubscription) {
                const oldEndpoint = existingSubscription.endpoint;
                // We have no permission, but a subscription - fix that now
                return existingSubscription.unsubscribe().then((succeeded) => {
                    if (succeeded) {
                        return Promise.all([
                            fetch("push/unsubscribe", {
                                method: "post",
                                headers: {
                                    "Content-type": "application/json"
                                },
                                body: JSON.stringify({
                                    endpoint: oldEndpoint
                                })
                            }),
                            w.removeJsonData("environmentVariables", "pushEndpoint")
                        ]).then(() => {
                            console.log("Push unsubscription suceeded!");
                        }, (err) => {
                            console.error("Push unsubscription failed:", err);
                        });
                    } else {
                        console.error("Push unsubscription failed");
                    }
                });
            }
        });
    }
    /**
     * This function is needed because Chrome doesn't accept a base64 encoded string
     * as value for applicationServerKey in pushManager.subscribe yet
     * https://bugs.chromium.org/p/chromium/issues/detail?id=802280
     * @param {string} base64String The base64 string to convert to an array of unsigned 8-bit integers
     */
    function urlBase64ToUint8Array(base64String: string): Uint8Array {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding)
            .replace(/\-/g, '+')
            .replace(/_/g, '/');

        const rawData = atob(base64);
        const outputArray = new Uint8Array(rawData.length);

        for (let i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }
        return outputArray;
    }
})(window, document, navigator, location);