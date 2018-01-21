interface PartialLoadDetails {
    loadTime: number;
    destination: string;
}
interface Window {
    /**
     * Queue a function to run when the DOM is ready for interactivity
     * (or now, if it is already interactive)
     */
    whenReady: (callback: Function) => void;
}