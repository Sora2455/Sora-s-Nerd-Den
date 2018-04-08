((w, d, n, l) => {
    "use strict";
    if ('serviceWorker' in n) {
        // Register a service worker hosted at the root of the
        // site using the default scope.
        n.serviceWorker.register('/serviceWorker.js', {
            scope: "./"
        }).then(function (registration) {
            console.log('Service worker registration succeeded:', registration);
        }).catch(function (error) {
            console.log('Service worker registration failed:', error);
        });
        n.serviceWorker.addEventListener("message", recieveMessage);
    }
    const updateMessage = d.getElementById('update');
    function recieveMessage(messageEvent: MessageEvent) {
        if (messageEvent.data && messageEvent.data.type === "refresh") {
            const url = messageEvent.data.url as string;
            if (url === l.href || url === l.pathname ||
                url === l.pathname.substring(1)) {
                updateMessage.removeAttribute('hidden');
            }
        }
    }
    updateMessage.getElementsByTagName('a')[0].addEventListener('click', () => {
        l.reload();
    });
    const cs = d.currentScript;
    if (cs) { cs.parentNode.removeChild(cs); }
})(window, document, navigator, location);