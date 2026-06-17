using UnityEngine;
using DG.Tweening;
using Seventh.Core.Services;
using Seventh.Gameplay.Player;

namespace Seventh.Gameplay.Items
{
    [RequireComponent(typeof(Collider2D))]
    public class KeyLock : MonoBehaviour
    {
        [Header("Lock Settings")]
        [SerializeField] private Transform _keyTargetPoint; // The spot where the key flies to (e.g. keyhole)
        [SerializeField] private GameObject _doorObject;     // The visual/physical door to open
        
        [Header("Unlock Animation")]
        [SerializeField] private float _doorOpenDuration = 0.8f;
        [SerializeField] private float _slideUpDistance = 2.5f;
        [SerializeField] private bool _fadeSprite = true;

        [Header("Audio")]
        [SerializeField] private AudioClip _unlockSFX;

        private Collider2D _collider;
        private bool _isLocked = true;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _collider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isLocked) return;

            // Check if the collider is the player
            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // Find a carried key
                var carriedKey = Key.GetCarriedKey(player.transform);
                if (carriedKey != null)
                {
                    Unlock(carriedKey);
                }
            }
        }

        private void Unlock(Key key)
        {
            _isLocked = false;
            
            // Disable trigger to prevent multi-activation
            if (_collider != null)
            {
                _collider.enabled = false;
            }

            // Determine destination for the key
            Vector3 flyDestination = _keyTargetPoint != null ? _keyTargetPoint.position : transform.position;

            // Consume the key and open the door upon completion
            key.Consume(flyDestination, OpenDoor);
        }

        private void OpenDoor()
        {
            // Play unlock sound
            var audioService = ServiceLocator.Get<IAudioService>();
            if (audioService != null && _unlockSFX != null)
            {
                audioService.PlaySFX(_unlockSFX);
            }

            if (_doorObject != null)
            {
                // Slide door up
                Vector3 endPos = _doorObject.transform.position + Vector3.up * _slideUpDistance;
                
                // Disable door collider immediately so player can pass through without waiting for the animation
                var doorCollider = _doorObject.GetComponent<Collider2D>();
                if (doorCollider != null)
                {
                    doorCollider.enabled = false;
                }

                // Create DOTween sequence to animate the door opening
                Sequence openSequence = DOTween.Sequence();
                openSequence.Join(_doorObject.transform.DOMove(endPos, _doorOpenDuration).SetEase(Ease.InOutQuad));

                if (_fadeSprite)
                {
                    var spriteRenderer = _doorObject.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        openSequence.Join(spriteRenderer.DOFade(0f, _doorOpenDuration));
                    }
                    
                    // Check for sprite renderers in children as well
                    var childSprites = _doorObject.GetComponentsInChildren<SpriteRenderer>();
                    foreach (var sprite in childSprites)
                    {
                        if (sprite != spriteRenderer)
                        {
                            openSequence.Join(sprite.DOFade(0f, _doorOpenDuration));
                        }
                    }
                }

                openSequence.OnComplete(() =>
                {
                    _doorObject.SetActive(false);
                });
            }
            else
            {
                // If no door object is specified, just deactivate this lock game object
                gameObject.SetActive(false);
            }
        }
    }
}
