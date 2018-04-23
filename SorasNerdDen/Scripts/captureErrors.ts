window.onerror = function (errorMsg, url, lineNumber, col, errorObj) {
    "use strict";
    try {
        const xhr = new XMLHttpRequest();
        xhr.open("POST", "/error/scripterror/", true);
        xhr.setRequestHeader('Content-Type', 'application/json; charset=utf-8');
        xhr.send(JSON.stringify({
            "Page": url,
            "Message": errorMsg,
            "Line": lineNumber,
            "Column": col,
            "StackTrace": (errorObj && errorObj.stack ? errorObj.stack : null)
        }));
    }
    catch (e) {
        console.error(e);
    }
    return false;
};
window.name = "Sora's Nerd Den";