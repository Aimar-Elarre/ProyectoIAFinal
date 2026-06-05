
using UnityEngine;

public class CoverSystem : MonoBehaviour
{
    public static CoverSystem Instance { get; private set; }

    CoverPoint[] _coverPoints;

    void Awake()
    {
        Instance = this;
        _coverPoints = FindObjectsByType<CoverPoint>(FindObjectsSortMode.None);
        Debug.Log($"[CoverSystem] {_coverPoints.Length} cover points registrados.");
    }

    public CoverPoint FindBestCover(Vector3 agentPos, Vector3 threatPos)
    {
        CoverPoint best = null;
        float bestScore = float.MinValue;

        foreach (var cp in _coverPoints)
        {
            if (cp.IsOccupied) continue;
            float score = cp.ScoreFor(agentPos, threatPos);
            if (score > bestScore)
            {
                bestScore = score;
                best = cp;
            }
        }

        return best;
    }

    public CoverPoint FindBestCoverInRange(
        Vector3 agentPos, Vector3 threatPos, float maxDistance)
    {
        CoverPoint best = null;
        float bestScore = float.MinValue;

        foreach (var cp in _coverPoints)
        {
            if (cp.IsOccupied) continue;
            if (Vector3.Distance(agentPos, cp.transform.position) > maxDistance) continue;

            float score = cp.ScoreFor(agentPos, threatPos);
            if (score > bestScore)
            {
                bestScore = score;
                best = cp;
            }
        }

        return best;
    }

    public CoverPoint FindNearestCover(Vector3 agentPos)
    {
        CoverPoint nearest = null;
        float minDist = float.MaxValue;

        foreach (var cp in _coverPoints)
        {
            if (cp.IsOccupied) continue;
            float dist = Vector3.Distance(agentPos, cp.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = cp;
            }
        }

        return nearest;
    }

    public void ReleaseCover(MonoBehaviour occupant)
    {
        foreach (var cp in _coverPoints)
            if (cp.Occupant == occupant) cp.Vacate();
    }

    void OnDrawGizmos()
    {
        if (_coverPoints == null) return;
        foreach (var cp in _coverPoints)
        {
            if (cp == null) continue;
            Gizmos.color = cp.IsOccupied
                ? new Color(1f, 0f, 0f, 0.4f)
                : new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawCube(cp.transform.position + Vector3.up * 0.5f,
                            Vector3.one * 0.25f);
        }
    }
}
