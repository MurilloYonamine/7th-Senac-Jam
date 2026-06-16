using UnityEngine;

namespace Seventh.Gameplay.Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        private Animator _animator;

        private readonly int _speedHash = Animator.StringToHash("Speed");
        private readonly int _moveXHash = Animator.StringToHash("MoveX");
        private readonly int _moveYHash = Animator.StringToHash("MoveY");


        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public void UpdateMovementAnimation(Vector2 movementInput)
        {
            float speed = movementInput.magnitude;
            _animator.SetFloat(_speedHash, speed);

            if (speed > 0.01f)
            {
                _animator.SetFloat(_moveXHash, movementInput.x);
                _animator.SetFloat(_moveYHash, movementInput.y);
            }
        }
    }
}
