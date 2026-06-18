using UnityEngine;
using UnityEngine.Events;
using Seventh.Gameplay.Player;

namespace Seventh.Gameplay.Items
{
    [RequireComponent(typeof(Collider2D))]
    public class CutsceneTrigger : MonoBehaviour
    {
        [Header("Trigger Settings")]
        [SerializeField] private bool _destroyOnTrigger = true;
        [SerializeField] private GameObject _visualToDeactivate;

        [Header("Events")]
        [SerializeField] private UnityEvent _onPlayerTriggered;

        private bool _hasTriggered = false;

        private void Awake()
        {
            // Ensure the collider is set as a trigger
            if (TryGetComponent<Collider2D>(out var col))
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasTriggered) return;

            // Check if the colliding object is the player
            if (other.GetComponent<PlayerMovement>() != null || other.CompareTag("Player"))
            {
                TriggerEvent();
            }
        }

        private void TriggerEvent()
        {
            _hasTriggered = true;
            _onPlayerTriggered?.Invoke();

            if (_visualToDeactivate != null)
            {
                _visualToDeactivate.SetActive(false);
            }

            if (_destroyOnTrigger)
            {
                // Destroy the root object to clean up the scene
                Destroy(gameObject);
            }
        }
    }
}
