///<reference path="definitions/definitions.d.ts" />
((w, d) => {
    "use strict";
    function recordTitleAndDescription() {
        const main = d.getElementById("main-content");
        //By convention, our page title is repeated in our main heading
        const titleElement = main.getElementsByTagName("h1")[0];
        //By convention, our page description is repeated in the first paragraph tag under the heading
        const descriptionElement = main.getElementsByTagName("p")[0];
        const titleAndDescription = {} as PageTitleAndDescription;
        //If the first child of the title element isn't a text node, that will be the emoji
        // in that case, grab the second node and trim it
        titleAndDescription.title = titleElement.childNodes[0].nodeType === 3 ?
            titleElement.childNodes[0].nodeValue :
            titleElement.childNodes[1].nodeValue.trim();
        titleAndDescription.description = descriptionElement.textContent;
        //Now (if we can), store that away in localStorage
        if (w.localStorage) {
            localStorage.setItem(location.pathname, JSON.stringify(titleAndDescription));
        }
        //And then, set the title and description of the page to our new values
        //(important if the page was partially loaded)
        d.title = `${titleAndDescription.title} - Sora's Nerd Den`;
        if (d.querySelector) {
            d.head.querySelector("meta[name='description']")
                .setAttribute("content", titleAndDescription.description);
            d.head.querySelector("meta[name='twitter:title']")
                .setAttribute("content", titleAndDescription.title);
            d.head.querySelector("meta[name='twitter:description']")
                .setAttribute("content", titleAndDescription.description);
        }
    }
    w.whenReady(() => {
        recordTitleAndDescription();
        d.getElementById("main-content").addEventListener("ContentModified", recordTitleAndDescription);
    });
})(window, document);