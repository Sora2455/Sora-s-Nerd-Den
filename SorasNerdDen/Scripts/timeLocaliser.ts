///<reference path="definitions/definitions.d.ts" />
((w, d) => {
    "use strict";
    //If the Internationalisation API is not supported, don't try to localise
    if (typeof Intl !== "object") {
        return;
    }
    //Try and use native language formatting, falling back to Australian, then American english
    const locales = [navigator.language, "en-AU", "en-US"];
    //Set up the formatters for time, dates, and date-times
    const timeFormatter = new Intl.DateTimeFormat(locales, { hour: "numeric", minute: "numeric", timeZoneName: "short" });
    const dateFormatter = new Intl.DateTimeFormat(locales, { weekday: "long", year: "numeric", month: "long", day: "numeric", timeZone: "UTC" });
    const dateTimeFormatter = new Intl.DateTimeFormat(locales, { year: "numeric", month: "long", day: "numeric", hour: "numeric", minute: "numeric", timeZoneName: "short" });
    /**
     * Localise <time> tags to their native language, format and timezone
     * @param timeTags The time tags to localise
     */
    function localiseTimes(timeTags: HTMLCollectionOf<HTMLTimeElement>) {
        //Now format each of the time tags
        for (let i = timeTags.length; i--;) {
            const time = timeTags[i];
            //Start with a 'blank' date
            const dateObj = new Date(Date.UTC(1900, 1));
            //Get the datetime property of the time tag
            const dateTimeString = time.getAttribute("datetime");
            if (dateTimeString.indexOf("-") !== -1) {
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
    w.whenReady(() => {
        //Get all the time tags
        const times = (d.getElementsByTagName("time") as any) as HTMLCollectionOf<HTMLTimeElement>;
        localiseTimes(times);
        const mainContent = d.getElementById("main-content");
        mainContent.addEventListener("ContentModified", () => {
            //Get the newly loaded time tags
            const times = (mainContent.getElementsByTagName("time") as any) as HTMLCollectionOf<HTMLTimeElement>;
            localiseTimes(times);
        });
    });
    function setDatePart(dateString: string, dateObj: Date) {
        const [year, month, day] = dateString.split("-");
        //JavsScript months are 0-indexed for some reason
        dateObj.setUTCFullYear(parseInt(year, 10), parseInt(month, 10) - 1, parseInt(day, 10));
    }
    function setTimePart(timeString: string, dateObj: Date) {
        const [hours, minutes, seconds] = timeString.replace("Z", "").split(":");
        dateObj.setUTCHours(parseInt(hours,10));
        dateObj.setUTCMinutes(parseInt(minutes,10));
        dateObj.setUTCSeconds(parseInt(seconds,10));
    }
})(window, document);