namespace SorasNerdDen.Controllers
{
    using Boilerplate.AspNetCore;
    using Microsoft.AspNetCore.Mvc;
    using SorasNerdDen.Constants;

    /// <summary>
    /// Provides methods that respond to HTTP requests with HTTP errors.
    /// </summary>
    [Route("[controller]")]
    public sealed class ErrorController : Controller
    {
        /// <summary>
        /// Gets the error view for the specified HTTP error status code. Returns a <see cref="PartialViewResult"/> if
        /// the request is an Ajax request, otherwise returns a full <see cref="ViewResult"/>.
        /// </summary>
        /// <param name="statusCode">The HTTP error status code.</param>
        /// <param name="status">The name of the HTTP error status code e.g. 'notfound'. This is not used but is here
        /// for aesthetic purposes.</param>
        /// <returns>A <see cref="PartialViewResult"/> if the request is an Ajax request, otherwise returns a full
        /// <see cref="ViewResult"/> containing the error view.</returns>
        [ResponseCache(CacheProfileName = CacheProfileName.Error)]
        [HttpGet("{statusCode:int:range(400, 599)}/{status?}", Name = ErrorControllerRoute.GetError)]
        public IActionResult Error(int statusCode, string status)
        {
            this.Response.StatusCode = statusCode;

            ActionResult result;
            if (this.Request.IsAjaxRequest())
            {
                // This allows us to show errors even in partial views.
                result = this.PartialView(ErrorControllerAction.Error, statusCode);
            }
            else
            {
                result = this.View(ErrorControllerAction.Error, statusCode);
            }

            return result;
        }

        /// <summary>
        /// The C# model of JSON data posted whenever there is an error in the client-side code
        /// </summary>
        public class JavaScriptErrorModel
        {
            public string Page;
            public string Message;
            public int? Line;
            public int? Column;
            public string StackTrace;
        }

        /// <summary>
        /// Logs a JavaScript error for later debugging
        /// </summary>
        [HttpPost("scripterror", Name = ErrorControllerRoute.ScriptError)]
        public IActionResult ScriptError([FromBody] JavaScriptErrorModel error)
        {
            //TODO - log this information somewhere!
            return new EmptyResult();
        }

        /// <summary>
        /// The C# model of JSON data posted whenever there is a page load client side that takes longer than our goal time
        /// </summary>
        public class PageLoadTimeModel
        {
            /// <summary>
            /// The time it took to download and parse the HTML of this page
            /// </summary>
            public decimal Interactive;
            /// <summary>
            /// The total loading time of the page (scripts and images)
            /// </summary>
            public decimal Total;
            /// <summary>
            /// The URL of the page the navigation was to
            /// </summary>
            public string To;
        }

        /// <summary>
        /// Logs a slow-loading page for later improvement
        /// </summary>
        [HttpPost("longloadingtime", Name = ErrorControllerRoute.LongLoadingTime)]
        public IActionResult LongLoadingTime([FromBody] PageLoadTimeModel loadTime)
        {
            //TODO - log this information somewhere!
            return new EmptyResult();
        }
    }
}