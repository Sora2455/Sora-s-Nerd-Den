namespace SorasNerdDen.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using SorasNerdDen.Constants;
    using SorasNerdDen.Settings;
    using System;
    using WebPush;

    public class PushController : Controller
    {
        private readonly IOptionsSnapshot<VapidSettings> vapidSettings;

        public PushController(IOptionsSnapshot<VapidSettings> vapidSettings)
        {
            this.vapidSettings = vapidSettings;
        }

        [HttpGet("push/publicKey", Name = PushControllerRoute.GetPublicKey)]
        public IActionResult PublicKey()
        {
            return Content(vapidSettings.Value.PublicKey);
        }

        public class PushSubscriptionKeys
        {
            public string auth;
            public string p256dh;
        }

        /// <summary>
        /// The C# model of JSON data posted when push notifications are being subscribed to,
        /// unsubscribed from, or updated
        /// </summary>
        public class PushSubscriptionChangeModel
        {
            /// <summary>
            /// The URL the push subscription calls to deliver the push message
            /// </summary>
            public string endpoint;
            public PushSubscriptionKeys keys;
            /// <summary>
            /// The timestamp the push subscription will expire (if any)
            /// </summary>
            public long? expires;
            public string oldEndpoint;
            public string newEndpoint;
        }

        [HttpPost("push/subscribe", Name = PushControllerRoute.Subscribe)]
        public IActionResult Subscribe([FromBody] PushSubscriptionChangeModel model)
        {
            var subscription = new PushSubscription(model.endpoint, model.keys?.p256dh, model.keys?.auth);
            VapidDetails vapidDetails = new VapidDetails("mailto:example@example.com",
                vapidSettings.Value.PublicKey, vapidSettings.Value.PrivateKey);
            //options["gcmAPIKey"] = @"[your key here]";
            var webPushClient = new WebPushClient();
            try
            {
                webPushClient.SendNotification(
                    subscription,
                    "{\"title\":\"It works!\",\"message\":\"Thank you for enabling live updates.\"}",
                    vapidDetails
                );
            }
            catch (WebPushException exception)
            {
                Console.WriteLine("Http STATUS code" + exception.StatusCode);
            }
            return new EmptyResult();
        }

        [HttpPost("push/unsubscribe", Name = PushControllerRoute.Unsubscribe)]
        public IActionResult Unubscribe([FromBody] PushSubscriptionChangeModel model)
        {
            return new EmptyResult();
        }

        [HttpPost("push/update", Name = PushControllerRoute.Update)]
        public IActionResult Update([FromBody] PushSubscriptionChangeModel model)
        {
            return new EmptyResult();
        }
    }
}
