using Seventh.Core.Constants;
using Seventh.Core.Events;
using Seventh.Core.Services;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Seventh.Core.Input
{
    public class InputService : MonoBehaviour, IInputService
    {
        public GameState CurrentGameState { get; private set; }

        private GameInput _gameInput;

        private InputAction _moveAction;
        private InputAction _attackAction;
        private InputAction _dashAction;
        private InputAction _menuAction;

        private void Awake()
        {
            ServiceLocator.Register<IInputService>(this);
            ServiceLocator.Get<IEventBus>().Subscribe<GameStateChangedEvent>(OnGameStateChanged);

            _gameInput = new GameInput();
            _moveAction = _gameInput.Player.Move;
            _attackAction = _gameInput.Player.Attack;
            _dashAction = _gameInput.Player.Dash;
            _menuAction = _gameInput.Player.Menu;
            _menuAction.performed += OnMenuActionPerformed;
            _gameInput.Enable();
        }

        public void OnDestroy()
        {
            _gameInput?.Disable();
            if (_menuAction != null)
            {
                _menuAction.performed -= OnMenuActionPerformed;
            }

            ServiceLocator.Get<IEventBus>().Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
            ServiceLocator.Unregister<IInputService>();
        }

        private void OnMenuActionPerformed(InputAction.CallbackContext ctx)
        {
            if (CurrentGameState == GameState.Playing || CurrentGameState == GameState.Paused)
            {
                ServiceLocator.Get<IEventBus>().Publish(new PlayerMenuPressedEvent());
            }
        }

        public void GetAttackInput(out bool isAttacking, out Vector2 attackDirection)
        {
            if (CurrentGameState != GameState.Playing)
            {
                isAttacking = false;
                attackDirection = Vector2.zero;
                return;
            }

            isAttacking = _attackAction.triggered;
            attackDirection = Vector2.zero;
        }

        public void GetDashInput(out bool isDashing)
        {
            if (CurrentGameState != GameState.Playing)
            {
                isDashing = false;
                return;
            }

            isDashing = _dashAction.triggered;
        }

        public void GetMenuInput(out bool isMenuOpen)
        {
            if (CurrentGameState != GameState.Playing && CurrentGameState != GameState.Paused)
            {
                isMenuOpen = false;
                return;
            }

            isMenuOpen = _menuAction.triggered;
        }

        public Vector2 GetMovementInput()
        {
            if (CurrentGameState != GameState.Playing)
            {
                return Vector2.zero;
            }

            return _moveAction.ReadValue<Vector2>();
        }

        public void OnGameStateChanged(GameStateChangedEvent evt)
        {
            CurrentGameState = evt.CurrentState;

            bool isGameplay = CurrentGameState == GameState.Playing;

            if (isGameplay)
            {
                _moveAction.Enable();
                _dashAction.Enable();
                _attackAction.Enable();
            }
            else
            {
                _moveAction.Disable();
                _dashAction.Disable();
                _attackAction.Disable();
            }
        }
    }
}
