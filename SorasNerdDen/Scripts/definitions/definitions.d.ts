interface NodeList {
    forEach: (callback: (node: Node, i: number, nodeList: NodeList) => void, thisArg?: any) => void;
}
interface PartialLoadDetails {
    loadTime: number;
    destination: string;
}
interface PageTitleAndDescription {
    /**
     * The (relative) url to the page
     */
    url: string;
    /**
     * The title of the page (also the main heading)
     */
    title: string;
    /**
     * A short description of the page (also the first paragraph)
     */
    description: string;
}
// To add to this list, you need to modify storageManager's onupgradeneeded handler
declare type StoreName = "pageDetails" | "pendingLoads" | "pendingComments";
interface Window {
    /**
     * Queue a function to run when the DOM is ready for interactivity
     * (or now, if it is already interactive)
     */
    whenReady: (callback: Function) => void;
    /**
     * Queue a function to run when the page is fully loaded (or now, if it is already loaded)
     */
    whenLoaded: (callback: Function) => void;
    /**
     * A promise that returns once the database functions are free to be interacted with
     * (i.e.) once the database has finished setting up
     */
    dbReady: Promise<void>;
    /**
     * Store the details of a JSON object to the user's disk (indexeddb with a localstorage fallback)
     * @param store The name of the namespace where this data will be stored
     * @param key A function that selects the index to store the data under from the obj being stored
     * @param data The data to store
     */
    storeJsonData<T>(store: StoreName, keyFunc: (obj: T) => string | number, data: T): Promise<void>;
    /**
     * Retrieves earlier stored data from the user's disk (indexeddb with a localstorage fallback)
     * @param store The name of the namespace where this data was stored
     * @param key The index that the data was stored under
     */
    retrieveJsonData: (store: StoreName, key: string | number) => Promise<any>;
}