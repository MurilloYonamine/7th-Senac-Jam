using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Seventh.Gameplay.Environment
{
    [RequireComponent(typeof(Tilemap))]
    public class TilemapSpikeTrap : MonoBehaviour
    {
        [Header("Tilemap Settings")]
        [Tooltip("The tile to search for in the tilemap to register spike locations")]
        [SerializeField] private TileBase _spikeTriggerTile;
        
        [Tooltip("The sequence of tiles representing the spike animation frames (e.g. frame 0 = retracted, frame N = fully extended)")]
        [SerializeField] private TileBase[] _animationFrames;
        
        [Header("Timing Settings")]
        [SerializeField] private float _frameRate = 0.08f;
        [SerializeField] private float _extendedDuration = 1.2f;
        [SerializeField] private float _cooldown = 2.5f;
        [SerializeField] private bool _autoCycle = true;

        [Header("Collision/Damage Settings")]
        [Tooltip("Optional: The collider that deals damage to the player when spikes are extended")]
        [SerializeField] private Collider2D _damageCollider;
        [SerializeField] private int _damageAmount = 10;
        [SerializeField] private float _knockbackForce = 4f;

        [Header("Audio Settings")]
        [SerializeField] private AudioClip _damageSFX;
        [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 1f;

        private Tilemap _tilemap;
        private List<Vector3Int> _spikePositions = new List<Vector3Int>();
        private Coroutine _cycleCoroutine;
        private bool _isTrapActive = false;

        private void Awake()
        {
            _tilemap = GetComponent<Tilemap>();
            if (_damageCollider == null)
            {
                _damageCollider = GetComponent<Collider2D>();
            }

            // Initially disable the collider so retracted spikes don't hurt the player
            if (_damageCollider != null)
            {
                _damageCollider.enabled = false;
            }
        }

        private void Start()
        {
            ScanSpikePositions();

            if (_autoCycle)
            {
                StartAutoCycle();
            }
            else
            {
                // Set initial retracted frame for all spike positions
                SetSpikeFrame(0);
            }
        }

        private void ScanSpikePositions()
        {
            _spikePositions.Clear();
            if (_tilemap == null || _spikeTriggerTile == null) return;

            BoundsInt bounds = _tilemap.cellBounds;
            
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    if (_tilemap.HasTile(pos) && _tilemap.GetTile(pos) == _spikeTriggerTile)
                    {
                        _spikePositions.Add(pos);
                    }
                }
            }

            Debug.Log($"[TilemapSpikeTrap] Registered {_spikePositions.Count} spike tile positions on {_tilemap.gameObject.name}.");
        }

        public void StartAutoCycle()
        {
            StopCycle();
            _cycleCoroutine = StartCoroutine(AutoCycleRoutine());
        }

        public void StopAutoCycle()
        {
            StopCycle();
            SetSpikeFrame(0);
            if (_damageCollider != null)
            {
                _damageCollider.enabled = false;
            }
        }

        public void TriggerTrap()
        {
            if (_isTrapActive) return;
            StartCoroutine(SingleTriggerRoutine());
        }

        private void StopCycle()
        {
            if (_cycleCoroutine != null)
            {
                StopCoroutine(_cycleCoroutine);
                _cycleCoroutine = null;
            }
        }

        private void SetSpikeFrame(int frameIndex)
        {
            if (_animationFrames == null || _animationFrames.Length == 0) return;
            int clampedIndex = Mathf.Clamp(frameIndex, 0, _animationFrames.Length - 1);
            TileBase frameTile = _animationFrames[clampedIndex];

            foreach (var pos in _spikePositions)
            {
                _tilemap.SetTile(pos, frameTile);
            }
        }

        private IEnumerator AutoCycleRoutine()
        {
            while (true)
            {
                yield return StartCoroutine(PlaySpikesSequence());
                yield return new WaitForSeconds(_cooldown);
            }
        }

        private IEnumerator SingleTriggerRoutine()
        {
            _isTrapActive = true;
            yield return StartCoroutine(PlaySpikesSequence());
            _isTrapActive = false;
        }

        private IEnumerator PlaySpikesSequence()
        {
            if (_animationFrames == null || _animationFrames.Length == 0) yield break;

            // Phase 1: Extend (forward through frames)
            for (int i = 0; i < _animationFrames.Length; i++)
            {
                SetSpikeFrame(i);
                
                // Enable collider on the last frame (fully extended)
                if (i == _animationFrames.Length - 1 && _damageCollider != null)
                {
                    _damageCollider.enabled = true;

                    // Immediately query and damage any player that is already standing on the spikes when they pop up
                    List<Collider2D> results = new List<Collider2D>();
                    ContactFilter2D filter = new ContactFilter2D();
                    filter.useTriggers = true;
                    _damageCollider.Overlap(filter, results);
                    foreach (var col in results)
                    {
                        if (col.CompareTag("Player"))
                        {
                            DealDamage(col);
                        }
                    }
                }
                
                yield return new WaitForSeconds(_frameRate);
            }

            // Phase 2: Stay extended
            yield return new WaitForSeconds(_extendedDuration);

            // Phase 3: Retract (backward through frames)
            for (int i = _animationFrames.Length - 1; i >= 0; i--)
            {
                // Disable collider as soon as they start retracting
                if (i < _animationFrames.Length - 1 && _damageCollider != null)
                {
                    _damageCollider.enabled = false;
                }

                SetSpikeFrame(i);
                yield return new WaitForSeconds(_frameRate);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                DealDamage(collision);
            }
        }

        private void DealDamage(Collider2D collision)
        {
            if (collision.TryGetComponent(out Seventh.Gameplay.Health.IDamageable damageable))
            {
                int prevHealth = (damageable as Seventh.Gameplay.Health.HealthBase)?.CurrentHealth ?? 0;

                Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
                Seventh.Gameplay.Health.DamageInfo damageInfo = new Seventh.Gameplay.Health.DamageInfo(
                    _damageAmount,
                    _knockbackForce,
                    knockbackDir,
                    gameObject,
                    Seventh.Gameplay.Health.HitIntensity.Light,
                    false
                );
                damageable.TakeDamage(damageInfo);

                int currentHealth = (damageable as Seventh.Gameplay.Health.HealthBase)?.CurrentHealth ?? 0;

                // Play custom spike hit SFX if player actually lost health
                if (currentHealth < prevHealth && _damageSFX != null)
                {
                    var audioService = Seventh.Core.Services.ServiceLocator.Get<Seventh.Core.Services.IAudioService>();
                    if (audioService != null)
                    {
                        audioService.PlaySFX(_damageSFX, new Seventh.Core.Services.AudioSettings(volumeOffset: _sfxVolume - 1f, spatialPosition: collision.transform.position));
                    }
                }
            }
        }

        private void OnDestroy()
        {
            StopCycle();
        }
    }
}
