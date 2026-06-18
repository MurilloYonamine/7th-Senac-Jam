using UnityEngine;
using DG.Tweening;

namespace Seventh.Gameplay.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyHopMovement : MonoBehaviour
    {
        [Header("Hop Physics")]
        [SerializeField] private float _hopSpeed = 4f;
        [SerializeField] private float _hopDuration = 0.3f;
        [SerializeField] private float _hopCooldown = 0.2f; 
        
        [Header("Visuals")]
        [SerializeField] private Transform _visualModel;
        [SerializeField] private float _hopHeight = 0.5f;

        private Rigidbody2D _rb;
        private bool _isHopping;
        private float _cooldownTimer;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        public void Move(Vector2 direction)
        {
            if (_isHopping) return;

            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            if (direction.sqrMagnitude > 0.01f)
            {
                PerformHop(direction.normalized);
            }
            else
            {
                Stop();
            }
        }

        public void Stop()
        {
            if (!_isHopping)
            {
                _rb.linearVelocity = Vector2.zero;
            }
        }

        private void PerformHop(Vector2 direction)
        {
            _isHopping = true;

            _rb.linearVelocity = direction * _hopSpeed;

            if (_visualModel != null)
            {
                _visualModel.DOKill();
                
                _visualModel.DOScale(new Vector3(1.2f, 0.8f, 1f), 0.05f).OnComplete(() =>
                {
                    _visualModel.DOScale(Vector3.one, 0.1f);
                    
                    _visualModel.DOLocalMoveY(_hopHeight, _hopDuration / 2f)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            _visualModel.DOLocalMoveY(0f, _hopDuration / 2f)
                                .SetEase(Ease.InQuad)
                                .OnComplete(() =>
                                {
                                    _visualModel.DOScale(new Vector3(1.2f, 0.8f, 1f), 0.05f).OnComplete(() =>
                                    {
                                        _visualModel.DOScale(Vector3.one, 0.1f);
                                        EndHop();
                                    });
                                });
                        });
                });
            }
            else
            {
                DOVirtual.DelayedCall(_hopDuration, EndHop);
            }
        }

        private void EndHop()
        {
            _isHopping = false;
            _rb.linearVelocity = Vector2.zero;
            _cooldownTimer = _hopCooldown;
        }

        public void Interrupt()
        {
            _isHopping = false;
            _rb.linearVelocity = Vector2.zero;
            _cooldownTimer = 0f;
            if (_visualModel != null)
            {
                _visualModel.DOKill();
                _visualModel.localScale = Vector3.one;
                _visualModel.localPosition = Vector3.zero;
            }
        }
    }
}
