using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Seventh.Core.Services;
using Seventh.Core.Audio;
using Seventh.Gameplay.Player;

namespace Seventh.Gameplay.Items
{
    [RequireComponent(typeof(Collider2D))]
    public class Key : MonoBehaviour
    {
        // Static registry to easily locate keys carried by a player
        private static readonly List<Key> ActiveKeys = new List<Key>();
        public static event System.Action<Transform> OnKeyStatusChanged;

        [Header("Follow Settings")]
        [SerializeField] private float _followSmoothTime = 0.25f;
        [SerializeField] private Vector3 _followOffset = new Vector3(0f, 1.2f, 0f);
        [SerializeField] private float _followDistance = 1.2f;
        [SerializeField] private float _keySpacing = 0.5f;
        [SerializeField] private float _offsetSmoothSpeed = 5f;
        [SerializeField] private float _rotationSmoothSpeed = 10f;
        [SerializeField] private float _rotationOffsetAngle = 0f;

        [Header("Bobbing Effect")]
        [SerializeField] private float _bobAmplitude = 0.15f;
        [SerializeField] private float _bobFrequency = 3f;

        [Header("Audio")]
        [SerializeField] private AudioClip _pickupSFX;
        [SerializeField, Range(0f, 1f)] private float _pickupVolume = 1f;

        [Header("Unlock Animation")]
        [SerializeField] private float _consumeDuration = 0.6f;

        private Collider2D _collider;
        private Transform _target;
        private bool _isCarried;
        private bool _isConsumed;
        
        private Vector3 _startPosition;
        private float _timeOffset;
        private Vector3 _currentVelocity;
        private Vector3 _currentFollowOffset;

        public bool IsCarried => _isCarried;
        public Transform Target => _target;

        private IAudioService _audioService;


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
            _audioService = ServiceLocator.Get<IAudioService>();

            _collider = GetComponent<Collider2D>();
            _collider.isTrigger = true;
            _startPosition = transform.position;
            _timeOffset = Random.Range(0f, 100f); 
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

            if (other.TryGetComponent<PlayerController>(out var player))
            {
                Collect(player.transform);
            }
        }

        public void Collect(Transform playerTransform)
        {
            _isCarried = true;
            _target = playerTransform;
            _currentFollowOffset = _followOffset;
            
            if (_collider != null)
            {
                _collider.enabled = false;
            }

            if (_audioService != null && _pickupSFX != null)
            {
                _audioService.PlaySFX(_pickupSFX, new Core.Services.AudioSettings { VolumeOffset = _pickupVolume });
            }

            OnKeyStatusChanged?.Invoke(playerTransform);
        }

        private void LateUpdate()
        {
            if (_isConsumed) return;

            if (_isCarried)
            {
                List<Key> myPlayerKeys = new List<Key>();
                for (int i = 0; i < ActiveKeys.Count; i++)
                {
                    var k = ActiveKeys[i];
                    if (k != null && k.IsCarried && k.Target == _target && !k._isConsumed)
                    {
                        myPlayerKeys.Add(k);
                    }
                }
                int myIndex = myPlayerKeys.IndexOf(this);

                Vector3 desiredOffset = _followOffset;
                Transform followTarget = _target;

                if (_target != null)
                {
                    var playerMovement = _target.GetComponent<PlayerMovement>();
                    Vector3 facingDir = playerMovement != null ? (Vector3)playerMovement.FacingDirection : Vector3.right;

                    if (myIndex == 0)
                    {
                        desiredOffset = -facingDir * _followDistance;
                        followTarget = _target;
                    }
                    else if (myIndex > 0)
                    {
                        desiredOffset = -facingDir * _keySpacing;
                        followTarget = myPlayerKeys[myIndex - 1].transform;
                    }
                }

                _currentFollowOffset = Vector3.Lerp(_currentFollowOffset, desiredOffset, Time.deltaTime * _offsetSmoothSpeed);

                Vector3 targetPos = followTarget.position + _currentFollowOffset;
                targetPos.y += Mathf.Sin(Time.time * _bobFrequency) * _bobAmplitude;
                
                transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _currentVelocity, _followSmoothTime);

                if (_currentVelocity.sqrMagnitude > 0.05f)
                {
                    float targetAngle = Mathf.Atan2(_currentVelocity.y, _currentVelocity.x) * Mathf.Rad2Deg + _rotationOffsetAngle;
                    float currentAngle = transform.eulerAngles.z;
                    float smoothedAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * _rotationSmoothSpeed);
                    transform.rotation = Quaternion.Euler(0f, 0f, smoothedAngle);
                }
                else if (_target != null)
                {
                    var playerMovement = _target.GetComponent<PlayerMovement>();
                    if (playerMovement != null)
                    {
                        float targetAngle = Mathf.Atan2(playerMovement.FacingDirection.y, playerMovement.FacingDirection.x) * Mathf.Rad2Deg + _rotationOffsetAngle;
                        float currentAngle = transform.eulerAngles.z;
                        float smoothedAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * (_rotationSmoothSpeed * 0.5f));
                        transform.rotation = Quaternion.Euler(0f, 0f, smoothedAngle);
                    }
                }
            }
            else
            {
                transform.SetPositionAndRotation(_startPosition + Vector3.up * Mathf.Sin((Time.time + _timeOffset) * _bobFrequency) * _bobAmplitude, Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.deltaTime * 5f));
            }
        }

        public void Consume(Vector3 destination, System.Action onComplete)
        {
            if (_isConsumed) return;
            _isConsumed = true;

            Transform player = _target;

            Sequence consumeSequence = DOTween.Sequence();
            
            consumeSequence.Join(transform.DOMove(destination, _consumeDuration).SetEase(Ease.OutQuad));
            consumeSequence.Join(transform.DOScale(Vector3.zero, _consumeDuration).SetEase(Ease.InBack));
            consumeSequence.Join(transform.DORotate(new Vector3(0, 0, 360f), _consumeDuration, RotateMode.FastBeyond360));
            
            consumeSequence.OnComplete(() =>
            {
                onComplete?.Invoke();
                OnKeyStatusChanged?.Invoke(player);
                Destroy(gameObject);
            });
        }
    }
}
