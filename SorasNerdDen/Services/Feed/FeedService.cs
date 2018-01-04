namespace SorasNerdDen.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Boilerplate.AspNetCore;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using Microsoft.SyndicationFeed;
    using Microsoft.SyndicationFeed.Atom;
    using SorasNerdDen.Constants;
    using SorasNerdDen.Settings;
    using System.Xml;

    /// <summary>
    /// Builds <see cref="SyndicationFeed"/>'s containing meta data about the feed and the feed entries.
    /// Note: We are targeting Atom 1.0 over RSS 2.0 because Atom 1.0 is a newer and more well defined format. Atom 1.0
    /// is a standard and RSS is not. See http://rehansaeed.com/building-rssatom-feeds-for-asp-net-mvc/.
    /// </summary>
    public sealed class FeedService : IFeedService
    {
        /// <summary>
        /// The feed universally unique identifier. Do not use the URL of your feed as this can change.
        /// A much better ID is to use a GUID which you can generate from Tools->Create GUID in Visual Studio.
        /// </summary>
        private const string FeedId = "[INSERT GUID HERE]";
        private const string PubSubHubbubHubUrl = "https://pubsubhubbub.appspot.com/";

        private readonly IOptionsSnapshot<AppSettings> appSettings;
        private readonly HttpClient httpClient;
        private readonly IUrlHelper urlHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeedService"/> class.
        /// </summary>
        /// <param name="appSettings">The application settings.</param>
        /// <param name="urlHelper">The URL helper.</param>
        public FeedService(
            IOptionsSnapshot<AppSettings> appSettings,
            IUrlHelper urlHelper)
        {
            this.appSettings = appSettings;
            this.urlHelper = urlHelper;
            this.httpClient = new HttpClient();
        }

        /// <summary>
        /// Gets the feed containing meta data about the feed and the feed entries.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> signifying if the request is cancelled.</param>
        /// <returns>A <see cref="SyndicationFeed"/>.</returns>
        public async Task<string> GetFeed(CancellationToken cancellationToken, DateTimeOffset? lastUpdated = null)
        {
            var sw = new StringWriter();
            
            using (XmlWriter xmlWriter = XmlWriter.Create(sw, new XmlWriterSettings() { Async = true, Indent = true }))
            {
                var writer = new AtomFeedWriter(xmlWriter);

                // id (Required) - The feed universally unique identifier.
                await writer.WriteId(FeedId);

                // title (Required) - Contains a human readable title for the feed. Often the same as the title of the
                //                    associated website. This value should not be blank.
                await writer.WriteTitle(appSettings.Value.SiteTitle);

                // subtitle (Recommended) - Contains a human-readable description or subtitle for the feed.
                // await writer.WriteDescription();// TODO not in atom?

                // updated (Optional) - Indicates the last time the feed was modified in a significant way.
                await writer.WriteUpdated(lastUpdated ?? DateTimeOffset.Now);

                // self link (Required) - The URL for the syndication feed.
                await writer.Write(new SyndicationLink(
                    new Uri(urlHelper.AbsoluteRouteUrl(HomeControllerRoute.GetFeed)),
                    "self"));

                // alternate link (Recommended) - The URL for the web page showing the same data as the syndication feed.
                await writer.Write(new SyndicationLink(
                    new Uri(urlHelper.AbsoluteRouteUrl(HomeControllerRoute.GetIndex))));

                // hub link (Recommended) - The URL for the PubSubHubbub hub. Used to push new entries to subscribers
                //                          instead of making them poll the feed. See feed updated method below.
                await writer.Write(new SyndicationLink(new Uri(PubSubHubbubHubUrl), "hub"));

                // logo (Optional) - Identifies a larger image which provides visual identification for the feed.
                //                   Images should be twice as wide as they are tall.
                await writer.Write(new SyndicationImage(
                    new Uri(urlHelper.AbsoluteContent("favicon-wide.svg")),
                    "logo"));

                // icon (Optional) - Identifies a small image which provides iconic visual identification for the feed.
                //                   Icons should be square.
                await writer.Write(new SyndicationImage(
                    new Uri(urlHelper.AbsoluteContent("favicon.svg")),
                    "icon"));

                // rights (Optional) - Conveys information about rights, e.g. copyrights, held in and over the feed.
                await writer.WriteRights($"© {DateTime.Now.Year} - {appSettings.Value.SiteTitle}");

                // author (Recommended) - Names one author of the feed. A feed may have multiple author elements. A feed
                //                        must contain at least one author element unless all of the entry elements contain
                //                        at least one author element.
                await writer.Write(GetPerson());

                // items (Required) - The items to add to the feed.
                foreach (var item in await GetItems(cancellationToken))
                {
                    await writer.Write(item);
                }

                await xmlWriter.FlushAsync();
            }

            return sw.ToString();

            //SyndicationFeed feed = new SyndicationFeed()
            //{
            //    // lang (Optional) - The language of the feed.
            //    Language = "en-GB"
            //};
        }

        /// <summary>
        /// Publishes the fact that the feed has updated to subscribers using the PubSubHubbub v0.4 protocol.
        /// </summary>
        /// <remarks>
        /// The PubSubHubbub is an open standard created by Google which allows subscription of feeds and allows
        /// updates to be pushed to them rather than them having to poll the feed. This means subscribers get live
        /// updates as they happen and also we may save some bandwidth because we have less polling of our feed.
        /// See https://pubsubhubbub.googlecode.com/git/pubsubhubbub-core-0.4.html for PubSubHubbub v0.4 specification.
        /// See https://github.com/pubsubhubbub for PubSubHubbub GitHub projects.
        /// See http://pubsubhubbub.appspot.com/ for Google's implementation of the PubSubHubbub hub we are using.
        /// </remarks>
        public Task PublishUpdate()
        {
            return httpClient.PostAsync(
                PubSubHubbubHubUrl,
                new FormUrlEncodedContent(
                    new KeyValuePair<string, string>[]
                    {
                        new KeyValuePair<string, string>("hub.mode", "publish"),
                        new KeyValuePair<string, string>(
                            "hub.url",
                            this.urlHelper.AbsoluteRouteUrl(HomeControllerRoute.GetFeed))
                    }));
        }

        private SyndicationPerson GetPerson()
        {
            return new SyndicationPerson("Sora Neku", "boreenpt@gmail.com", AtomContributorTypes.Author);
        }

        /// <summary>
        /// Gets the collection of <see cref="SyndicationItem"/>'s that represent the atom entries.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> signifying if the request is cancelled.</param>
        /// <returns>A collection of <see cref="SyndicationItem"/>'s.</returns>
        private Task<List<SyndicationItem>> GetItems(CancellationToken cancellationToken)
        {
            List<SyndicationItem> items = new List<SyndicationItem>();

            for (int i = 1; i < 4; ++i)
            {
                SyndicationItem item = new SyndicationItem()
                {
                    // id (Required) - Identifies the entry using a universally unique and permanent URI. Two entries
                    //                 in a feed can have the same value for id if they represent the same entry at
                    //                 different points in time.
                    Id = FeedId + i,
                    // title (Required) - Contains a human readable title for the entry. This value should not be blank.
                    Title = "Item " + i,
                    // description (Recommended) - A summary of the entry.
                    Description = "A summary of item " + i,
                    // updated (Optional) - Indicates the last time the entry was modified in a significant way. This
                    //                      value need not change after a typo is fixed, only after a substantial
                    //                      modification. Generally, different entries in a feed will have different
                    //                      updated timestamps.
                    LastUpdated = DateTimeOffset.Now,
                    // published (Optional) - Contains the time of the initial creation or first availability of the entry.
                    Published = DateTimeOffset.Now
                };

                // link (Recommended) - Identifies a related Web page. An entry must contain an alternate link if there
                //                      is no content element.
                item.AddLink(new SyndicationLink(
                    new Uri(urlHelper.AbsoluteRouteUrl(HomeControllerRoute.GetIndex)),
                    "alternative"));
                // AND/OR
                // Text content  (Optional) - Contains or links to the complete content of the entry. Content must be
                //                            provided if there is no alternate link.
                // item.Content = SyndicationContent.CreatePlaintextContent("The actual plain text content of the entry");
                // HTML content (Optional) - Content can be plain text or HTML. Here is a HTML example.
                // item.Content = SyndicationContent.CreateHtmlContent("The actual HTML content of the entry");

                // author (Optional) - Names one author of the entry. An entry may have multiple authors. An entry must
                //                     contain at least one author element unless there is an author element in the
                //                     enclosing feed, or there is an author element in the enclosed source element.
                item.AddContributor(GetPerson());

                // category (Optional) - Specifies a category that the entry belongs to. A entry may have multiple
                //                       category elements.
                item.AddCategory(new SyndicationCategory("CategoryName"));

                items.Add(item);
            }

            return Task.FromResult(items);
        }
    }
}