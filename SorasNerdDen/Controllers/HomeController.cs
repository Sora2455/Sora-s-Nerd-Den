namespace SorasNerdDen.Controllers
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Boilerplate.AspNetCore;
    using Boilerplate.AspNetCore.Filters;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using SorasNerdDen.Attributes;
    using SorasNerdDen.Constants;
    using SorasNerdDen.Services;
    using SorasNerdDen.Settings;

    public class HomeController : Controller
    {
        // Hardcoded for demo purposes
        private static readonly DateTime lastModifiedDate = new DateTime(2017, 11, 4);

        private readonly IOptionsSnapshot<AppSettings> appSettings;
        private readonly IFeedService feedService;
        private readonly IRobotsService robotsService;
        private readonly ISitemapService sitemapService;

        public HomeController(
            IFeedService feedService,
            IRobotsService robotsService,
            ISitemapService sitemapService,
            IOptionsSnapshot<AppSettings> appSettings)
        {
            this.appSettings = appSettings;
            this.feedService = feedService;
            this.robotsService = robotsService;
            this.sitemapService = sitemapService;
        }

        [HttpGet("", Name = HomeControllerRoute.GetIndex)]
        [AddVersionHeader]
        public IActionResult Index()
        {
            return View(HomeControllerAction.Index);
        }

        [HttpGet("about", Name = HomeControllerRoute.GetAbout)]
        [AddVersionHeader]
        public IActionResult About()
        {
            return View(HomeControllerAction.About);
        }

        [HttpGet("contact", Name = HomeControllerRoute.GetContact)]
        [AddVersionHeader]
        public IActionResult Contact()
        {
            return View(HomeControllerAction.Contact);
        }

        [HttpGet("loading", Name = HomeControllerRoute.GetLoading)]
        public IActionResult Loading()
        {
            return View(HomeControllerAction.Loading);
        }

        [HttpGet("offline", Name = HomeControllerRoute.GetOffline)]
        public IActionResult Offline()
        {
            return View(HomeControllerAction.Offline);
        }

        /// <summary>
        /// Gets the Atom 1.0 feed for the current site. Note that Atom 1.0 is used over RSS 2.0 because Atom 1.0 is a
        /// newer and more well defined format. Atom 1.0 is a standard and RSS is not. See
        /// http://rehansaeed.com/building-rssatom-feeds-for-asp-net-mvc/
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> signifying if the request is cancelled.
        /// See http://www.davepaquette.com/archive/2015/07/19/cancelling-long-running-queries-in-asp-net-mvc-and-web-api.aspx</param>
        /// <returns>The Atom 1.0 feed for the current site.</returns>
        [ResponseCache(CacheProfileName = CacheProfileName.Feed)]
        [Route("feed", Name = HomeControllerRoute.GetFeed)]
        public async Task<IActionResult> Feed(CancellationToken cancellationToken)
        {
            return Content(await feedService.GetFeed(cancellationToken), ContentType.Atom, Encoding.Unicode);
        }

        /// <summary>
        /// Tells search engines (or robots) how to index your site.
        /// The reason for dynamically generating this code is to enable generation of the full absolute sitemap URL
        /// and also to give you added flexibility in case you want to disallow search engines from certain paths. The
        /// sitemap is cached for one day, adjust this time to whatever you require. See
        /// http://rehansaeed.com/dynamically-generating-robots-txt-using-asp-net-mvc/
        /// </summary>
        /// <returns>The robots text for the current site.</returns>
        [NoTrailingSlash]
        [ResponseCache(CacheProfileName = CacheProfileName.RobotsText)]
        [Route("robots.txt", Name = HomeControllerRoute.GetRobotsText)]
        public IActionResult RobotsText()
        {
            string content = this.robotsService.GetRobotsText();
            return this.Content(content, ContentType.Text, Encoding.UTF8);
        }

        /// <summary>
        /// Gets the sitemap XML for the current site. You can customize the contents of this XML from the
        /// <see cref="SitemapService"/>. The sitemap is cached for one day, adjust this time to whatever you require.
        /// http://www.sitemaps.org/protocol.html
        /// </summary>
        /// <param name="index">The index of the sitemap to retrieve. <c>null</c> if you want to retrieve the root
        /// sitemap file, which may be a sitemap index file.</param>
        /// <returns>The sitemap XML for the current site.</returns>
        [NoTrailingSlash]
        [Route("sitemap.xml", Name = HomeControllerRoute.GetSitemapXml)]
        public async Task<IActionResult> SitemapXml(int? index = null)
        {
            string content = await this.sitemapService.GetSitemapXml(index);

            if (content == null)
            {
                return this.BadRequest("Sitemap index is out of range.");
            }

            return this.Content(content, ContentType.Xml, Encoding.UTF8);
        }
    }
}