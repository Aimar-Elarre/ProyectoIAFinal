using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder : MonoBehaviour
{
    public static AStarPathfinder instance;

    public enum HeuristicType
    {
        Euclidean,
        Manhattan,
        Diagonal
    }

    [Header("Heurística")]
    [Tooltip("Selecciona la variante de heurística que quieres probar")]
    public HeuristicType heuristicType = HeuristicType.Euclidean;

    void Awake()
    {
        instance = this;
    }


    public List<Node> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Node startNode  = GridManager.instance.NodeFromWorldPoint(startPos);
        Node targetNode = GridManager.instance.NodeFromWorldPoint(targetPos);

        if (!startNode.walkable)
        {
            Debug.LogWarning("A*: El nodo de INICIO no es walkable. Mueve el Seeker a una celda blanca del grid.");
            return null;
        }
        if (!targetNode.walkable)
        {
            Debug.LogWarning("A*: El nodo de DESTINO no es walkable. Mueve el Target a una celda blanca del grid.");
            return null;
        }

        List<Node>    openList  = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        startNode.gCost = 0f;
        startNode.hCost = GetHeuristic(startNode, targetNode);
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            Node current = openList[0];
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].fCost < current.fCost ||
                    (Mathf.Approximately(openList[i].fCost, current.fCost) &&
                     openList[i].hCost < current.hCost))
                {
                    current = openList[i];
                }
            }

            openList.Remove(current);
            closedSet.Add(current);

            if (current == targetNode)
                return RetracePath(startNode, targetNode);

            foreach (Node neighbour in GridManager.instance.GetNeighbours(current))
            {
                if (!neighbour.walkable || closedSet.Contains(neighbour))
                    continue;

                if (heuristicType == HeuristicType.Manhattan)
                {
                    int ndx = Mathf.Abs(current.gridX - neighbour.gridX);
                    int ndy = Mathf.Abs(current.gridY - neighbour.gridY);
                    if (ndx == 1 && ndy == 1) continue;
                }

                float newGCost = current.gCost + GetMoveCost(current, neighbour);

                if (newGCost < neighbour.gCost || !openList.Contains(neighbour))
                {
                    neighbour.gCost  = newGCost;
                    neighbour.hCost  = GetHeuristic(neighbour, targetNode);
                    neighbour.parent = current;

                    if (!openList.Contains(neighbour))
                        openList.Add(neighbour);
                }
            }
        }

        Debug.LogWarning("A*: No se encontró camino entre los puntos dados.");
        return null;
    }


    float GetHeuristic(Node a, Node b)
    {
        return heuristicType switch
        {
            HeuristicType.Euclidean => HeuristicEuclidean(a, b),
            HeuristicType.Manhattan => HeuristicManhattan(a, b),
            HeuristicType.Diagonal  => HeuristicDiagonal(a, b),
            _                       => 0f
        };
    }

    float GetMoveCost(Node a, Node b)
    {
        int dx = Mathf.Abs(a.gridX - b.gridX);
        int dy = Mathf.Abs(a.gridY - b.gridY);
        float baseCost = (dx == 1 && dy == 1) ? 14f : 10f;
        float penalty = (a.terrainPenalty + b.terrainPenalty) * 0.5f;
        return baseCost + penalty;
    }

    List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != null && currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Reverse();
        GridManager.instance.path = path;
        return path;
    }

    float HeuristicEuclidean(Node a, Node b)
    {
        float dx = Mathf.Abs(a.gridX - b.gridX);
        float dy = Mathf.Abs(a.gridY - b.gridY);
        return Mathf.Sqrt(dx * dx + dy * dy) * 10f;
    }

    float HeuristicManhattan(Node a, Node b)
    {
        float dx = Mathf.Abs(a.gridX - b.gridX);
        float dy = Mathf.Abs(a.gridY - b.gridY);
        return (dx + dy) * 10f;
    }

    float HeuristicDiagonal(Node a, Node b)
    {
        float dx = Mathf.Abs(a.gridX - b.gridX);
        float dy = Mathf.Abs(a.gridY - b.gridY);
        return Mathf.Max(dx, dy) * 10f;
    }
}
