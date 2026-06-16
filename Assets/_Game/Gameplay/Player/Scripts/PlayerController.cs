using UnityEngine;

namespace Seventh.Gameplay.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerAnimator))]
    [RequireComponent(typeof(PlayerDash))]
    [RequireComponent(typeof(PlayerAttack))]
    public class PlayerController : MonoBehaviour
    {
        private PlayerMovement _movement;
        private PlayerAnimator _animator;
        private PlayerDash _dash;
        private PlayerAttack _attack;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _animator = GetComponent<PlayerAnimator>();
            _dash = GetComponent<PlayerDash>();
            _attack = GetComponent<PlayerAttack>();
        }
    }
}
