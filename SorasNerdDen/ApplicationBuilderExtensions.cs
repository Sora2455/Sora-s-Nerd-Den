﻿namespace SorasNerdDen
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Boilerplate.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.CookiePolicy;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using SorasNerdDen.Constants;
    using SorasNerdDen.Settings;

    public static partial class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Configure default cookie settings for the application which are more secure by default.
        /// </summary>
        public static IApplicationBuilder UseCookiePolicy(this IApplicationBuilder application) =>
            application.UseCookiePolicy(
                new CookiePolicyOptions()
                {
                    // Ensure that external script cannot access the cookie.
                    HttpOnly = HttpOnlyPolicy.Always,
                    // Ensure that the cookie can only be transported over HTTPS.
                    Secure = CookieSecurePolicy.Always
                });

        /// <summary>
        /// Adds developer friendly error pages for the application which contain extra debug and exception information.
        /// Note: It is unsafe to use this in production.
        /// </summary>
        public static IApplicationBuilder UseDeveloperErrorPages(this IApplicationBuilder application) =>
            application
                // When a database error occurs, displays a detailed error page with full diagnostic information. It is
                // unsafe to use this in production. Uncomment this if using a database.
                // .UseDatabaseErrorPage(DatabaseErrorPageOptions.ShowAll);
                // When an error occurs, displays a detailed error page with full diagnostic information.
                // See http://docs.asp.net/en/latest/fundamentals/diagnostics.html
                .UseDeveloperExceptionPage();

        /// <summary>
        /// Adds user friendly error pages.
        /// </summary>
        public static IApplicationBuilder UseErrorPages(this IApplicationBuilder application)
        {
            // Add error handling middle-ware which handles all HTTP status codes from 400 to 599 by re-executing
            // the request pipeline for the following URL. '{0}' is the HTTP status code e.g. 404.
            application.UseStatusCodePagesWithReExecute("/error/{0}/");

            // Returns a 500 Internal Server Error response when an unhandled exception occurs.
            application.UseInternalServerErrorOnException();

            return application;
        }

        /// <summary>
        /// Uses the static files middleware to serve static files. Also adds the Cache-Control and Pragma HTTP
        /// headers. The cache duration is controlled from configuration.
        /// See http://andrewlock.net/adding-cache-control-headers-to-static-files-in-asp-net-core/.
        /// </summary>
        public static IApplicationBuilder UseStaticFilesWithCacheControl(
            this IApplicationBuilder application,
            IConfigurationRoot configuration)
        {
            var cacheProfile = configuration
                .GetSection<CacheProfileSettings>()
                .CacheProfiles
                .First(x => string.Equals(x.Key, CacheProfileName.StaticFiles, StringComparison.Ordinal))
                .Value;
            return application
                .UseStaticFiles(
                    new StaticFileOptions()
                    {
                        OnPrepareResponse = context =>
                        {
                            context.Context.ApplyCacheProfile(cacheProfile);
                        }
                    });
        }

        /// <summary>
        /// Adds the Strict-Transport-Security HTTP header to responses. This HTTP header is only relevant if you are
        /// using TLS. It ensures that content is loaded over HTTPS and refuses to connect in case of certificate
        /// errors and warnings.
        /// See https://developer.mozilla.org/en-US/docs/Web/Security/HTTP_strict_transport_security and
        /// http://www.troyhunt.com/2015/06/understanding-http-strict-transport.html
        /// Note: Including subdomains and a minimum maxage of 18 weeks is required for preloading.
        /// Note: You can refer to the following article to clear the HSTS cache in your browser:
        /// http://classically.me/blogs/how-clear-hsts-settings-major-browsers
        /// </summary>
        public static IApplicationBuilder UseStrictTransportSecurityHttpHeader(this IApplicationBuilder application) =>
            application.UseHsts(options => options.MaxAge(days: 18 * 7).IncludeSubdomains().Preload());

        /// <summary>
        /// Adds the Content-Security-Policy (CSP) and/or Content-Security-Policy-Report-Only HTTP headers. This
        /// creates a white-list from where various content in a web page can be loaded from. See
        /// http://rehansaeed.com/content-security-policy-for-asp-net-mvc/,
        /// https://developer.mozilla.org/en-US/docs/Web/Security/CSP/CSP_policy_directives and
        /// https://github.com/NWebsec/NWebsec/wiki and for more information.
        /// Note: Filters can be applied to individual controllers and actions to override this base policy e.g. If an
        /// action requires access to content from YouTube.com, then you can add the following attribute to the action:
        /// [CspFrameSrc(CustomSources = "*.youtube.com")].
        /// </summary>
        public static IApplicationBuilder UseContentSecurityPolicyHttpHeader(
            this IApplicationBuilder application,
            int? sslPort,
            IHostingEnvironment hostingEnvironment) =>
            // Content-Security-Policy-Report-Only - Adds the Content-Security-Policy-Report-Only HTTP header to enable
            //      logging of violations without blocking them. This is good for testing CSP without enabling it.
            // application.UseCspReportOnly(...);
            // OR
            application.UseCsp(
                options =>
                {
                    options
                        // Enables logging of CSP violations. Register with the https://report-uri.io/ service to get a
                        // URL where you can send your CSP violation reports and view them.
                        .ReportUris(x => x.Uris("https://sorasnerdden.report-uri.com/r/d/csp/enforce"))
                        // upgrade-insecure-requests - This directive is only relevant if you are using HTTPS. Any
                        // objects on the page using HTTP are automatically upgraded to HTTPS.
                        // See https://scotthelme.co.uk/migrating-from-http-to-https-ease-the-pain-with-csp-and-hsts/
                        // and http://www.w3.org/TR/upgrade-insecure-requests/
                        .UpgradeInsecureRequests(sslPort ?? 443)
                        // default-src - Sets a default source list for a number of directives. If the other directives
                        // below are not used then this is the default setting.
                        .DefaultSources(x => x.Self())                    // We allow only ourselves by defualt
                        // base-uri - This directive restricts the document base URL
                        //            See http://www.w3.org/TR/html5/infrastructure.html#document-base-url.
                        // .BaseUris(x => ...)
                        // connect-src - This directive restricts which URIs the protected resource can load using
                        //               script interfaces (Ajax Calls and Web Sockets).
                        //.ConnectSources(x => x.Self())         // Allow all AJAX and Web Sockets calls from the same domain.
                        // font-src - This directive restricts from where the protected resource can load fonts.
                        //.FontSources(
                        //    x =>
                        //    {
                        //        x.Self();                                 // Allow all fonts from the same domain.
                        //        x.CustomSources(new string[]              // Allow fonts from the following sources.
                        //        {
                        //            // "*.example.com",                   // Allow AJAX and Web Sockets to example.com.
                        //        });
                        //    })
                        // form-action - This directive restricts which URLs can be used as the action of HTML form elements.
                        .FormActions(x => x.Self())              // Allow the current domain.
                        // frame-src - This directive restricts from where the protected resource can embed frames.
                        //             This is deprecated in favour of child-src but should still be used for older browsers.
                        // .FrameSources(x => ...)
                        // frame-ancestors - This directive restricts from where the protected resource can embed
                        //                   frame, iframe, object, embed or applet's.
                        .FrameAncestors(x => x.Self());
                        // img-src - This directive restricts from where the protected resource can load images.
                        //.ImageSources(x => x.Self())           // Allow the current domain.
                        // script-src - This directive restricts which scripts the protected resource can execute.
                        //              The directive also controls other resources, such as XSLT style sheets, which
                        //              can cause the user agent to execute script.
                        //.ScriptSources(x => x.Self())          // Allow all scripts from the same domain.
                        // media-src - This directive restricts from where the protected resource can load video and audio.
                        // .MediaSources(x => ...)
                        // object-src - This directive restricts from where the protected resource can load plug-ins.
                        // .ObjectSources(x => ...)
                        // plugin-types - This directive restricts the set of plug-ins that can be invoked. You can
                        //                also use the @Html.CspMediaType("application/pdf") HTML helper instead of this
                        //                attribute. The HTML helper will add the media type to the CSP header.
                        // .PluginTypes(x => x.MediaTypes("application/x-shockwave-flash", "application/xaml+xml"))
                        // style-src - This directive restricts which styles the user applies to the protected resource.
                        //.StyleSources(x => x.Self());                  // Allow all stylesheets from the same domain.
                });

        /// <summary>
        /// Adds the X-Content-Type-Options, X-Download-Options and X-Frame-Options HTTP headers to the response for
        /// added security. See
        /// http://rehansaeed.com/nwebsec-asp-net-mvc-security-through-http-headers/,
        /// http://www.dotnetnoob.com/2012/09/security-through-http-response-headers.html and
        /// https://github.com/NWebsec/NWebsec/wiki for more information.
        /// </summary>
        public static IApplicationBuilder UseSecurityHttpHeaders(this IApplicationBuilder application) =>
            application
                // X-Content-Type-Options - Adds the X-Content-Type-Options HTTP header. Stop IE9 and below from
                //                          sniffing files and overriding the Content-Type header (MIME type).
                .UseXContentTypeOptions()
                // X-Download-Options - Adds the X-Download-Options HTTP header. When users save the page, stops them
                //                      from opening it and forces a save and manual open.
                .UseXDownloadOptions()
                // X-Frame-Options - Adds the X-Frame-Options HTTP header. Stop clickjacking by stopping the page from
                //                   opening in an iframe or only allowing it from the same origin.
                //   SameOrigin - Specifies that the X-Frame-Options header should be set in the HTTP response,
                //                instructing the browser to display the page when it is loaded in an iframe - but only
                //                if the iframe is from the same origin as the page.
                //   Deny - Specifies that the X-Frame-Options header should be set in the HTTP response, instructing
                //          the browser to not display the page when it is loaded in an iframe.
                .UseXfo(options => options.SameOrigin());

        /// <summary>
        /// Declare to the users that we are not collecting their browsing habits
        /// </summary>
        public static IApplicationBuilder DeclareNotTracking(this IApplicationBuilder application) =>
            application.Use(next =>
            {
                return async context =>
                {
                    //Set the tracking header to 'No' so users know we are not tracking them
                    //https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Tk
                    context.Response.Headers["Tk"] = "N";
                    await next(context);
                };
            });
    }
}