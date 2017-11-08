﻿(() => {
    "use strict";
    if ('serviceWorker' in navigator) {
        // Register a service worker hosted at the root of the
        // site using the default scope.
        (navigator as Navigator).serviceWorker.register('/serviceWorker.js', {
            scope: "./"
        }).then(function (registration) {
            console.log('Service worker registration succeeded:', registration);
        }).catch(function (error) {
            console.log('Service worker registration failed:', error);
        });
    }
})();