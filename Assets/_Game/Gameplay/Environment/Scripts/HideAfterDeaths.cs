using UnityEngine;
using Seventh.Gameplay.Player;

namespace Seventh.Gameplay.Environment
{
    public class HideAfterDeaths : MonoBehaviour
    {
        [Header("Hide Settings")]
        [Tooltip("The number of deaths after which this object will be hidden.")]
        [SerializeField] private int _hideAfterDeathCount = 1;
        
        [Header("References (Optional)")]
        [SerializeField] private PlayerHealth _playerHealth;

        private static int _sessionDeathCount = 0;

        private void Awake()
        {
            if (_playerHealth == null)
            {
                _playerHealth = FindAnyObjectByType<PlayerHealth>();
            }

            // If we already reached the threshold before this object is enabled/loaded, disable it immediately
            CheckAndHide();
        }

        private void OnEnable()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnDeath += OnPlayerDeath;
            }
        }

        private void OnDisable()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnDeath -= OnPlayerDeath;
            }
        }

        private void OnPlayerDeath()
        {
            _sessionDeathCount++;
            CheckAndHide();
        }

        private void CheckAndHide()
        {
            if (_sessionDeathCount >= _hideAfterDeathCount)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Resets the death counter (e.g. when returning to the main menu or starting a new game session).
        /// </summary>
        public static void ResetSessionDeaths()
        {
            _sessionDeathCount = 0;
        }
    }
}
