using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class GridManager : MonoBehaviour
{
    public static GridManager instance;

    [Header("Tamaño del grid")]
    public Vector2 gridWorldSize = new Vector2(20f, 20f);
    public float nodeRadius = 0.5f;

    [Header("Obstáculos")]
    public LayerMask unwalkableMask;

    [Header("Sigilo / Zonas oscuras")]
    public LayerMask darkTileMask;
    public float darkTilePenalty = 10f;

    Node[,] _grid;
    float _nodeDiameter;
    int _gridSizeX, _gridSizeY;

    [HideInInspector] public List<Node> path;


    void Awake()
    {
        instance = this;

        _nodeDiameter = nodeRadius * 2f;

        _gridSizeX = Mathf.RoundToInt(gridWorldSize.x / _nodeDiameter);
        _gridSizeY = Mathf.RoundToInt(gridWorldSize.y / _nodeDiameter);

        CreateGrid();
    }

    public int MaxSize => _gridSizeX * _gridSizeY;

    void CreateGrid()
    {
        _grid = new Node[_gridSizeX, _gridSizeY];

        Vector3 worldBottomLeft = transform.position
            - Vector3.right * gridWorldSize.x * 0.5f
            - Vector3.forward * gridWorldSize.y * 0.5f;

        for (int x = 0; x < _gridSizeX; x++)
        {
            for (int y = 0; y < _gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft
                    + Vector3.right * (x * _nodeDiameter + nodeRadius)
                    + Vector3.forward * (y * _nodeDiameter + nodeRadius);

                bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask);
                bool isDark = Physics.CheckSphere(worldPoint, nodeRadius, darkTileMask);
                float penalty = isDark ? darkTilePenalty : 0f;
                _grid[x, y] = new Node(walkable, worldPoint, x, y, isDark, penalty);
            }
        }
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x - transform.position.x + gridWorldSize.x * 0.5f) / gridWorldSize.x;
        float percentY = (worldPosition.z - transform.position.z + gridWorldSize.y * 0.5f) / gridWorldSize.y;

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((_gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((_gridSizeY - 1) * percentY);

        return _grid[x, y];
    }

    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int checkX = node.gridX + dx;
                int checkY = node.gridY + dy;

                if (checkX >= 0 && checkX < _gridSizeX &&
                    checkY >= 0 && checkY < _gridSizeY)
                {
                    neighbours.Add(_grid[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }


    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position,
            new Vector3(gridWorldSize.x, 1f, gridWorldSize.y));

        if (_grid == null) return;

        foreach (Node node in _grid)
        {
            if (path != null && path.Contains(node))
                Gizmos.color = Color.blue;          // Path found
            else if (node.isDark)
                Gizmos.color = new Color(0.2f, 0.2f, 0.6f, 0.5f); // Dark tile
            else
                Gizmos.color = node.walkable
                    ? new Color(1f, 1f, 1f, 0.3f)  // Walkable (semi-transparent)
                    : new Color(1f, 0f, 0f, 0.6f);  // Unwalkable (red)

            Gizmos.DrawCube(node.worldPosition,
                Vector3.one * (_nodeDiameter - 0.1f));
        }
    }
}
