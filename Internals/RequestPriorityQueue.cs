using NationStatesSharp.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NationStatesSharp
{
    internal class RequestPriorityQueue : PriorityQueue<Request, uint>
    {
        /// <summary>
        /// The queue detected, during the last Enqueue operation, that no one is waiting for new items. It is therefore reasonable to assume that the consumer died.
        /// Use this event to detected consumer failures and for restarting these.
        /// </summary>
        public event EventHandler Jammed;

        public new void Enqueue(Request item, uint priority)
        {
            if (item.Status == RequestStatus.Pending)
            {
                if (!isWaiting && Count > 2)
                {
                    Jammed?.Invoke(this, new EventArgs());
                }
                base.Enqueue(item, priority);
                _waitCompletionSource?.TrySetResult();
                isWaiting = false;
            }
        }

        private TaskCompletionSource _waitCompletionSource;
        private bool isWaiting = false;

        public Task WaitForNextItemAsync(CancellationToken cancellationToken)
        {
            if (Count > 0)
            {
                return Task.FromResult(true);
            }
            else
            {
                if (!isWaiting)
                {
                    _waitCompletionSource = new TaskCompletionSource();
                    isWaiting = true;
                }
                cancellationToken.Register(() => _waitCompletionSource.TrySetCanceled());
                return _waitCompletionSource.Task;
            }
        }
    }
}