using UnityEngine;

public class Node
{
    public bool walkable;          // ¿Se puede caminar por el nodo?
    public Vector3 worldPosition;  // Posición en world space
    public int gridX;              // Columna en el grid
    public int gridY;              // Fila en el grid

    public float gCost;  // Coste acumulado desde el inicio (g)
    public float hCost;  // Estimación hasta el target (h)
    public float fCost => gCost + hCost;  // Coste total, se utiliza para estimar el coste del nodo (f = g + h)

    public bool isDark;          // Zona oscura / sigilosa
    public float terrainPenalty; // Coste extra por terreno especial

    public Node parent;  // Nodo anterior. Utilizado para hacer el recorrido inverso.

    public Node(bool walkable, Vector3 worldPosition, int gridX, int gridY, bool isDark = false, float terrainPenalty = 0f)
    {
        this.walkable      = walkable;
        this.worldPosition = worldPosition;
        this.gridX         = gridX;
        this.gridY         = gridY;
        this.isDark        = isDark;
        this.terrainPenalty = terrainPenalty;
    }
}
