using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SorasNerdDen.Services.HtmlHelpers
{
    public static class HtmlHelperExtensions
    {
        /// <summary>
        /// Renders a HTML picture tag with an SVG source and a PNG fallback
        /// </summary>
        /// <param name="helper">The HTML helper being used to render the text</param>
        /// <param name="imageName">The filename of an image in the Images folder</param>
        /// <param name="imageAltText">The alternate text of the image
        /// (for if the file doesn't load or the use is visually impaired)</param>
        /// <param name="height">The height of the image in pixels</param>
        /// <param name="width">The width of the image in pixels</param>
        /// <param name="cssClass">The css class to apply to the pitcure (null if not applicable)</param>
        /// <param name="cssId">The css id to apply to the picture (null if not applicable)</param>
        /// <param name="lazy">True if the image is to be lazy loaded, false otherwise</param>
        /// <returns>A HtmlString of a picture tag of the nominated SVG with PNG fallback</returns>
        public static IHtmlContent SvgPicture(this IHtmlHelper helper, string imageName,
            string imageAltText, int height, int width, string cssClass = null, string cssId = null,
            bool lazy = true)
        {
            string classString = cssClass != null ? $" class=\"{cssClass}\"" : null;
            string idString = cssId != null ? $" id=\"{cssId}\"" : null;
            string lazyPre = lazy ? @"<noscript data-lazy-load>" : null;
            string lazyPost = lazy ? @"</noscript>" : null;
            return new HtmlString(
                    $"{lazyPre}<picture>" +
                        $"<source type=\"image/svg+xml\" srcset=\"/img/svg/{imageName}.svg\">" +
                        $"<img src=\"/img/fallback/{imageName}.png\" alt=\"{imageAltText}\"" +
                            $"height=\"{height}\" width=\"{width}\"{classString}{idString}>" +
                    $"</picture>{lazyPost}");
        }
    }
}
