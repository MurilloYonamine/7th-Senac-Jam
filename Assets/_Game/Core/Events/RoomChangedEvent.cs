using UnityEngine;

namespace Seventh.Core.Events
{
    public readonly struct RoomChangedEvent
    {
        public readonly string RoomId;
        public readonly Vector2 RoomPosition;

        public RoomChangedEvent(string roomId, Vector2 roomPosition)
        {
            RoomId = roomId;
            RoomPosition = roomPosition;
        }
    }
}
