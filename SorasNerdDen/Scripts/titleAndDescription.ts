///<reference path="definitions/definitions.d.ts" />
((w, d) => {
    "use strict";
    function getPageTitle() {
        //By convention, our page title is repeated in our main heading
        const mainHeading = d.querySelector("#main-content h1");
        //If the first child of the title element isn't a text node, that will be the emoji
        // in that case, grab the second node and trim it
        return mainHeading.childNodes[0].nodeType === 3 ?
            mainHeading.childNodes[0].nodeValue :
            mainHeading.childNodes[1].nodeValue.trim();
    }
    function recordTitleAndDescription() {
        //By convention, our page description is repeated in the first paragraph tag under the heading
        const descriptionElement = d.querySelector("#main-content p");
        const td = {} as PageTitleAndDescription;
        td.title = getPageTitle();
        td.description = descriptionElement.textContent;
        td.url = location.pathname;
        //Now (if we can), store that away in a client-side db
        if (typeof Promise !== "undefined" &&
            //Only record the title if this isn't the loading page or the offline page
            //TODO - not error pages?
            td.title !== "Loading" && td.title !== "Offline") {
            w.dbReady.then(() => {
                w.storePageDetails(td);
            });
        }
        //And then, set the title and description of the page to our new values
        //(important if the page was partially loaded)
        d.title = `${td.title} - Sora's Nerd Den`;
        if (d.querySelector) {
            d.head.querySelector("meta[name='description']")
                .setAttribute("content", td.description);
            d.head.querySelector("meta[name='twitter:title']")
                .setAttribute("content", td.title);
            d.head.querySelector("meta[name='twitter:description']")
                .setAttribute("content", td.description);
        }
    }
    w.whenReady(() => {
        recordTitleAndDescription();
        d.getElementById("main-content").addEventListener("ContentModified", recordTitleAndDescription);
    });
})(window, document);