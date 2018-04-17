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
     * Store the details of a page to the user's disk (indexeddb with a localstorage fallback)
     * @param pageTitleAndDescription The page details
     */
    storePageDetails: (pageTitleAndDescription: PageTitleAndDescription)
        => Promise<void>;
    /**
     * Retrieve the details of a page from the user's disk (indexeddb with a localstorage fallback)
     * @param relativeUrl The (relative) URL of the page we are retrieving the details of
     */
    retreivePageDetails: (relativeUrl: string) => Promise<PageTitleAndDescription>;
}