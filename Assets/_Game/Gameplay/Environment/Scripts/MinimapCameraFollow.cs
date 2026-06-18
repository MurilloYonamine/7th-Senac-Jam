using UnityEngine;
using Seventh.Gameplay.Player;

namespace Seventh.Gameplay.Environment
{
    public class MinimapCameraFollow : MonoBehaviour
    {
        [Header("Follow Target")]
        [SerializeField] private Transform _target;
        
        [Header("Camera Height")]
        [SerializeField] private float _cameraZ = -10f;

        private void Start()
        {
            if (_target == null)
            {
                var player = FindAnyObjectByType<PlayerController>();
                if (player != null)
                {
                    _target = player.transform;
                }
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // Follow player's X and Y, maintaining the specified Z height
            transform.position = new Vector3(_target.position.x, _target.position.y, _cameraZ);
        }
    }
}
