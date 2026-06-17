using System.Collections.Generic;
using UnityEngine;

namespace Seventh.Core.Services
{
    public interface ITilemapPathfinder
    {
        bool IsWalkable(Vector3Int position, Collider2D roomCollider = null);
        List<Vector3> FindPath(Vector3 startWorld, Vector3 targetWorld, Collider2D roomCollider = null);
    }
}
