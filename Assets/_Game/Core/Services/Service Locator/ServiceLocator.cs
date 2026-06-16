using System;
using System.Collections.Generic;
using UnityEngine;

namespace Seventh.Core.Services
{
    public static class ServiceLocator
    {
        private const string TAG = "<color=yellow><b>[ServiceLocator]</b></color>";

        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public static void Register<T>(T service)
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"{TAG} O serviço do tipo {type.Name} já está registrado.");
                return;
            }

            _services.Add(type, service);
            Debug.Log($"{TAG} Serviço do tipo {type.Name} registrado.");
        }

        public static void Unregister<T>()
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                _services.Remove(type);
            }
        }

        public static T Get<T>()
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }

            Debug.LogError($" {TAG} Serviço do tipo {type} não foi encontrado.");
            return default;
        }

        public static bool TryGet<T>(out T service)
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var found))
            {
                service = (T)found;
                return true;
            }

            service = default;
            return false;
        }
        public static void Clear()
        {
            _services.Clear();
        }
    }
}
