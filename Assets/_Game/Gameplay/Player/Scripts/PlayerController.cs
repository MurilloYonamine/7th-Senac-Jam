using UnityEngine;
using Seventh.Core;
using Seventh.Core.Constants;
using Seventh.Core.Events;
using Seventh.Core.Services;

namespace Seventh.Gameplay.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerAnimator))]
    [RequireComponent(typeof(PlayerDash))]
    [RequireComponent(typeof(PlayerAttack))]
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerController : MonoBehaviour
    {
        private PlayerMovement _movement;
        private PlayerAnimator _animator;
        private PlayerDash _dash;
        private PlayerAttack _attack;
        private PlayerHealth _health;

        [SerializeField] private GameObject _pauseMenu;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _animator = GetComponent<PlayerAnimator>();
            _dash = GetComponent<PlayerDash>();
            _attack = GetComponent<PlayerAttack>();
            _health = GetComponent<PlayerHealth>();
        }

        private void Start()
        {
            var eventBus = ServiceLocator.Get<IEventBus>();
            eventBus?.Subscribe<PlayerMenuPressedEvent>(HandleMenuPressed);
        }

        private void OnDestroy()
        {
            var eventBus = ServiceLocator.Get<IEventBus>();
            eventBus?.Unsubscribe<PlayerMenuPressedEvent>(HandleMenuPressed);
        }

        private void HandleMenuPressed(PlayerMenuPressedEvent evt)
        {
            if (_pauseMenu == null) return;

            var gameStateService = ServiceLocator.Get<IGameStateService>();
            if (gameStateService != null && gameStateService.CurrentGameState == GameState.Playing)
            {
                _pauseMenu.SetActive(true);
            }
        }
    }
}
