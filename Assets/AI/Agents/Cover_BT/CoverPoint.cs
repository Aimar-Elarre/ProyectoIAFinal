
using UnityEngine;

[SelectionBase]
public class CoverPoint : MonoBehaviour
{
    public bool IsOccupied { get; private set; }
    public MonoBehaviour Occupant { get; private set; }

    public void Occupy(MonoBehaviour occupant)
    {
        IsOccupied = true;
        Occupant = occupant;
    }

    public void Vacate()
    {
        IsOccupied = false;
        Occupant = null;
    }

    public bool ProtectsFrom(Vector3 threatPos)
    {
        Vector3 toThreat = (threatPos - transform.position).normalized;
        return Vector3.Dot(toThreat, transform.forward) < 0.2f;
    }

    public float ScoreFor(Vector3 agentPos, Vector3 threatPos)
    {
        if (!ProtectsFrom(threatPos)) return float.MinValue;
        float distToAgent  = Vector3.Distance(agentPos, transform.position);
        float distToThreat = Vector3.Distance(transform.position, threatPos);
        return distToThreat - distToAgent * 1.5f;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = IsOccupied ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.7f);
        Gizmos.DrawWireSphere(transform.position + transform.forward * 0.7f, 0.08f);
    }
}
