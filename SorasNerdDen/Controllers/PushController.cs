namespace SorasNerdDen.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using SorasNerdDen.Constants;
    using SorasNerdDen.Settings;
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Serialization;
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

        public class PushSubscriptionUpdateModel
        {
            public PushSubscriptionModel newSubscription;
            public string oldEndpoint;
        }

        [HttpPost("push/subscribe", Name = PushControllerRoute.Subscribe)]
        public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionModel model)
        {
            await SendNotification(model,
                new PushPayload("It works!", "Thank you for enabling live updates."));
            return new EmptyResult();
        }

        [HttpPost("push/unsubscribe", Name = PushControllerRoute.Unsubscribe)]
        public IActionResult Unubscribe([FromBody] PushSubscriptionModel model)
        {
            return new EmptyResult();
        }

        [HttpPost("push/update", Name = PushControllerRoute.Update)]
        public async Task<IActionResult> Update([FromBody] PushSubscriptionUpdateModel model)
        {
            // TODO unregister old endpoint, register new on TO THE CURRENT SESSION
            // (protects us against people changing the subscription to their device 
            // when they aren't the owner)
            await SendNotification(model.newSubscription,
                new PushPayload("Hello there", "Your push subscription has auto-renewed."));
            return new EmptyResult();
        }

        public class PushPayload
        {
            /// <summary>
            /// The title of the push notification
            /// </summary>
            [JsonPropertyName("title")]
            public string Title { get; set; }
            /// <summary>
            /// The message of the push notification
            /// (should only be a couple of sentences at most)
            /// </summary>
            [JsonPropertyName("message")]
            public string Message { get; set; }
            /// <summary>
            /// A push message with the same tag as an earlier one
            /// will replace that earlier one if it is still open
            /// </summary>
            [JsonPropertyName("tag")]
            public string Tag { get; set; }
            /// <summary>
            /// The timestamp that this notification should appear to be sent from
            /// (remembering that the user might only get their notifications 
            /// the next time they connect to the internet)
            /// </summary>
            [JsonPropertyName("timestamp")]
            public long Timestamp { get; set; }

            /// <summary>
            /// Create a push payload for a given date/time
            /// </summary>
            /// <param name="title">The title of the notification</param>
            /// <param name="message">The message of the notification</param>
            /// <param name="tag">The tag of the notification
            /// (notifications with the same tag overwrite each other)</param>
            /// <param name="eventDateTime">The date/time the event happened</param>
            public PushPayload(string title, string message,
                string tag, DateTimeOffset eventDateTime)
            {
                Title = title;
                Message = message;
                Tag = tag;
                Timestamp = eventDateTime.ToUnixTimeMilliseconds();
            }

            /// <summary>
            /// Create a push payload for the current date/time
            /// </summary>
            /// <param name="title">The title of the notification</param>
            /// <param name="message">The message of the notification</param>
            /// <param name="tag">The tag of the notification
            /// (notifications with the same tag overwrite each other)</param>
            public PushPayload(string title, string message, string tag = null)
            {
                Title = title;
                Message = message;
                Tag = tag;
                Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
        }

        /// <summary>
        /// Send a push notification to the given subscription
        /// </summary>
        /// <param name="subscriptionModel">The model of the push subscription to send to</param>
        /// <param name="payload">The model of the payload to send</param>
        /// <returns>A task that resolves after the notification has been sent</returns>
        private async Task SendNotification(PushSubscriptionModel subscriptionModel, PushPayload payload)
        {
            PushSubscription subscription = new PushSubscription(subscriptionModel.endpoint,
                subscriptionModel.keys?.p256dh, subscriptionModel.keys?.auth);
            VapidDetails vapidDetails = new VapidDetails("mailto:example@example.com",
                vapidSettings.Value.PublicKey, vapidSettings.Value.PrivateKey);
            string payloadString = await SerializeToJsonAsync(payload);
            try
            {
                await webPushClient.SendNotificationAsync(subscription, payloadString, vapidDetails);
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

        /// <summary>
        /// Serialize an object with the DataContract attribute to JSON
        /// </summary>
        /// <typeparam name="T">A class decorated with the DataContract attribute</typeparam>
        /// <param name="obj">The instance to serialize</param>
        /// <returns>A JSON string</returns>
        private static async Task<string> SerializeToJsonAsync<T>(T obj) where T : class
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (StreamReader reader = new StreamReader(memoryStream))
            {
                JsonSerializerOptions options = new JsonSerializerOptions {
                    IgnoreNullValues = true
                };
                await JsonSerializer.SerializeAsync(memoryStream, obj, options);
                memoryStream.Position = 0;
                return await reader.ReadToEndAsync();
            }
        }
    }
}
