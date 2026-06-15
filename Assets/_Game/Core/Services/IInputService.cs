using Seventh.Core.Constants;
using UnityEngine;

namespace Seventh.Core.Services
{
    public interface IInputService
    {
        GameState CurrentGameState { get; }

        // =================== Input Methods ===================
        Vector2 GetMovementInput();
        void GetAttackInput(out bool isAttacking, out Vector2 attackDirection);
        void GetDashInput(out bool isDashing, out Vector2 dashDirection);
        void GetMenuInput(out bool isMenuOpen);

        void OnGameStateChanged(GameState newState);
    }
}
