﻿(() => {
    "use strict";
    //If the Internationalisation API is not supported, don't try to localise
    if (!("Intl" in window)) {
        return;
    }
    /**
     * Localise <time> tags to their native language, format and timezone
     * @param timeTags The time tags to localise
     */
    function localiseTimes(timeTags: NodeListOf<HTMLTimeElement>) {
        //Try and use native language formatting, falling back to Australian, then American english
        const locales = [navigator.language, "en-AU", "en-US"];
        //Set up the formatters for time, dates, and date-times
        const timeFormatter = new Intl.DateTimeFormat(locales, { hour: "numeric", minute: "numeric" });
        const dateFormatter = new Intl.DateTimeFormat(locales, { weekday: "long", year: "numeric", month: "long", day: "numeric" });
        const dateTimeFormatter = new Intl.DateTimeFormat(locales, { year: "numeric", month: "long", day: "numeric", hour: "numeric", minute: "numeric" });
        //Now format each of the time tags
        for (let i = timeTags.length; i--;) {
            const time = timeTags[i];
            //Start with a 'blank' date
            const dateObj = new Date(Date.UTC(1900, 1));
            //Get the datetime property of the time tag
            const dateTimeString = time.getAttribute("datetime");
            if (dateTimeString.includes("-")) {
                //Date with possible time
                const [dateString, timeString] = dateTimeString.split("T");
                setDatePart(dateString, dateObj);
                if (timeString) {
                    setTimePart(timeString, dateObj);
                    time.textContent = dateTimeFormatter.format(dateObj);
                } else {
                    time.textContent = dateFormatter.format(dateObj);
                }
            } else {
                //Time only
                setTimePart(dateTimeString, dateObj);
                time.textContent = timeFormatter.format(dateObj);
            }
        }
    }
    document.addEventListener("DOMContentLoaded", () => {
        //Get all the time tags
        const times = document.getElementsByTagName("time");
        localiseTimes(times);
    });
    const mainTag = document.getElementsByTagName("main")[0];
    mainTag.addEventListener("PartialyLoaded", () => {
        //Get the newly loaded time tags
        const times = mainTag.getElementsByTagName("time");
        localiseTimes(times);
    });
    function setDatePart(dateString: string, dateObj: Date) {
        const [year, month, day] = dateString.split("-");
        //JavsScript months are 0-indexed for some reason
        dateObj.setUTCFullYear(parseInt(year, 10), parseInt(month, 10) - 1, parseInt(day, 10));
    }
    function setTimePart(timeString: string, dateObj: Date) {
        const [hours, minutes] = timeString.replace("Z", "").split(":");
        dateObj.setUTCHours(parseInt(hours,10));
        dateObj.setUTCMinutes(parseInt(minutes, 10));
    }
})();