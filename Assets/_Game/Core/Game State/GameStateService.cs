using Seventh.Core.Constants;
using Seventh.Core.Events;
using Seventh.Core.Services;
using UnityEngine;

namespace Seventh.Core
{
    public class GameStateService : MonoBehaviour, IGameStateService
    {
        private const string TAG = "<color=yellow><b>[GameStateService]</b></color>";
        public GameState CurrentGameState { get; private set; }

        private void Awake()
        {
            ServiceLocator.Register<IGameStateService>(this);
            ServiceLocator.Get<IEventBus>().Publish(new GameStateChangedEvent(GameState.None, GameState.Playing));
            
            ChangeGameState(GameState.Playing);
        }

        private void OnDestroy()
        {
            ServiceLocator.Get<IEventBus>().Publish(new GameStateChangedEvent(CurrentGameState, GameState.None));
            ServiceLocator.Unregister<IGameStateService>();
        }

        public void ChangeGameState(GameState newState)
        {
            CurrentGameState = newState;
            Debug.Log($"{TAG} Game state changed to {newState}");
        }
    }
}
