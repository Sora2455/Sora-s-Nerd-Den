window.onerror = function (errorMsg, url, lineNumber, col, errorObj) {
    "use strict";
    try {
        if (errorMsg.indexOf('Script error.') > -1) { return; }
        const formData = new FormData();
        formData.append("Page", url);
        formData.append("Message", errorMsg);
        formData.append("Line", (lineNumber || 0).toString());
        formData.append("Column", (col || 0).toString());
        formData.append("StackTrace", errorObj && errorObj.stack ? errorObj.stack : "");
        fetch("/error/scripterror/", {
            method: 'POST',
            body: formData
        });
    }
    catch (e) {
        console.error(e);
    }
    return false;
};