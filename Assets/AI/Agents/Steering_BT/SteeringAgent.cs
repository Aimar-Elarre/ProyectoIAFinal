
using UnityEngine;

public class SteeringAgent : MonoBehaviour
{
    public enum SteeringMode { Seek, Flee, Arrive }

    [Header("Target")]
    public Transform target;

    [Header("Behavior")]
    public SteeringMode mode = SteeringMode.Arrive;

    [Header("Velocidad")]
    public float maxSpeed = 4f;
    [Tooltip("Qué tan rápido cambia la velocidad actual hacia la deseada.")]
    public float steeringForce = 8f;

    [Header("Arrive")]
    [Tooltip("Radio exterior: el agente empieza a frenar dentro de este radio.")]
    public float slowRadius = 3f;
    [Tooltip("Radio interior: el agente se detiene dentro de este radio.")]
    public float targetRadius = 0.2f;

    Vector3 _velocity;
    public Vector3 Velocity => _velocity;

    void Update()
    {
        if (target == null) return;

        Vector3 desiredVelocity = ComputeDesiredVelocity();

        _velocity = Vector3.MoveTowards(_velocity, desiredVelocity, steeringForce * Time.deltaTime);

        transform.position += _velocity * Time.deltaTime;

        if (_velocity.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Lerp(
                transform.forward, _velocity.normalized, 10f * Time.deltaTime);
    }

    Vector3 ComputeDesiredVelocity() => mode switch
    {
        SteeringMode.Seek   => SteeringBehaviors.Seek(transform.position, target.position, maxSpeed),
        SteeringMode.Flee   => SteeringBehaviors.Flee(transform.position, target.position, maxSpeed),
        SteeringMode.Arrive => SteeringBehaviors.Arrive(transform.position, target.position,
                                                         maxSpeed, slowRadius, targetRadius),
        _ => Vector3.zero
    };

    void OnDrawGizmos()
    {
        if (target == null) return;

        if (mode == SteeringMode.Arrive)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            Gizmos.DrawWireSphere(target.position, slowRadius);
            Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
            Gizmos.DrawWireSphere(target.position, targetRadius);
        }

        if (Application.isPlaying && _velocity.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + _velocity);
        }
    }
}
