using Seventh.Core.Constants;
using Seventh.Core.Events;
using UnityEngine;

namespace Seventh.Core.Services
{
    public interface IInputService
    {
        GameState CurrentGameState { get; }

        Vector2 GetMovementInput();
        void GetAttackInput(out bool isAttacking, out Vector2 attackDirection);
        void GetDashInput(out bool isDashing);
        void GetMenuInput(out bool isMenuOpen);

        void OnGameStateChanged(GameStateChangedEvent evt);
    }
}
