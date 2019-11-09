namespace SorasNerdDen.Settings
{
    /// <summary>
    /// The settings for Voluntary Application Server Identification - VAPID.
    /// </summary>
    public class VapidSettings
    {
        /// <summary>
        /// The public key used to verify push notifications
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// The private key used to encrypt push notifications
        /// </summary>
        public string PrivateKey { get; set; }
    }
}
