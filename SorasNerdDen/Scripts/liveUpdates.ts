interface INetworkInformation extends EventTarget {
    /**
     * Returns the effective bandwidth estimate in megabits per second,
     * rounded to the nearest multiple of 25 kilobits per seconds.
     */
    downlink: number;
    /**
     * Returns the maximum downlink speed, in megabits per second (Mbps),
     * for the underlying connection technology.
     */
    downlinkMax: number;
    /**
     * Returns the effective type of the connection meaning one of 'slow-2g', '2g', '3g', or '4g'.
     * This value is determined using a combination of recently observed round-trip time and downlink values.
     */
    effectiveType: "slow-2g" | "2g" | "3g" | "4g";
    /**
     * Returns the estimated effective round-trip time of the current connection,
     * rounded to the nearest multiple of 25 milliseconds.
     */
    rtt: number;
    /**
     * Returns true if the user has set a reduced data usage option on the user agent.
     */
    saveData: boolean;
    /**
     * Returns the type of connection a device is using to communicate with the network.
     */
    type: "bluetooth" | "cellular" | "ethernet" | "none" | "wifi" | "wimax" | "other" | "unknown";
}

// tslint:disable-next-line:interface-name
interface Navigator {
    connection: INetworkInformation;
}

let eventEmitter: EventSource;
let liveUpdateUnusableReason = getLiveUpdateUnusableReason();
const liveUpdateEventTarget = document.createTextNode(null) as EventTarget;

function getLiveUpdateUnusableReason(): string {
    if (typeof EventSource !== "function") {
        return "Live updates are not supported by this browser.";
    }

    if (navigator.connection) {
        if (navigator.connection.saveData) {
            return "Live updates are suspended while in data saving mode.";
        }
        const connectionType = navigator.connection.effectiveType;
        if (connectionType === "slow-2g" ||
            connectionType === "2g" ||
            connectionType === "3g") {
            return "Live updates are suspended while on slow connections.";
        }
    }

    if (!navigator.onLine) {
        return "Live updates are not possible while offline.";
    }

    if (document.visibilityState !== "visible") {
        return "Live updates are suspended while the page is not shown.";
    }

    if (!document.hasFocus()) {
        return "Live updates are suspended while the page is not focused.";
    }

    return null;
}

function checkLiveUpdateStatus() {
    const unusableReason = getLiveUpdateUnusableReason();
    if (liveUpdateUnusableReason !== unusableReason) {
        liveUpdateUnusableReason = unusableReason;
        // The reason has changed/appeared/disappeared!
        liveUpdateEventTarget.dispatchEvent(new CustomEvent("StatusChange", { detail: liveUpdateUnusableReason }));
    }
    if (unusableReason && !!eventEmitter) {
        tearDownLiveUpdates();
    } else if (!eventEmitter) {
        setUpLiveUpdates();
    }
}
// TODO - don't run if push notifications are up
function setUpLiveUpdates() {
    eventEmitter = new EventSource(null);
}

function tearDownLiveUpdates() {
    if (eventEmitter) {
        eventEmitter.close();
        eventEmitter = null;
    }
}

self.addEventListener("blur", checkLiveUpdateStatus, { capture: true });
self.addEventListener("focus", checkLiveUpdateStatus, { capture: true });
self.addEventListener("visibilitychange", checkLiveUpdateStatus, { capture: true });
if (navigator.connection) {
    navigator.connection.addEventListener("change", checkLiveUpdateStatus, { capture: true });
}
self.addEventListener("resume", checkLiveUpdateStatus, { capture: true });
self.addEventListener("online", tearDownLiveUpdates, { capture: true });
// If the following events fire, we know we have to stop live updates
self.addEventListener("freeze", tearDownLiveUpdates, { capture: true });
self.addEventListener("offline", tearDownLiveUpdates, { capture: true });
// Initial state check
checkLiveUpdateStatus();
