using System;
using UnityEngine;

namespace Seventh.Core.Services
{
    public interface IEventBus 
    {
        void Subscribe<TEvent>(Action<TEvent> handler);
        void Unsubscribe<TEvent>(Action<TEvent> handler);
        void Publish<TEvent>(TEvent eventData);
    }
}
