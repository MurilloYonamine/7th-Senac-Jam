using UnityEngine;
using Seventh.Core.Events;
using Seventh.Core.Services;

namespace Seventh.Gameplay.Bootstrap
{
    public static class GameBootstrapper
    {
        private const string TAG = "<color=yellow><b>[GameBootstrapper]</b></color>";
        private const string CORE_SYSTEMS_NAME = "[ CORE SYSTEMS ]";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            var eventBus = new EventBus();
            ServiceLocator.Register<IEventBus>(eventBus);

            GameObject coreSystems = Resources.Load<GameObject>(CORE_SYSTEMS_NAME);
            if (coreSystems != null)
            {
                GameObject instantiateObject = Object.Instantiate(coreSystems);
                instantiateObject.name = CORE_SYSTEMS_NAME;
                Object.DontDestroyOnLoad(instantiateObject);
                Debug.Log($"{TAG} Core systems initialized successfully.");
            }
            else
            {
                Debug.LogError($"{TAG} Failed to load core systems prefab at path: {CORE_SYSTEMS_NAME}");
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Cleanup()
        {
            ServiceLocator.Clear();
            Debug.Log($"{TAG} Services cleared on application quit.");
        }
    }
}
