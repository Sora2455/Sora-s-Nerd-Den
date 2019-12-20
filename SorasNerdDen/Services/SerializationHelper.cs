namespace SorasNerdDen.Services
{
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class SerializationHelper
    {
        /// <summary>
        /// Serialize an object with the DataContract attribute to JSON
        /// </summary>
        /// <typeparam name="T">A class decorated with the DataContract attribute</typeparam>
        /// <param name="obj">The instance to serialize</param>
        /// <returns>A JSON string</returns>
        public static async Task<string> SerializeToJsonAsync<T>(T obj) where T : class
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (StreamReader reader = new StreamReader(memoryStream))
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    IgnoreNullValues = true
                };
                await JsonSerializer.SerializeAsync(memoryStream, obj, options);
                memoryStream.Position = 0;
                return await reader.ReadToEndAsync();
            }
        }
    }
}
