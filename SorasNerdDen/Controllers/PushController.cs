namespace SorasNerdDen.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using SorasNerdDen.Constants;
    using SorasNerdDen.Models;
    using SorasNerdDen.Services.CancellationTokens;
    using SorasNerdDen.Settings;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using WebPush;

    public class PushController : Controller
    {
        private readonly IOptionsSnapshot<VapidSettings> vapidSettings;

        private static readonly WebPushClient webPushClient = new WebPushClient();
        // Use server-sent events when Pushing is not supported or disabled
        private static readonly ConcurrentDictionary<Guid, ConcurrentCollection<HttpResponse>>
            ServerSentEventResponses =
            new ConcurrentDictionary<Guid, ConcurrentCollection<HttpResponse>>();

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

        [HttpPost("push/eventsource", Name = PushControllerRoute.EventSource)]
        public async Task<IActionResult> EventSource()
        {
            if (Request.Headers["Accept"] == "text/event-stream")
            {
                Response.ContentType = "text/event-stream";
                await Response.Body.FlushAsync();

                Guid clientGuid = Guid.NewGuid();//TODO

                ConcurrentCollection<HttpResponse> listOfClientConections =
                    new ConcurrentCollection<HttpResponse>
                    {
                        Response
                    };
                ServerSentEventResponses.AddOrUpdate(clientGuid, listOfClientConections,
                    (key, oldValue) => {
                    oldValue.Add(Response);
                    return oldValue;
                });

                await HttpContext.RequestAborted.WaitAsync();

                // Remove the now-closed response
                if (ServerSentEventResponses.TryGetValue(clientGuid,
                    out ConcurrentCollection<HttpResponse> clientResponses))
                {
                    try
                    {
                        clientResponses._lock.EnterWriteLock();
                        clientResponses.Remove(Response);
                        if (clientResponses.Count == 0)
                        {
                            //TODO unit test the interleaving
                            ServerSentEventResponses.TryRemove(clientGuid, out clientResponses);
                        }
                    }
                    finally
                    {
                        clientResponses._lock.ExitWriteLock();
                    }
                }
            }

            return new EmptyResult();
        }

        private static async Task WriteEventSourceEventAsync(Guid clientGuid, PushPayload payload)
        {
            string payloadString = await SerializeToJsonAsync(payload);

            if (ServerSentEventResponses.TryGetValue(clientGuid,
                out ConcurrentCollection<HttpResponse> responses))
            {
                foreach (HttpResponse response in responses)
                {
                    await WriteEventSourceDataAsync("data", payloadString, response);
                }
            }
        }

        private static async Task WriteEventSourceDataAsync(string dataType, string data,
            HttpResponse response)
        {
            await response.WriteAsync($"{dataType}: {data}\n\n");
            await response.Body.FlushAsync();
        }

        //TODO call when?
        private static async Task WriteEventSourceHeartbeatAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                foreach(KeyValuePair<Guid, ConcurrentCollection<HttpResponse>> pair
                    in ServerSentEventResponses)
                {
                    // It doesn't really matter what we write, as long as we write something
                    // to keep the connection alive
                    foreach (HttpResponse response in pair.Value)
                    {
                        await WriteEventSourceDataAsync(null, null, response);
                    }
                }

                await Task.Delay(1000 * 15);// Repeat every 15 seconds
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
