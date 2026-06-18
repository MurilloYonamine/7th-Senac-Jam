using System.Collections.Generic;
using UnityEngine;

namespace Seventh.Core.Services
{
    public interface ITilemapPathfinder
    {
        bool IsWalkable(Vector3Int position, Collider2D roomCollider = null);
        bool IsWorldPositionWalkable(Vector3 worldPosition, Collider2D roomCollider = null);
        List<Vector3> FindPath(Vector3 startWorld, Vector3 targetWorld, Collider2D roomCollider = null);
        Vector3 FindBestFleePosition(Vector3 startWorld, Vector3 playerWorld, float searchRadius, Collider2D roomCollider = null);
    }
}
