(function () {
    "use strict";
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.register('/serviceWorker.js', {
            scope: "./"
        }).then(function (registration) {
            console.log('Service worker registration succeeded:', registration);
        }).catch(function (error) {
            console.log('Service worker registration failed:', error);
        });
    }
})();
