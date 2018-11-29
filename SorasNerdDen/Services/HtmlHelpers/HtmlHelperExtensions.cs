using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SorasNerdDen.Services.HtmlHelpers
{
    /// <summary>
    /// An incomplete list of the emoji characters availible in the Unicode standard
    /// </summary>
    public enum Emoji : uint
    {
        /// <summary>≡</summary>
        TripleBar = 8801,
        /// <summary>⌛</summary>
        Hourglass = 8987,
        /// <summary>⏳</summary>
        HourglassWithFlowingSand = 9203,
        /// <summary>☆</summary>
        WhiteStar = 9734,
        /// <summary>☕</summary>
        HotBeverage = 9749,
        /// <summary>☠</summary>
        SkullAndCrossbones = 9760,
        /// <summary>☢</summary>
        RadioactiveSymbol = 9762,
        /// <summary>☣</summary>
        BiohazardSymbol = 9763,
        /// <summary>☤</summary>
        Caduceus = 9764,
        /// <summary>☮</summary>
        PeaceSymbol = 9774,
        /// <summary>⚠</summary>
        WarningSign = 9888,
        /// <summary>✝</summary>
        LatinCross = 10013,
        /// <summary>🍺</summary>
        BeerMug = 127866,
        /// <summary>🍻</summary>
        ClinkingBeerMugs = 127867,
        /// <summary>🏘</summary>
        Houses = 127960,
        /// <summary>🏠</summary>
        House = 127968,
        /// <summary>💁</summary>
        InformationDeskPerson = 128129,
        /// <summary>💡</summary>
        LightBulb = 128161,
        /// <summary>📁</summary>
        FileFolder = 128193,
        /// <summary>📂</summary>
        OpenFileFolder = 128194,
        /// <summary>📜</summary>
        Scroll = 128220,
        /// <summary>📞</summary>
        Telephone = 128222,
        /// <summary>📰</summary>
        Newspaper = 128240,
        /// <summary>📱</summary>
        MobilePhone = 128241,
        /// <summary>🔌</summary>
        ElectricPlug = 128268,
        /// <summary>🔍</summary>
        MagnifyingGlass = 128269,
        /// <summary>🔒</summary>
        Lock = 128274,
        /// <summary>🔓</summary>
        OpenLock = 128275,
        /// <summary>🔗</summary>
        LinkSymbol = 128279,
        /// <summary>🔦</summary>
        ElectricTorch = 128294,
        /// <summary>🗄</summary>
        FileCabinet = 128452,
        /// <summary>🗋</summary>
        BlankDocument = 128459,
        /// <summary>🗺</summary>
        WorldMap = 128506
    }

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
        /// <returns>A HtmlString of a picture tag of the nominated SVG with PNG fallback</returns>
        public static IHtmlContent SvgPicture(this IHtmlHelper helper, string imageName,
            string imageAltText, int height, int width, string cssClass = null, string cssId = null)
        {
            string classString = cssClass != null ? $" class=\"{cssClass}\"" : null;
            string idString = cssId != null ? $" id=\"{cssId}\"" : null;
            return new HtmlString(
                    $"<picture>" +
                        $"<source type=\"image/svg+xml\" srcset=\"/img/{imageName}.svg\">" +
                        $"<img src=\"/img/{imageName}.png\" alt=\"{imageAltText}\" " +
                            $"height=\"{height}\" width=\"{width}\"{classString}{idString}>" +
                    $"</picture>");
        }

        /// <summary>
        /// Takes a CamelCase string and converts it into the form 'Camel case'
        /// </summary>
        /// <param name="camelCase">The CamelCase string to convert</param>
        /// <returns>A human-readable string</returns>
        private static string DisplayCamelCaseString(string camelCase)
        {
            List<char> chars = new List<char> {camelCase[0]};
            foreach (char c in camelCase.Skip(1))
            {
                if (char.IsUpper(c))
                {
                    chars.Add(' ');
                    chars.Add(char.ToLower(c));
                }
                else chars.Add(c);
            }
            return new string(chars.ToArray());
        }

        /// <summary>
        /// Render an emoji character that is readable to both human sight and screen readers
        /// </summary>
        /// <param name="helper">The HTML helper being used to render the text</param>
        /// <param name="emoji">The <see cref="HtmlHelpers.Emoji"/> to render</param>
        /// <returns>A HtmlString of an emoji character wrapped in a span tag containing a description
        /// for screen-readers</returns>
        public static IHtmlContent Emoji(this IHtmlHelper helper, Emoji emoji)
        {
            string emojiName = DisplayCamelCaseString(emoji.ToString());
            return new HtmlString(
                $"<span role=\"img\" aria-label=\"{emojiName}\" tabindex=\"0\" class=\"emoji\">" +
                //The first HTML entity is the symbol we want to display
                // the second one tells the browser to render the symbol as text, not an image
                $"&#{(uint)emoji};&#xFE0E;</span>"
            );
        }

        /// <summary>
        /// Render an emoji character that is readable to both human sight and screen readers
        /// </summary>
        /// <param name="helper">The HTML helper being used to render the text</param>
        /// <param name="emoji">The <see cref="HtmlHelpers.Emoji"/> to render</param>
        /// <param name="cssClass">A CSS class to apply to the rendered element</param>
        /// <returns>A HtmlString of an emoji character wrapped in a span tag containing a description
        /// for screen-readers</returns>
        public static IHtmlContent Emoji(this IHtmlHelper helper, Emoji emoji, string cssClass)
        {
            string emojiName = DisplayCamelCaseString(emoji.ToString());
            return new HtmlString(
                $"<span role=\"img\" aria-label=\"{emojiName}\" tabindex=\"0\" class=\"{cssClass}\">" +
                //The first HTML entity is the symbol we want to display
                // the second one tells the browser to render the symbol as text, not an image
                $"&#{(uint)emoji};&#xFE0E;</span>"
            );
        }

        /// <summary>
        /// Place a human-and-computer readable date and time on the page
        /// (will be localised by JavaScript if possible)
        /// </summary>
        /// <param name="helper">The HTML helper being used to render the text</param>
        /// <param name="dateTime">The DateTimeOffset object to render</param>
        /// <returns>A HtmlString representing the passed DateTime</returns>
        public static IHtmlContent DateTime(this IHtmlHelper helper, DateTimeOffset dateTime)
        {
            //The dateTime in a format the computer will understand
            string computerString = dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss'Z'");
            string timeString = $"<time datetime=\"{computerString}\"></time>";
            return new HtmlString(timeString);
        }

        /// <summary>
        /// Place a human-and-computer readable date on the page
        /// (will be localised by JavaScript if possible)
        /// </summary>
        /// <param name="helper">The HTML helper being used to render the text</param>
        /// <param name="dateTime">The DateTime object to render (assumed to be in the local timezone)</param>
        /// <returns>A HtmlString representing the passed DateTime</returns>
        public static IHtmlContent Date(this IHtmlHelper helper, DateTime dateTime)
        {
            //The dateTime in a format the computer will understand
            string computerString = dateTime.ToUniversalTime().ToString("yyyy-MM-dd");
            //The dateTime in a format a human will understand
            string humanString = dateTime.ToString("dddd, dd MMM yyyy");
            //The name of the timezone at this computer
            string timeZoneName = TimeZoneInfo.Local.StandardName;
            string timeString = $"<time datetime=\"{computerString}\">{humanString} {timeZoneName}</time>";
            return new HtmlString(timeString);
        }

        /// <summary>
        /// Place a human-and-computer readable time on the page
        /// (will be localised by JavaScript if possible)
        /// </summary>
        /// <param name="helper">The HTML helper being used to render the text</param>
        /// <param name="dateTime">The DateTime object to render (assumed to be in the local timezone)</param>
        /// <returns>A HtmlString representing the passed DateTime</returns>
        public static IHtmlContent Time(this IHtmlHelper helper, DateTime dateTime)
        {
            //The dateTime in a format the computer will understand
            string computerString = dateTime.ToUniversalTime().ToString("HH:mm:ssZ");
            //The dateTime in a format a human will understand
            string humanString = dateTime.ToString("h:mm:ss tt");
            //The name of the timezone at this computer
            string timeZoneName = TimeZoneInfo.Local.StandardName;
            string timeString = $"<time datetime=\"{computerString}\">{humanString} {timeZoneName}</time>";
            return new HtmlString(timeString);
        }
    }
}
