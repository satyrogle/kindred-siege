using UnityEngine;
using System.Collections.Generic;

namespace KindredSiege.Battle
{
    /// <summary>
    /// Grid system for the battlefield. Handles unit positioning, pathfinding, and spatial queries.
    /// Start with a simple square grid — upgrade to hex later if desired.
    /// </summary>
    public class BattleGrid : MonoBehaviour
    {
        [Header("Grid Config")]
        [SerializeField] private int width = 12;
        [SerializeField] private int height = 8;
        [SerializeField] private float cellSize = 1.5f;
        [SerializeField] private Vector3 originOffset = Vector3.zero;

        [Header("Visuals")]
        [SerializeField] private GameObject cellPrefab; // Optional: visual tile prefab
        [SerializeField] private Color team1Zone = new Color(0.2f, 0.4f, 0.8f, 0.2f);
        [SerializeField] private Color team2Zone = new Color(0.8f, 0.2f, 0.2f, 0.2f);

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;

        // Grid occupancy — which unit is at each cell
        private UnitController[,] occupancy;

        // Hazard overlay — terrain effects per cell (GDD §12)
        private HazardType[,] hazards;

        private void Awake()
        {
            occupancy = new UnitController[width, height];
            hazards   = new HazardType[width, height];
        }

        // ─── Coordinate Conversion ───

        /// <summary>Convert grid coordinates to world position (centre of cell).</summary>
        public Vector3 GridToWorld(int x, int y)
        {
            return new Vector3(
                x * cellSize + cellSize * 0.5f,
                0f,
                y * cellSize + cellSize * 0.5f
            ) + originOffset;
        }

        /// <summary>Convert grid coordinates to world position.</summary>
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return GridToWorld(gridPos.x, gridPos.y);
        }

        /// <summary>Convert world position to grid coordinates.</summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            Vector3 local = worldPos - originOffset;
            int x = Mathf.FloorToInt(local.x / cellSize);
            int y = Mathf.FloorToInt(local.z / cellSize);
            return new Vector2Int(
                Mathf.Clamp(x, 0, width - 1),
                Mathf.Clamp(y, 0, height - 1)
            );
        }

        // ─── Occupancy ───

        public bool IsInBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;
        public bool IsInBounds(Vector2Int pos) => IsInBounds(pos.x, pos.y);

        public bool IsOccupied(int x, int y)
        {
            if (!IsInBounds(x, y)) return true;
            return occupancy[x, y] != null;
        }

        public bool IsOccupied(Vector2Int pos) => IsOccupied(pos.x, pos.y);

        public void PlaceUnit(UnitController unit, int x, int y)
        {
            if (!IsInBounds(x, y)) return;
            occupancy[x, y] = unit;
            unit.transform.position = GridToWorld(x, y);
        }

        public void PlaceUnit(UnitController unit, Vector2Int pos)
        {
            PlaceUnit(unit, pos.x, pos.y);
        }

        public void RemoveUnit(int x, int y)
        {
            if (IsInBounds(x, y))
                occupancy[x, y] = null;
        }

        public void ClearGrid()
        {
            occupancy = new UnitController[width, height];
            hazards   = new HazardType[width, height];
        }

        // ─── Hazard Overlay (GDD §12) ───

        /// <summary>Set the hazard type for a grid cell.</summary>
        public void SetHazard(int x, int y, HazardType type)
        {
            if (!IsInBounds(x, y)) return;
            hazards[x, y] = type;
        }

        public void SetHazard(Vector2Int pos, HazardType type) => SetHazard(pos.x, pos.y, type);

        /// <summary>Get the hazard type at a world-space position. Clamps to grid bounds — never throws.</summary>
        public HazardType GetHazardAt(Vector3 worldPos)
        {
            if (hazards == null) return HazardType.None;
            var cell = WorldToGrid(worldPos); // already clamped
            return hazards[cell.x, cell.y];
        }

        public UnitController GetUnitAt(int x, int y)
        {
            if (!IsInBounds(x, y)) return null;
            return occupancy[x, y];
        }

        // ─── Spatial Queries ───

        /// <summary>Get all empty cells in a region (for unit placement).</summary>
        public List<Vector2Int> GetEmptyCells(int startX, int startY, int regionWidth, int regionHeight)
        {
            var cells = new List<Vector2Int>();
            for (int x = startX; x < startX + regionWidth && x < width; x++)
            {
                for (int y = startY; y < startY + regionHeight && y < height; y++)
                {
                    if (IsInBounds(x, y) && !IsOccupied(x, y))
                        cells.Add(new Vector2Int(x, y));
                }
            }
            return cells;
        }

        /// <summary>Get team 1 deployment zone (left side of grid).</summary>
        public List<Vector2Int> GetTeam1Zone()
        {
            return GetEmptyCells(0, 0, width / 3, height);
        }

        /// <summary>Get team 2 deployment zone (right side of grid).</summary>
        public List<Vector2Int> GetTeam2Zone()
        {
            int startX = width - width / 3;
            return GetEmptyCells(startX, 0, width / 3, height);
        }

        // ─── Debug Visualisation ───

        private void OnDrawGizmos()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3 pos = GridToWorld(x, y);

                    // Base wireframe — deployment zones
                    if (x < width / 3)
                        Gizmos.color = team1Zone;
                    else if (x >= width - width / 3)
                        Gizmos.color = team2Zone;
                    else
                        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.1f);

                    Gizmos.DrawWireCube(pos, new Vector3(cellSize * 0.9f, 0.05f, cellSize * 0.9f));

                    // Hazard overlay — filled cube, colour-coded
                    HazardType haz = (hazards != null) ? hazards[x, y] : HazardType.None;
                    switch (haz)
                    {
                        case HazardType.DeepWater:
                            Gizmos.color = new Color(0.1f, 0.3f, 0.9f, 0.45f);
                            Gizmos.DrawCube(pos, new Vector3(cellSize * 0.85f, 0.08f, cellSize * 0.85f));
                            break;
                        case HazardType.Shrine:
                            Gizmos.color = new Color(0.9f, 0.8f, 0.1f, 0.45f);
                            Gizmos.DrawCube(pos, new Vector3(cellSize * 0.85f, 0.08f, cellSize * 0.85f));
                            break;
                        case HazardType.EldritchGround:
                            Gizmos.color = new Color(0.6f, 0.1f, 0.9f, 0.45f);
                            Gizmos.DrawCube(pos, new Vector3(cellSize * 0.85f, 0.08f, cellSize * 0.85f));
                            break;
                    }
                }
            }
        }
    }
}
