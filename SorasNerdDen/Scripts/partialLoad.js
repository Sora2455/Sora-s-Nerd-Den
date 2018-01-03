if ('NodeList' in window && !NodeList.prototype.forEach) {
    NodeList.prototype.forEach = function (callback, thisArg) {
        "use strict";
        thisArg = thisArg || window;
        for (var i = 0; i < this.length; i++) {
            callback.call(thisArg, this[i], i, this);
        }
    };
}
(function () {
    "use strict";
    document.addEventListener("DOMContentLoaded", function () {
        if (history.pushState) {
            document.querySelectorAll("a[href]").forEach(function (link) {
                link.addEventListener("click", tryPartialLoad);
            });
            var newTarget = getPartialUrl(location.toString());
            partialLoad(newTarget);
        }
    });
    var partialLoadEvent = new CustomEvent("PartialyLoaded", { bubbles: true });
    document.addEventListener("PartialyLoaded", function (ev) {
        ev.target.querySelectorAll("a[href]").forEach(function (link) {
            link.addEventListener("click", tryPartialLoad);
        });
    });
    function tryPartialLoad(e) {
        var originalTarget = e.target.closest("a[href]").href;
        if (!originalTarget) {
            return;
        }
        var desination;
        try {
            desination = new URL(originalTarget);
        }
        catch (err) {
            return;
        }
        if (desination.host !== location.host) {
            return;
        }
        if (!originalTarget.endsWith("/") || originalTarget.includes("/feed/")) {
            return;
        }
        e.preventDefault();
        var newTarget = getPartialUrl(originalTarget);
        partialLoad(newTarget);
        var stateObject = { target: newTarget };
        history.pushState(stateObject, "", originalTarget);
    }
    function getPartialUrl(originalUrl) {
        if (originalUrl.includes("?")) {
            return originalUrl + "&v=m";
        }
        else {
            return originalUrl + "?v=m";
        }
    }
    function partialLoad(destination, isOffline) {
        document.getElementById("loading-indicator").style.display = "block";
        fetch(destination).then(function (response) {
            document.getElementById("loading-indicator").style.display = "none";
            return response.text().then(function (text) {
                var mainContent = document.getElementById("main-content");
                mainContent.innerHTML = text;
                mainContent.dispatchEvent(partialLoadEvent);
                var mainHeaders = mainContent.getElementsByTagName("h1");
                if (mainHeaders.length > 0) {
                    document.title = mainHeaders[0].textContent + " - Sora's Nerd Den";
                }
            });
        }).catch(function () {
            document.getElementById("loading-indicator").style.display = "none";
            if (!isOffline) {
                return partialLoad("/offline/?v=m", true);
            }
        });
    }
    window.addEventListener("popstate", function (ev) {
        var target = ev.state && ev.state.target;
        if (!target) {
            target = location.origin;
        }
        partialLoad(target);
    });
})();
