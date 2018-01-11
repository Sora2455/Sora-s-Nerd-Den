namespace SorasNerdDen.Constants
{
    public static class ContentDeliveryNetwork
    {
        public static class Polyfill
        {
            public const string Domain = "cdn.polyfill.io";
            private const string polyfillQueryString = "?features=default,fetch&flags=gated";
            public const string PolyfillDevUrl = "https://cdn.polyfill.io/v2/polyfill.js" + polyfillQueryString;
            public const string PolyfillProdUrl = "https://cdn.polyfill.io/v2/polyfill.min.js" + polyfillQueryString;
        }
    }
}