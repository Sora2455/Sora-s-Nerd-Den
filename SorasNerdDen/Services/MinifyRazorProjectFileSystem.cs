using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using WebMarkupMin.Core;

/// <summary>
/// A custom Razor engine that minifies the output at compile time
/// </summary>
/// <remarks>
/// Solution taken from http://www.guardrex.com/post/razor-minification.html and https://github.com/aspnet/Razor/issues/2422
/// </remarks>
public class MinifyRazorProjectFileSystem : RazorProjectFileSystem
{
    private readonly RazorProjectFileSystem _inner;

    public MinifyRazorProjectFileSystem(RazorProjectFileSystem inner)
    {
        _inner = inner;
    }

    public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
    {
        var items = _inner.EnumerateItems(basePath);
        return items.Select(i => new MinifiedRazorProjectItem(i));
    }

    public override RazorProjectItem GetItem(string path)
    {
        var item = _inner.GetItem(path);

        return new MinifiedRazorProjectItem(item);
    }

    private class MinifiedRazorProjectItem : RazorProjectItem
    {
        private readonly RazorProjectItem _inner;

        public MinifiedRazorProjectItem(RazorProjectItem inner)
        {
            _inner = inner;
        }

        // If pages fail due to missing attribute quotes, add:
        //
        // AttributeQuotesRemovalMode = HtmlAttributeQuotesRemovalMode.KeepQuotes,
        //
        // to the HtmlMinificationSettings.
        private HtmlMinifier _htmlMinifier = new HtmlMinifier(new HtmlMinificationSettings()
        {
            RemoveOptionalEndTags = false,
        });

        public override string BasePath => _inner.BasePath;

        public override string FilePath => _inner.FilePath;

        public override string PhysicalPath => _inner.PhysicalPath;

        public override bool Exists => _inner.Exists;

        public override Stream Read()
        {
            if (PhysicalPath.EndsWith("_ViewStart.cshtml") || PhysicalPath.EndsWith("_ViewImports.cshtml"))
            {
                //We don't modify the purely code files
                return _inner.Read();
            }
            return Minify(_inner.Read());
        }

        private Stream Minify(Stream markup)
        {
            string markupString = new StreamReader(markup).ReadToEnd();

            // Seperate out the import statements from the start of the file (we don't minify those)
            string html = string.Empty;
            string directives = string.Empty;
            int markupStart = markupString.IndexOf("\r\n<");
            if (markupStart != -1)
            {
                directives = markupString.Substring(0, markupStart + 2);
                html = markupString.Substring(markupStart + 2);
            }
            else
            {
                html = markupString;
            }

            MarkupMinificationResult result = _htmlMinifier.Minify(html, string.Empty, Encoding.UTF8, true);

            if (result.Errors.Count == 0)
            {
                MinificationStatistics statistics = result.Statistics;
                if (statistics != null)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Original size: {statistics.OriginalSize:N0} Bytes | Minified size: {statistics.MinifiedSize:N0} Bytes | Saved: {statistics.SavedInPercent:N2}%");
                }
                //Console.WriteLine($"{Environment.NewLine}Minified content:{Environment.NewLine}{Environment.NewLine}{result.MinifiedContent}");

                string minifiedString = directives + result.MinifiedContent;
                byte[] minifiedBytes = Encoding.UTF8.GetBytes(minifiedString);
                return new MemoryStream(minifiedBytes);
            }
            else
            {
                IList<MinificationErrorInfo> errors = result.Errors;

                Console.WriteLine();
                Console.WriteLine($"Found {errors.Count:N0} error(s):");

                foreach (var error in errors)
                {
                    Console.WriteLine($" - Line {error.LineNumber}, Column {error.ColumnNumber}: {error.Message}");
                }

                return markup;
            }
        }
    }
}