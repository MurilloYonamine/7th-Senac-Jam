using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Seventh.Core.Services;

namespace Seventh.Gameplay.Environment
{
    public class TilemapPathfinder : MonoBehaviour, ITilemapPathfinder
    {
        [Header("Tilemaps")]
        [SerializeField] private Tilemap _floorTilemap;
        [SerializeField] private Tilemap _wallsTilemap;

        [Header("Pathfinding Settings")]
        [SerializeField] private bool _allowDiagonal = true;
        [SerializeField] private int _maxIterations = 1000;

        private void Awake()
        {
            ServiceLocator.Register<ITilemapPathfinder>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<ITilemapPathfinder>();
        }

        public bool IsWalkable(Vector3Int position, Collider2D roomCollider = null)
        {
            if (_wallsTilemap != null && _wallsTilemap.HasTile(position))
            {
                return false;
            }

            if (_floorTilemap != null && !_floorTilemap.HasTile(position))
            {
                return false;
            }

            if (roomCollider != null)
            {
                Grid grid = _floorTilemap != null ? _floorTilemap.layoutGrid : _wallsTilemap.layoutGrid;
                Vector3 cellCenterWorld = grid.GetCellCenterWorld(position);
                if (!roomCollider.OverlapPoint(cellCenterWorld))
                {
                    return false;
                }
            }

            return true;
        }

        public List<Vector3> FindPath(Vector3 startWorld, Vector3 targetWorld, Collider2D roomCollider = null)
        {
            if (_floorTilemap == null && _wallsTilemap == null)
            {
                Debug.LogWarning("[TilemapPathfinder] No tilemaps assigned to the pathfinder.");
                return new List<Vector3> { targetWorld };
            }

            Grid grid = _floorTilemap != null ? _floorTilemap.layoutGrid : _wallsTilemap.layoutGrid;
            Vector3Int startCell = grid.WorldToCell(startWorld);
            Vector3Int targetCell = grid.WorldToCell(targetWorld);

            if (!IsWalkable(targetCell, roomCollider))
            {
                targetCell = FindClosestWalkableNeighbor(targetCell, roomCollider);
            }

            if (startCell == targetCell)
            {
                return new List<Vector3> { targetWorld };
            }

            var openList = new List<PathNode>();
            var closedList = new HashSet<Vector3Int>();

            var startNode = new PathNode(startCell)
            {
                GCost = 0,
                HCost = CalculateDistanceCost(startCell, targetCell)
            };
            openList.Add(startNode);

            int iterations = 0;
            while (openList.Count > 0 && iterations < _maxIterations)
            {
                iterations++;
                PathNode currentNode = GetLowestFCostNode(openList);

                if (currentNode.Position == targetCell)
                {
                    return RetracePath(currentNode, grid, targetWorld);
                }

                openList.Remove(currentNode);
                closedList.Add(currentNode.Position);

                foreach (Vector3Int neighborPos in GetNeighbors(currentNode.Position, _allowDiagonal))
                {
                    if (closedList.Contains(neighborPos) || !IsWalkable(neighborPos, roomCollider))
                    {
                        continue;
                    }

                    if (_allowDiagonal && IsCuttingCorner(currentNode.Position, neighborPos, roomCollider))
                    {
                        continue;
                    }

                    int newGCost = currentNode.GCost + CalculateDistanceCost(currentNode.Position, neighborPos);
                    PathNode neighborNode = openList.Find(n => n.Position == neighborPos);

                    if (neighborNode == null)
                    {
                        neighborNode = new PathNode(neighborPos)
                        {
                            GCost = newGCost,
                            HCost = CalculateDistanceCost(neighborPos, targetCell),
                            Parent = currentNode
                        };
                        openList.Add(neighborNode);
                    }
                    else if (newGCost < neighborNode.GCost)
                    {
                        neighborNode.GCost = newGCost;
                        neighborNode.Parent = currentNode;
                    }
                }
            }

            return new List<Vector3> { targetWorld };
        }

        private Vector3Int FindClosestWalkableNeighbor(Vector3Int cell, Collider2D roomCollider = null)
        {
            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

            queue.Enqueue(cell);
            visited.Add(cell);

            int searchRadiusLimit = 5;
            int searchedCount = 0;

            while (queue.Count > 0 && searchedCount < 100)
            {
                searchedCount++;
                Vector3Int current = queue.Dequeue();

                if (IsWalkable(current, roomCollider))
                {
                    return current;
                }

                foreach (Vector3Int neighbor in GetNeighbors(current, false))
                {
                    if (!visited.Contains(neighbor) && Mathf.Abs(neighbor.x - cell.x) <= searchRadiusLimit && Mathf.Abs(neighbor.y - cell.y) <= searchRadiusLimit)
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return cell;
        }

        private bool IsCuttingCorner(Vector3Int current, Vector3Int neighbor, Collider2D roomCollider = null)
        {
            int dx = neighbor.x - current.x;
            int dy = neighbor.y - current.y;

            if (dx != 0 && dy != 0)
            {
                Vector3Int side1 = new Vector3Int(current.x + dx, current.y, 0);
                Vector3Int side2 = new Vector3Int(current.x, current.y + dy, 0);

                if (!IsWalkable(side1, roomCollider) || !IsWalkable(side2, roomCollider))
                {
                    return true;
                }
            }
            return false;
        }

        private List<Vector3> RetracePath(PathNode endNode, Grid grid, Vector3 exactTargetWorld)
        {
            List<Vector3> path = new List<Vector3>();
            PathNode current = endNode;

            while (current != null)
            {
                // Align to cell center in world space
                Vector3 cellCenterWorld = grid.GetCellCenterWorld(current.Position);
                path.Add(cellCenterWorld);
                current = current.Parent;
            }

            path.Reverse();

            // Refine final node to exact target destination to avoid jitter at destination
            if (path.Count > 0)
            {
                path[path.Count - 1] = exactTargetWorld;
            }

            return path;
        }

        private int CalculateDistanceCost(Vector3Int a, Vector3Int b)
        {
            int xDistance = Mathf.Abs(a.x - b.x);
            int yDistance = Mathf.Abs(a.y - b.y);
            int remaining = Mathf.Abs(xDistance - yDistance);
            return Mathf.Min(xDistance, yDistance) * 14 + remaining * 10;
        }

        private PathNode GetLowestFCostNode(List<PathNode> nodeList)
        {
            PathNode lowest = nodeList[0];
            for (int i = 1; i < nodeList.Count; i++)
            {
                if (nodeList[i].FCost < lowest.FCost || (nodeList[i].FCost == lowest.FCost && nodeList[i].HCost < lowest.HCost))
                {
                    lowest = nodeList[i];
                }
            }
            return lowest;
        }

        private List<Vector3Int> GetNeighbors(Vector3Int position, bool allowDiagonal)
        {
            var neighbors = new List<Vector3Int>();

            if (allowDiagonal)
            {
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0) continue;
                        neighbors.Add(new Vector3Int(position.x + x, position.y + y, 0));
                    }
                }
            }
            else
            {
                neighbors.Add(new Vector3Int(position.x + 1, position.y, 0));
                neighbors.Add(new Vector3Int(position.x - 1, position.y, 0));
                neighbors.Add(new Vector3Int(position.x, position.y + 1, 0));
                neighbors.Add(new Vector3Int(position.x, position.y - 1, 0));
            }

            return neighbors;
        }

        private class PathNode
        {
            public Vector3Int Position;
            public int GCost;
            public int HCost;
            public int FCost => GCost + HCost;
            public PathNode Parent;

            public PathNode(Vector3Int position)
            {
                Position = position;
            }
        }
    }
}
