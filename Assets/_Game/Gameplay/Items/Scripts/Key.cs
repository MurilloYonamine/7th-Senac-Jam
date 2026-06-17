using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Seventh.Core.Services;
using Seventh.Gameplay.Player;

namespace Seventh.Gameplay.Items
{
    [RequireComponent(typeof(Collider2D))]
    public class Key : MonoBehaviour
    {
        // Static registry to easily locate keys carried by a player
        private static readonly List<Key> ActiveKeys = new List<Key>();

        [Header("Follow Settings")]
        [SerializeField] private float _followSmoothTime = 0.25f;
        [SerializeField] private Vector3 _followOffset = new Vector3(0f, 1.2f, 0f);

        [Header("Bobbing Effect")]
        [SerializeField] private float _bobAmplitude = 0.15f;
        [SerializeField] private float _bobFrequency = 3f;

        [Header("Audio")]
        [SerializeField] private AudioClip _pickupSFX;

        [Header("Unlock Animation")]
        [SerializeField] private float _consumeDuration = 0.6f;

        private Collider2D _collider;
        private Transform _target;
        private bool _isCarried;
        private bool _isConsumed;
        
        private Vector3 _startPosition;
        private float _timeOffset;
        private Vector3 _currentVelocity;

        public bool IsCarried => _isCarried;
        public Transform Target => _target;

        /// <summary>
        /// Finds the first key currently carried by the specified player transform.
        /// </summary>
        public static Key GetCarriedKey(Transform player)
        {
            for (int i = 0; i < ActiveKeys.Count; i++)
            {
                var key = ActiveKeys[i];
                if (key != null && key.IsCarried && key.Target == player && !key._isConsumed)
                {
                    return key;
                }
            }
            return null;
        }

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _collider.isTrigger = true;
            _startPosition = transform.position;
            _timeOffset = Random.Range(0f, 100f); // Randomize phase shift so multiple keys on ground don't bob in sync
        }

        private void OnEnable()
        {
            ActiveKeys.Add(this);
        }

        private void OnDisable()
        {
            ActiveKeys.Remove(this);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isCarried || _isConsumed) return;

            // Detect if the collider belongs to the player
            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                Collect(player.transform);
            }
        }

        public void Collect(Transform playerTransform)
        {
            _isCarried = true;
            _target = playerTransform;
            
            if (_collider != null)
            {
                _collider.enabled = false;
            }

            // Play pickup sound
            var audioService = ServiceLocator.Get<IAudioService>();
            if (audioService != null && _pickupSFX != null)
            {
                audioService.PlaySFX(_pickupSFX);
            }
        }

        private void LateUpdate()
        {
            if (_isConsumed) return;

            if (_isCarried)
            {
                // Smoothly follow the player with offset and bobbing
                Vector3 targetPos = _target.position + _followOffset;
                targetPos.y += Mathf.Sin(Time.time * _bobFrequency) * _bobAmplitude;
                
                transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _currentVelocity, _followSmoothTime);
            }
            else
            {
                // Idle bobbing on the ground
                transform.position = _startPosition + Vector3.up * Mathf.Sin((Time.time + _timeOffset) * _bobFrequency) * _bobAmplitude;
            }
        }

        /// <summary>
        /// Animates the key flying to the lock/door, then invokes the callback and destroys the key.
        /// </summary>
        public void Consume(Vector3 destination, System.Action onComplete)
        {
            if (_isConsumed) return;
            _isConsumed = true;

            // Disable following logic and animate using DOTween
            Sequence consumeSequence = DOTween.Sequence();
            
            consumeSequence.Join(transform.DOMove(destination, _consumeDuration).SetEase(Ease.OutQuad));
            consumeSequence.Join(transform.DOScale(Vector3.zero, _consumeDuration).SetEase(Ease.InBack));
            consumeSequence.Join(transform.DORotate(new Vector3(0, 0, 360f), _consumeDuration, RotateMode.FastBeyond360));
            
            consumeSequence.OnComplete(() =>
            {
                onComplete?.Invoke();
                Destroy(gameObject);
            });
        }
    }
}
