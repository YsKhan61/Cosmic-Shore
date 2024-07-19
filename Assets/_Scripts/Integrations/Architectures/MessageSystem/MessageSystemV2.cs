using System;
using System.Collections.Generic;
using System.Linq;

namespace CosmicShore.Integrations.Architectures.MessageSystem
{
    public class MessageSystemV2<T> : IMessageSystem<T>
    {
        /// <summary>
        /// A list of message handlers ready to be invoked and subscribed.
        /// </summary>
        private readonly List<Action<T>> _handlers = new();

        /// <summary>
        /// A dictionary of pending handlers.
        /// If the handler's pair value is true, it means the handler should be added.
        /// If the handler's pair value is false, it means the handler should be removed
        /// </summary>
        private readonly Dictionary<Action<T>, bool> _pendingHandlers = new();
        
        /// <summary>
        /// A flag signifying if the message system is disposed.
        /// If true, don't try to access the message system, invoke or subscribe to any event handlers.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// IDisposable implementation, clear event handlers and pending handlers upon message system garbage collection.
        /// </summary>
        public virtual void Dispose()
        {
            if (IsDisposed) return;

            IsDisposed = true;
            _handlers.Clear();
            _pendingHandlers.Clear();
        }

        /// <summary>
        /// Publish corresponding message type
        /// </summary>
        /// <param name="message">Message type to publish</param>
        public virtual void Publish(T message)
        {
            // Add handlers from pending handlers
            // Linq has deferred execution, the query is not executed until it actually enumerate over the results.
            // But in this case both AddRange and RemoveAll cause immediate execution.
            _handlers.AddRange(_pendingHandlers
                .Where(pair => pair.Value)
                .Select(pair => pair.Key));
            
            // Remove all handlers where pending value is false
            // which means these handlers are marked as to be removed.
            _handlers.RemoveAll(handler => 
                _pendingHandlers.ContainsKey(handler) && !_pendingHandlers[handler]);
            
            // Clear out pending handlers because they are already been added to handlers.
            _pendingHandlers.Clear();

            // Invoke all the event handlers with the corresponding message type
            _handlers.Where(handler => handler != null)
                .ToList()
                .ForEach(handler => handler.Invoke(message));

        }
        
        /// <summary>
        /// Subscribe to event handlers
        /// </summary>
        /// <param name="handler">Event that handles corresponding type</param>
        /// <returns>A disposable subscription</returns>
        /// <exception cref="Exception">An exception to notify </exception>
        public virtual IDisposable Subscribe(Action<T> handler)
        {
            // If the handler has been subscribed, no need to do it again
            if (IsSubscribed(handler)) throw new AggregateException("Attempting to subscribe the same handler more than once.");

            // Mark the handler as pending to be added
            if (!_pendingHandlers.TryAdd(handler, true))
            {
                // If the handler is marked as to be removed, remove it instead of adding
                if (!_pendingHandlers[handler])
                    _pendingHandlers.Remove(handler);
            }

            // Return a disposable subscription using this message system and corresponding handler
            var subscription = new DisposableSubscription<T>(this, handler);
            return subscription;
        }

        /// <summary>
        /// Unsubscribe an event handler
        /// </summary>
        /// <param name="handler">An event handler of a corresponding type</param>
        public void Unsubscribe(Action<T> handler)
        {
            // If the handler is not subscribed, no need to bother
            if (!IsSubscribed(handler)) return;

            // Try mark the handler "to be removed", and proceed to remove it
            if (_pendingHandlers.TryAdd(handler, false))
            {
                if (!_pendingHandlers[handler])
                    _pendingHandlers.Remove(handler);
            }
        }
        
        /// <summary>
        /// A predicate deciding if the event handler is subscribed.
        /// </summary>
        /// <param name="handler">An event handler foa corresponding type</param>
        /// <returns></returns>
        private bool IsSubscribed(Action<T> handler)
        {
            var isPendingRemoval = _pendingHandlers.ContainsKey(handler) && !_pendingHandlers[handler];
            var isPendingAdding = _pendingHandlers.ContainsKey(handler) && _pendingHandlers[handler];
            return _handlers.Contains(handler) && !isPendingRemoval || isPendingAdding;
        }

    }
}
