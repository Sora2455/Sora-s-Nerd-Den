using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Globalization;
using System.IO;

namespace SorasNerdDen.Attributes
{
    /// <summary>
    /// Adds a 'last modified' header to this response so that we can return a 304 Not Modified response
    /// if the client has a current version of the page in cache.
    /// </summary>
    public class AddVersionHeaderAttribute : ActionFilterAttribute
    {
        private const string BASE_VIEW_FOLDER = "Views";

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            CheckLastModified(filterContext);
        }

        /// <summary>
        /// Checks if the view has been modified since the last version in the client's cache.
        /// If not, return a 304 Not Modified response.
        /// </summary>
        /// <param name="filterContext">The current action context</param>
        private static void CheckLastModified(ActionExecutingContext filterContext)
        {
            string actionName = filterContext.RouteData.Values["action"].ToString();
            string controllerName = filterContext.RouteData.Values["controller"].ToString();
            //If v=m is in the query string, this is a minimal view and there is no point checking the
            // last modified date of the shared view
            bool includesShared = filterContext.HttpContext.Request.Query["v"] != "m";

            string viewPath = Path.Combine(BASE_VIEW_FOLDER, controllerName, actionName + ".cshtml");

            DateTime lastModifiedDate = new FileInfo(viewPath).LastWriteTime;

            if (includesShared)
            {
                string sharedViewPath = Path.Combine(BASE_VIEW_FOLDER, "Shared", "_Layout.cshtml");

                DateTime shareModifiedDate = new FileInfo(sharedViewPath).LastWriteTime;

                //Get the maximum of the two dates
                lastModifiedDate = lastModifiedDate > shareModifiedDate ? lastModifiedDate : shareModifiedDate;
            }

            CheckLastModified(lastModifiedDate, filterContext);
        }

        /// <summary>
        /// Checks if the page has been modified since the last version in the client's cache.
        /// If not, return a 304 Not Modified response.
        /// </summary>
        /// <param name="lastModifiedDateTime">The DateTime of the last page update</param>
        /// <param name="filterContext">The current action context</param>
        private static void CheckLastModified(DateTime lastModifiedDateTime, ActionExecutingContext filterContext)
        {
            // First, let the user know what the last modified date is so that they know what to ask for next time
            filterContext.HttpContext.Response.Headers.Add("Last-Modified", lastModifiedDateTime.ToString("R"));

            string clientLastModifiedString = filterContext.HttpContext.Request.Headers["If-Modified-Since"];
            // If this is the first time the client is fetching this page, this header won't be present
            if (DateTime.TryParseExact(clientLastModifiedString, "R", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal, out DateTime clientLastModifiedDateTime))
            {
                TimeSpan behind = lastModifiedDateTime - clientLastModifiedDateTime;
                //If the client's copy was modified within a second of the server copy, assume they are the same
                if (behind < TimeSpan.FromSeconds(1))
                {
                    //Client has the current version of the page cached
                    filterContext.Result = new StatusCodeResult(304);
                }
            }
        }
    }
}
