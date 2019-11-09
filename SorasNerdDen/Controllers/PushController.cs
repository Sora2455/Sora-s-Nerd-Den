namespace SorasNerdDen.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using SorasNerdDen.Constants;
    using SorasNerdDen.Settings;
    using System;
    using System.Threading.Tasks;
    using WebPush;

    public class PushController : Controller
    {
        private readonly IOptionsSnapshot<VapidSettings> vapidSettings;

        private static readonly WebPushClient webPushClient = new WebPushClient();

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
            /// <summary>
            /// A secret unique to this push subscription
            /// </summary>
            public string auth;
            /// <summary>
            /// The public key for this push subscription
            /// (the private key remains on the browser)
            /// </summary>
            public string p256dh;
        }

        /// <summary>
        /// The C# model of JSON data posted when push notifications are being subscribed to
        /// or unsubscribed from
        /// </summary>
        public class PushSubscriptionModel
        {
            /// <summary>
            /// The URL the push subscription calls to deliver the push message
            /// </summary>
            public string endpoint;
            /// <summary>
            /// The encryption keys for this push subscription
            /// </summary>
            public PushSubscriptionKeys keys;
            /// <summary>
            /// The expiration date of this push subscription (if any)
            /// </summary>
            public long? expirationTime;
        }

        [HttpPost("push/subscribe", Name = PushControllerRoute.Subscribe)]
        public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionModel model)
        {
            await SendNotification(model, "It works!", "Thank you for enabling live updates.");
            return new EmptyResult();
        }

        [HttpPost("push/unsubscribe", Name = PushControllerRoute.Unsubscribe)]
        public IActionResult Unubscribe([FromBody] PushSubscriptionModel model)
        {
            return new EmptyResult();
        }

        [HttpPost("push/update", Name = PushControllerRoute.Update)]
        public async Task<IActionResult> Update([FromBody] PushSubscriptionModel model)
        {
            await SendNotification(model, "Hello there", "Your push subscription has auto-renewed.");
            return new EmptyResult();
        }

        private async Task SendNotification(PushSubscriptionModel model, string title, string message)
        {
            var subscription = new PushSubscription(model.endpoint, model.keys?.p256dh, model.keys?.auth);
            VapidDetails vapidDetails = new VapidDetails("mailto:example@example.com",
                vapidSettings.Value.PublicKey, vapidSettings.Value.PrivateKey);
            try
            {
                await webPushClient.SendNotificationAsync(
                    subscription,
                    $"{{\"title\":\"{title}\",\"message\":\"{message}\"}}",
                    vapidDetails
                );
            }
            catch (WebPushException exception)
            {
                switch ((int)exception.StatusCode)
                {
                    case 400:
                        Console.WriteLine("Invalid request (usually malformed headers)");
                        break;
                    case 404:
                        Console.WriteLine("Subscription not found (has expired, we should forget about it)");
                        break;
                    case 410:
                        Console.WriteLine("Subscription gone (has expired, we should forget about it)");
                        break;
                    case 413:
                        Console.WriteLine("Payload too large (max payload size is 4kb)");
                        break;
                    case 429:
                        Console.WriteLine("Too many requests");
                        break;
                    default:
                        Console.WriteLine("Http STATUS code" + exception.StatusCode);
                        break;
                }
            }
        }
    }
}
