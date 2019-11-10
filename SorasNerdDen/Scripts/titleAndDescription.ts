///<reference path="definitions/definitions.d.ts" />
((w, d) => {
    "use strict";
    function recordTitleAndDescription() {
        const tdElm = d.getElementById("td");
        if (!tdElm) return;
        const td = JSON.parse(d.getElementById("td").textContent) as PageTitleAndDescription;
        td.url = location.pathname;
        //Now (if we can), store that away in a client-side db
        if (typeof Promise !== "undefined" &&
            //Only record the title if this isn't the loading page or the offline page
            //TODO - not error pages?
            td.title !== "Loading" && td.title !== "Offline") {
            w.dbReady.then(() => {
                w.storeJsonData("pageDetails", (td) => td.url, td);
            });
        }
        //And then, set the title and description of the page to our new values
        //(important if the page was partially loaded)
        d.title = `${td.title} - @SiteTitle`;
        if (d.querySelector) {
            d.head.querySelector("meta[name='description']")
                .setAttribute("content", td.description);
            d.head.querySelector("meta[property='og:title']")
                .setAttribute("content", td.title);
            d.head.querySelector("meta[property='og:description']")
                .setAttribute("content", td.description);
            d.head.querySelector("meta[property='og:url']")
                .setAttribute("content", location.href);
        }
    }
    w.whenReady(() => {
        recordTitleAndDescription();
        d.getElementById("main-content").addEventListener("ContentModified", recordTitleAndDescription);
    });
})(window, document);