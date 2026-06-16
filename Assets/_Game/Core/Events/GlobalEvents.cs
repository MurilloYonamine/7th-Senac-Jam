using UnityEngine;
using Seventh.Core.Constants;

namespace Seventh.Core.Events
{
    public readonly struct GameStateChangedEvent {
        public readonly GameState PreviousState;
        public readonly GameState CurrentState;

        public GameStateChangedEvent(GameState previous, GameState current) {
            PreviousState = previous;
            CurrentState = current;
        }
    }

    public readonly struct EnemyDefeatedEvent {
        public readonly GameObject EnemyGameObject;

        public EnemyDefeatedEvent(GameObject enemy) {
            EnemyGameObject = enemy;
        }
    }
}
