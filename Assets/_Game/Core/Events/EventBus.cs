using System;
using System.Collections.Generic;
using Seventh.Core.Services;

namespace Seventh.Core.Events
{
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
            var eventType = typeof(TEvent);
            if (!_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType] = new List<Delegate>();
            }
            _subscribers[eventType].Add(handler);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            var eventType = typeof(TEvent);
            if (_subscribers.ContainsKey(eventType))
            {
                _subscribers[eventType].Remove(handler);
                if (_subscribers[eventType].Count == 0)
                {
                    _subscribers.Remove(eventType);
                }
            }
        }

        public void Publish<TEvent>(TEvent eventData)
        {
            var eventType = typeof(TEvent);
            if (_subscribers.ContainsKey(eventType))
            {
                var subscribersCopy = new List<Delegate>(_subscribers[eventType]);
                foreach (var subscriber in subscribersCopy)
                {
                    if (subscriber is Action<TEvent> action)
                    {
                        action.Invoke(eventData);
                    }
                }
            }
        }
    }
}
