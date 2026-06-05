
using UnityEngine;

public class TacticalPoint : MonoBehaviour
{
    public enum PointType { Flank, Elevated, Support, Ambush }

    [Header("Configuración")]
    public PointType type = PointType.Flank;
    [Tooltip("Peso de preferencia al elegir entre varios puntos del mismo tipo.")]
    public float weight = 1f;

    public bool IsOccupied { get; private set; }
    public MonoBehaviour Occupant { get; private set; }

    public void Occupy(MonoBehaviour occupant) { IsOccupied = true;  Occupant = occupant; }
    public void Vacate()                        { IsOccupied = false; Occupant = null; }

    public bool IsGoodFlankFor(Vector3 agentPos, Vector3 targetPos)
    {
        Vector3 agentToTarget = (targetPos - agentPos).normalized;
        Vector3 agentToPoint  = (transform.position - agentPos).normalized;
        float dot = Vector3.Dot(agentToTarget, agentToPoint);
        return dot is > -0.6f and < 0.6f; // ± 53° respecto al eje principal
    }

    public float ScoreFor(Vector3 agentPos)
    {
        float dist = Vector3.Distance(agentPos, transform.position);
        return weight / (1f + dist * 0.2f);
    }

    void OnDrawGizmos()
    {
        Color color = type switch
        {
            PointType.Flank    => Color.blue,
            PointType.Elevated => Color.yellow,
            PointType.Support  => Color.green,
            PointType.Ambush   => Color.red,
            _                  => Color.white
        };

        Gizmos.color = IsOccupied ? new Color(0.5f, 0.5f, 0.5f) : color;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.3f,
            $"{type}{(IsOccupied ? " [X]" : "")}");
#endif
    }
}
