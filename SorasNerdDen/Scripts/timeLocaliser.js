(function () {
    "use strict";
    //If the Internationalisation API is not supported, don't try to localise
    if (!("Intl" in window)) {
        return;
    }
    //Try and use native language formatting, falling back to Australian, then American english
    var locales = [navigator.language, "en-AU", "en-US"];
    //Set up the formatters for time, dates, and date-times
    var timeFormatter = new Intl.DateTimeFormat(locales, { hour: "numeric", minute: "numeric" });
    var dateFormatter = new Intl.DateTimeFormat(locales, { weekday: "long", year: "numeric", month: "long", day: "numeric" });
    var dateTimeFormatter = new Intl.DateTimeFormat(locales, { year: "numeric", month: "long", day: "numeric", hour: "numeric", minute: "numeric" });
    /**
     * Localise <time> tags to their native language, format and timezone
     * @param timeTags The time tags to localise
     */
    function localiseTimes(timeTags) {
        //Now format each of the time tags
        for (var i = timeTags.length; i--;) {
            var time = timeTags[i];
            //Start with a 'blank' date
            var dateObj = new Date(Date.UTC(1900, 1));
            //Get the datetime property of the time tag
            var dateTimeString = time.getAttribute("datetime");
            if (dateTimeString.includes("-")) {
                //Date with possible time
                var _a = dateTimeString.split("T"), dateString = _a[0], timeString = _a[1];
                setDatePart(dateString, dateObj);
                if (timeString) {
                    setTimePart(timeString, dateObj);
                    time.textContent = dateTimeFormatter.format(dateObj);
                }
                else {
                    time.textContent = dateFormatter.format(dateObj);
                }
            }
            else {
                //Time only
                setTimePart(dateTimeString, dateObj);
                time.textContent = timeFormatter.format(dateObj);
            }
        }
    }
    document.addEventListener("DOMContentLoaded", function () {
        //Get all the time tags
        var times = document.getElementsByTagName("time");
        localiseTimes(times);
    });
    var mainContent = document.getElementById("main-content");
    mainContent.addEventListener("ContentModified", function () {
        //Get the newly loaded time tags
        var times = mainContent.getElementsByTagName("time");
        localiseTimes(times);
    });
    function setDatePart(dateString, dateObj) {
        var _a = dateString.split("-"), year = _a[0], month = _a[1], day = _a[2];
        //JavsScript months are 0-indexed for some reason
        dateObj.setUTCFullYear(parseInt(year, 10), parseInt(month, 10) - 1, parseInt(day, 10));
    }
    function setTimePart(timeString, dateObj) {
        var _a = timeString.replace("Z", "").split(":"), hours = _a[0], minutes = _a[1];
        dateObj.setUTCHours(parseInt(hours, 10));
        dateObj.setUTCMinutes(parseInt(minutes, 10));
    }
})();
