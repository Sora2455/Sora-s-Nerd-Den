using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Globalization;

namespace SorasNerdDen.Controllers
{
    public class BaseController : Controller
    {
        private const string BASE_VIEW_FOLDER = "Views";

        /// <summary>
        /// Returns true if the client is requesting the body of the page without the header/footer
        /// </summary>
        protected bool IsMinimalViewRequest()
        {
            return Request.Query["v"] == "m";
        }

        /// <summary>
        /// Checks if the view has been modified since the last version in the client's cache
        /// </summary>
        /// <param name="controllerName">The name of the controller that controls this view</param>
        /// <param name="actionName">The name of the action that returns this view</param>
        /// <param name="includesShared">True if the age of the shared view should also be
        /// taken into account</param>
        /// <returns>False if the client has the current version of the view chached, true otherwise</returns>
        protected bool CheckLastModified(string controllerName, string actionName, bool includesShared)
        {
            string viewPath = Path.Combine(BASE_VIEW_FOLDER,
                controllerName.Replace("Controller", ""), actionName + ".cshtml");

            DateTime lastModifiedDate = new FileInfo(viewPath).LastWriteTime;

            if (includesShared)
            {
                string sharedViewPath = Path.Combine(BASE_VIEW_FOLDER, "Shared", "_Layout.cshtml");

                DateTime shareModifiedDate = new FileInfo(sharedViewPath).LastWriteTime;

                //Get the maximum of the two dates
                lastModifiedDate = lastModifiedDate > shareModifiedDate ? lastModifiedDate : shareModifiedDate;
            }

            return CheckLastModified(lastModifiedDate);
        }

        /// <summary>
        /// Checks if the page has been modified since the last version in the client's cache
        /// </summary>
        /// <param name="lastModifiedDateTime">The DateTime of the last page update</param>
        /// <returns>False if the client has the current version of the view chached, true otherwise</returns>
        protected bool CheckLastModified(DateTime lastModifiedDateTime)
        {
            // First, let the user know what the last modified date is so that they know what to ask for next time
            Response.Headers.Add("Last-Modified", lastModifiedDateTime.ToUniversalTime().ToString("R"));

            string clientLastModifiedString = Request.Headers["If-Modified-Since"];
            // If this is the first time the client is fetching this page, this header won't be present
            if (DateTime.TryParseExact(clientLastModifiedString, "R", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out DateTime clientLastModifiedDateTime))
            {
                if (clientLastModifiedDateTime.ToLocalTime() >= lastModifiedDateTime) return false;
            }

            return true;
        }

        /// <summary>
        /// Return this to let the user know that the content has not been modified since they last fetched it
        /// </summary>
        /// <returns>A StatusCodeResult of 304</returns>
        protected StatusCodeResult ContentNotModified()
        {
            return StatusCode(304);
        }

        /// <summary>
        /// Return this to let the user know that the content has not been modified since they last fetched it
        /// </summary>
        /// <param name="value">The value to set on the Microsoft.AspNetCore.Mvc.ObjectResult.</param>
        /// <returns>A StatusCodeResult of 304</returns>
        protected ObjectResult ContentNotModified(object value)
        {
            return StatusCode(304, value);
        }
    }
}
