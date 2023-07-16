using System.Collections.Generic;

namespace NationStatesSharp.Interfaces
{
    /// <summary>
    /// Handles all request handling, scheduling and rate limiting, 
    /// </summary>
    public interface IRequestDispatcher
    {
        /// <summary>
        /// Adds a <see cref="Request"/> to the internal queue with the specified priority 
        /// </summary>
        /// <param name="request">The <see cref="Request"/> to add to the queue</param>
        /// <param name="priority">The priority with this <see cref="Request"/> should be scheduled. 
        /// Lower priority numbers have priority over higher ones. (0 having highest priority)
        /// Requests with the same priorities are processed according to the FIFO principle.</param>
        void AddToQueue(Request request, uint priority = 1000);
        /// <summary>
        /// Adds a <see cref="Request"/> to the internal queue with the specified priority 
        /// </summary>
        /// <param name="requests">The <see cref="Request"/>s to add to the queue</param>
        /// <param name="priority">The priority with this <see cref="Request"/> should be scheduled. 
        /// Lower priority numbers have priority over higher ones. (0 having highest priority)
        /// Requests with the same priorities are processed according to the FIFO principle.</param>
        void AddToQueue(IEnumerable<Request> requests, uint priority = 1000);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="priority"></param>
        void Dispatch(Request request, uint priority = 1000);
        /// <summary>
        /// Queues a request for dispatching. See also <see cref="AddToQueue(IEnumerable{Request}, uint)"/>
        /// </summary>
        /// <param name="requests">The <see cref="Request"/>s to add to the queue</param>
        /// <param name="priority"></param>
        void Dispatch(IEnumerable<Request> requests, uint priority = 1000);
        /// <summary>
        /// Start the internal request worker to start processing request in the internal queue.
        /// </summary>
        void Start();
        /// <summary>
        /// Stops the internal request worker and cancel all remaining request in the queue.
        /// </summary>
        void Shutdown();
    }
}