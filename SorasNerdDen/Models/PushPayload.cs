namespace SorasNerdDen.Models
{
    using System;
    using System.Text.Json.Serialization;
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
}
