///<reference path="definitions/definitions.d.ts" />
// NodeList.forEach polyfill
if ('NodeList' in window && !NodeList.prototype.forEach) {
    NodeList.prototype.forEach = function (callback: Function, thisArg: any) {
        "use strict";
        thisArg = thisArg || window;
        for (let i = 0; i < this.length; i++) {
            callback.call(thisArg, this[i], i, this);
        }
    };
}