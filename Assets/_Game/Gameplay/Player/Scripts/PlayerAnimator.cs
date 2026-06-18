using UnityEngine;

namespace Seventh.Gameplay.Player
{
    public class PlayerAnimator : MonoBehaviour
    {
        private Animator _animator;

        private readonly int _speedHash = Animator.StringToHash("Speed");
        private readonly int _moveXHash = Animator.StringToHash("MoveX");
        private readonly int _moveYHash = Animator.StringToHash("MoveY");
        private readonly int _attackHash = Animator.StringToHash("Attack");
        private readonly int _alternateHash = Animator.StringToHash("AlternateAttack");
        private readonly int _comboIndexHash = Animator.StringToHash("ComboIndex");

        private bool _hasAttackParameter;
        private bool _hasAlternateParameter;
        private bool _hasComboIndexParameter;


        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            _hasAttackParameter = HasParameter("Attack");
            _hasAlternateParameter = HasParameter("AlternateAttack");
            _hasComboIndexParameter = HasParameter("ComboIndex");
        }

        private bool HasParameter(string paramName)
        {
            if (_animator == null) return false;
            foreach (AnimatorControllerParameter param in _animator.parameters)
            {
                if (param.name == paramName) return true;
            }
            return false;
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

        public void PlayAttackAnimation()
        {
            if (_hasAttackParameter)
            {
                _animator.SetTrigger(_attackHash);
            }
        }

        public void SetAlternateAttackParameter(bool isAlternate)
        {
            if (_hasAlternateParameter)
            {
                _animator.SetBool(_alternateHash, isAlternate);
            }
        }

        public void SetComboIndex(int comboIndex)
        {
            if (_hasComboIndexParameter)
            {
                _animator.SetInteger(_comboIndexHash, comboIndex);
            }
        }
    }
}
