namespace SorasNerdDen.Services
{
    using Microsoft.AspNetCore.Http;
    using SorasNerdDen.Models;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Collects the EventSource connections to this server, keeps them alive, and writes data to them.
    /// </summary>
    public interface IEventSourceService
    {
        /// <summary>
        /// Adds a connection to the list that we are keeping alive
        /// </summary>
        /// <param name="clientGuid">The GUID of the client connected to us</param>
        /// <param name="connection">The connection to keep alive</param>
        void KeepConnectionAlive(Guid clientGuid, HttpResponse connection);
        /// <summary>
        /// Removes a connection from the list that we are keeping alive
        /// </summary>
        /// <param name="clientGuid">The GUID of the client no longer connected to us</param>
        /// <param name="connection">The connection to allow to die</param>
        void LetConnectionDie(Guid clientGuid, HttpResponse connection);
        /// <summary>
        /// Asynchronously write data to a specific client's EventSource(s)
        /// </summary>
        /// <param name="clientGuid">The GUID of the client we are attempting to contact</param>
        /// <param name="payload">The payload to send</param>
        /// <returns>A task that resolves once the data write is complete</returns>
        Task WriteEventSourceEventAsync(Guid clientGuid, PushPayload payload);
        /// <summary>
        /// Begin writing 'heartbeat' messages to our connections to keep them alive.
        /// </summary>
        /// <param name="token">A cancellation token that will close all connections when triggered.</param>
        /// <returns>A task that will continue until cancelled</returns>
        Task StartHeartbeat(CancellationToken token);
    }
}
