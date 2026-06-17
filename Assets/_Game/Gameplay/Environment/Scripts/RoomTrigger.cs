using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using Seventh.Core.Services;
using Seventh.Core.Events;
using Seventh.Gameplay.Player;

namespace Seventh.Gameplay.Environment
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class RoomTrigger : MonoBehaviour
    {
        private static readonly List<RoomTrigger> AllRooms = new List<RoomTrigger>();

        [Header("Room Settings")]
        [SerializeField] private string _roomId;
        [SerializeField] private Vector2 _roomPosition;

        [Header("Cinemachine Camera")]
        [SerializeField] private CinemachineCamera _roomCamera;

        private BoxCollider2D _collider;
        private IEventBus _eventBus;

        public string RoomId => _roomId;
        public Vector2 RoomPosition => _roomPosition;
        public CinemachineCamera RoomCamera => _roomCamera;
        public BoxCollider2D RoomCollider => _collider;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
            _collider.isTrigger = true;

            if (string.IsNullOrEmpty(_roomId))
            {
                _roomId = gameObject.name;
            }
            if (_roomPosition == Vector2.zero)
            {
                _roomPosition = transform.position;
            }

            if (_roomCamera != null)
            {
                var lens = _roomCamera.Lens;
                lens.NearClipPlane = 0f;
                _roomCamera.Lens = lens;
            }
        }

        private void Start()
        {
            _eventBus = ServiceLocator.Get<IEventBus>();
        }

        private void OnEnable()
        {
            if (!AllRooms.Contains(this))
            {
                AllRooms.Add(this);
            }
        }

        private void OnDisable()
        {
            AllRooms.Remove(this);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                ActivateRoom();
            }
        }

        public void ActivateRoom()
        {
            foreach (var room in AllRooms)
            {
                if (room != null && room.RoomCamera != null)
                {
                    room.RoomCamera.Priority = (room == this) ? 10 : 0;
                }
            }

            _eventBus?.Publish(new RoomChangedEvent(_roomId, _roomPosition));
        }

        private void OnDrawGizmos()
        {
            if (_collider == null)
            {
                _collider = GetComponent<BoxCollider2D>();
            }

            if (_collider != null)
            {
                Gizmos.color = new Color(0f, 0.8f, 0.8f, 0.15f);
                Gizmos.DrawCube(_collider.bounds.center, _collider.bounds.size);
                Gizmos.color = new Color(0f, 0.8f, 0.8f, 0.6f);
                Gizmos.DrawWireCube(_collider.bounds.center, _collider.bounds.size);
            }
        }
    }
}
