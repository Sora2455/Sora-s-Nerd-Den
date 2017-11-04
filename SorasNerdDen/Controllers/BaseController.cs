using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;

namespace SorasNerdDen.Controllers
{
    public class BaseController : Controller
    {
        /// <summary>
        /// Checks if the page has been modified since the last version in the client's cache
        /// </summary>
        /// <param name="lastModifiedDateTime">The DateTime of the last page update</param>
        /// <returns>True if the page has been modified since the client last fetched it,
        ///  false otherwise</returns>
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
