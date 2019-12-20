namespace SorasNerdDen.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using SorasNerdDen.Constants;
    using SorasNerdDen.Models;
    using SorasNerdDen.Services;
    using SorasNerdDen.Services.CancellationTokens;
    using SorasNerdDen.Settings;
    using System;
    using System.Threading.Tasks;
    using WebPush;

    public class PushController : Controller
    {
        // Use server-sent events when Pushing is not supported or disabled
        private readonly IEventSourceService eventSourceService;
        private readonly IOptionsSnapshot<VapidSettings> vapidSettings;

        private static readonly WebPushClient webPushClient = new WebPushClient();

        public PushController(IEventSourceService eventSourceService,
            IOptionsSnapshot<VapidSettings> vapidSettings)
        {
            this.eventSourceService = eventSourceService;
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

        [HttpPost("push/eventsource", Name = PushControllerRoute.EventSource)]
        public async Task<IActionResult> EventSource()
        {
            if (Request.Headers["Accept"] == "text/event-stream")
            {
                Response.ContentType = "text/event-stream";
                await Response.Body.FlushAsync();

                Guid clientGuid = Guid.Empty;//TODO

                eventSourceService.KeepConnectionAlive(clientGuid, Response);

                await HttpContext.RequestAborted.WaitAsync();

                // Remove the now-closed response
                eventSourceService.LetConnectionDie(clientGuid, Response);
            }

            return new EmptyResult();
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
            string payloadString = await SerializationHelper.SerializeToJsonAsync(payload);
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
    }
}
