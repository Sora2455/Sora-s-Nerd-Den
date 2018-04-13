interface NodeList {
    forEach: (callback: (node: Node, i: number, nodeList: NodeList) => void, thisArg?: any) => void;
}
interface PartialLoadDetails {
    loadTime: number;
    destination: string;
}
interface PageTitleAndDescription {
    title: string;
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
}