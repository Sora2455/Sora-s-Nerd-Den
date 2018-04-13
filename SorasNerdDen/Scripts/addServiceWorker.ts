///<reference path="definitions/definitions.d.ts" />
((w, d, n, l) => {
    "use strict";
    if ('serviceWorker' in n) {
        w.whenLoaded(() => {
            n.serviceWorker.register('/serviceWorker.js');
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
})(window, document, navigator, location);