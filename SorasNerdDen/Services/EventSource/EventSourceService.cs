namespace SorasNerdDen.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Concurrency;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Hosting;
    using SorasNerdDen.Models;

    /// <summary>
    /// Collects the EventSource connections to this server, keeps them alive, and writes data to them.
    /// </summary>
    public class EventSourceService : BackgroundService, IEventSourceService
    {
        private static readonly ConcurrentDictionaryOfCollections<Guid, HttpResponse>
            ServerSentEventResponses =
            new ConcurrentDictionaryOfCollections<Guid, HttpResponse>();

        /// <summary>
        /// Adds a connection to the list that we are keeping alive
        /// </summary>
        /// <param name="clientGuid">The GUID of the client connected to us</param>
        /// <param name="connection">The connection to keep alive</param>
        public async Task KeepConnectionAlive(Guid clientGuid, HttpResponse connection)
        {
            // Tell the client-side to retry after 10 seconds if the connection drops
            await WriteEventSourceDataAsync("retry", "10000", connection);
            ServerSentEventResponses.Add(clientGuid, connection);
        }

        /// <summary>
        /// Removes a connection from the list that we are keeping alive
        /// </summary>
        /// <param name="clientGuid">The GUID of the client no longer connected to us</param>
        /// <param name="connection">The connection to allow to die</param>
        public void LetConnectionDie(Guid clientGuid, HttpResponse connection)
        {
            ServerSentEventResponses.Remove(clientGuid, connection);
        }

        /// <summary>
        /// Asynchronously write data to a specific client's EventSource(s)
        /// </summary>
        /// <param name="clientGuid">The GUID of the client we are attempting to contact</param>
        /// <param name="payload">The payload to send</param>
        /// <returns>A task that resolves once the data write is complete</returns>
        public async Task WriteEventSourceEventAsync(Guid clientGuid, PushPayload payload)
        {
            IEnumerable<HttpResponse> clientResponses = ServerSentEventResponses.Get(clientGuid);
            string payloadString = await SerializationHelper.SerializeToJsonAsync(payload);

            foreach (HttpResponse response in clientResponses)
            {
                await WriteEventSourceDataAsync("data", payloadString, response);
            }
        }

        /*public override async Task StartAsync(CancellationToken cancellationToken)
        {

        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {

        }*/

        /// <summary>
        /// Begin writing 'heartbeat' messages to our connections to keep them alive.
        /// </summary>
        /// <param name="token">A cancellation token that will close all connections when triggered.</param>
        /// <returns>A task that will continue until cancelled</returns>
        protected override async Task ExecuteAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // It doesn't really matter what we write, as long as we write something
                // to keep the connection alive
                foreach (HttpResponse response in ServerSentEventResponses.GetAll())
                {
                    await WriteEventSourceDataAsync(null, null, response);
                }

                await Task.Delay(1000 * 15, token);// Repeat every 15 seconds
            }
        }

        /// <summary>
        /// Write a message to the EventSource connection
        /// </summary>
        /// <param name="dataType">The type of message to write (data, event, etc)</param>
        /// <param name="data">The JSON data to write to the response</param>
        /// <param name="connection">The connection to write to</param>
        /// <returns>A task that returns once the write is complete</returns>
        private static async Task WriteEventSourceDataAsync(string dataType, string data,
            HttpResponse connection)
        {
            await connection.WriteAsync($"{dataType}: {data}\n\n");
            await connection.Body.FlushAsync();
        }
    }
}
