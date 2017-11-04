﻿namespace SorasNerdDen.Constants
{
    public static class ContentDeliveryNetwork
    {
        public static class Google
        {
            public const string Domain = "ajax.googleapis.com";
            public const string JQueryUrl = "https://ajax.googleapis.com/ajax/libs/jquery/3.1.1/jquery.min.js";
        }

        public static class MaxCdn
        {
            public const string Domain = "maxcdn.bootstrapcdn.com";
            public const string FontAwesomeUrl = "https://maxcdn.bootstrapcdn.com/font-awesome/4.7.0/css/font-awesome.min.css";
        }

        public static class Microsoft
        {
            public const string Domain = "ajax.aspnetcdn.com";
            public const string BootstrapUrl = "https://ajax.aspnetcdn.com/ajax/bootstrap/3.3.7/bootstrap.min.js";
        }

        public static class Polyfill
        {
            public const string Domain = "cdn.polyfill.io";
            private const string polyfillQueryString = "?features=default,fetch&flags=gated";
            public const string PolyfillDevUrl = "https://cdn.polyfill.io/v2/polyfill.js" + polyfillQueryString;
            public const string PolyfillProdUrl = "https://cdn.polyfill.io/v2/polyfill.min.js" + polyfillQueryString;
        }
    }
}