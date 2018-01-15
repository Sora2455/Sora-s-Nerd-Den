(function () {
    "use strict";
    var config = {
        // If the image gets within 50px in the Y axis, start the download.
        rootMargin: "50px 0px",
        threshold: 0.01
    };
    var observer;
    //If we're using a browser without the IntersectionObserver (IE11, Safari 11), skip the lazy part and just load the resources
    if ("IntersectionObserver" in window) {
        observer = new IntersectionObserver(onIntersection, config);
    }
    /**
     * Set up the lazy items so that they won't try to load when we add them to the document, but will once the user is close to seeing them
     * @param lazyArea The div tag that is currently disconnected from the DOM, containing images that shouldn't be loaded until visible
     */
    function prepareLazyContents(lazyArea) {
        var lazyImgs = lazyArea.getElementsByTagName("img");
        for (var i = lazyImgs.length; i--;) {
            var lazyImg = lazyImgs[i];
            //Store our ACTUAL source for later
            lazyImg.setAttribute("data-lazy-src", lazyImg.getAttribute("src"));
            //Set the item to point to a temporary replacement (a 1x1 pixel gif)
            lazyImg.setAttribute("src", "/img/spacer.gif");
            //Now observe the item so that we can start loading when it gets close to the viewport
            observer.observe(lazyImg);
        }
        var lazyPictures = lazyArea.getElementsByTagName("picture");
        for (var i2 = lazyPictures.length; i2--;) {
            var lazyPicture = lazyPictures[i2];
            var newSource = document.createElement("source");
            newSource.setAttribute("srcset", "/img/spacer.gif");
            newSource.setAttribute("data-lazy-remove", "true");
            //adding this source tag at the start of the picture tag means the browser will load it first
            lazyPicture.insertBefore(newSource, lazyPicture.firstChild);
            var baseImage = lazyPicture.getElementsByTagName("img")[0];
            if (baseImage) {
                //this is a picture tag, so we need to watch the image (as the picture tag is usually smaller than the image)
                observer.observe(baseImage);
            }
        }
    }
    /**
     * Handle the intersection postback
     */
    function onIntersection(entries, obsvr) {
        entries.forEach(function (entry) {
            if (entry.intersectionRatio === 0) {
                return;
            }
            //if the item is now visible, load it and stop watching it
            var lazyItem = entry.target;
            obsvr.unobserve(lazyItem);
            //Just in case the img is the decendent of a picture element, check for source tags
            var jammingSource = lazyItem.parentElement.querySelector("source[data-lazy-remove]");
            if (jammingSource) {
                lazyItem.parentElement.removeChild(jammingSource);
            }
            //Put the source back where we found it - now that the element is attached to the document, it will load now
            lazyItem.setAttribute("src", lazyItem.getAttribute("data-lazy-src"));
            lazyItem.removeAttribute("data-lazy-src");
        });
    }
    /**
     * Retrieve the elements from the 'lazy load' no script tags and prepare them for display
     */
    function setUp() {
        //Get all the noscript tags on the page
        var lazyLoadAreas = document.getElementsByTagName("noscript");
        for (var i = lazyLoadAreas.length; i--;) {
            var noScriptTag = lazyLoadAreas[i];
            //only process the ones marked for lazy loading
            if (!noScriptTag.hasAttribute("data-lazy-load")) {
                continue;
            }
            // The contents of a noscript tag are treated as text to JavaScript
            var lazyAreaHtml = noScriptTag.textContent || noScriptTag.innerHTML;
            // So we stick them in the innerHTML of a new div tag to 'load' them
            var lazyArea = document.createElement("div");
            lazyArea.innerHTML = lazyAreaHtml;
            //Only delay loading if we can use the IntersectionObserver to check for visibility
            if (observer) {
                prepareLazyContents(lazyArea);
            }
            noScriptTag.parentNode.replaceChild(lazyArea, noScriptTag);
        }
    }
    //Use requestAnimationFrame as this will propably cause repaints
    document.addEventListener("DOMContentLoaded", function () { requestAnimationFrame(setUp); });
    document.getElementById("main-content").addEventListener("ContentModified", function () { requestAnimationFrame(setUp); });
})();
