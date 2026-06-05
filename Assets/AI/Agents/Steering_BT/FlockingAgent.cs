
using System.Collections.Generic;
using UnityEngine;

public class FlockingAgent : MonoBehaviour
{
    [Header("Radios")]
    public float neighborRadius = 3f;
    [Tooltip("Distancia mínima entre agentes antes de aplicar separación.")]
    public float separationRadius = 1.2f;

    [Header("Velocidad")]
    public float maxSpeed = 4f;
    public float steeringForce = 6f;

    [Header("Pesos de flocking")]
    public float cohesionWeight = 1f;
    public float separationWeight = 1.8f;
    public float alignmentWeight = 1f;

    [Header("Líder (Parte 2)")]
    public Transform leaderTarget;
    public float leaderWeight = 1.2f;

    Vector3 _velocity;
    public Vector3 Velocity => _velocity;

    static FlockingAgent[] _allAgents;

    void Start()
    {
        _velocity = Random.insideUnitSphere.normalized * maxSpeed * 0.5f;
        _velocity.y = 0f;
        _allAgents = null; // fuerza re-escaneo si se añaden boids en runtime
    }

    void Update()
    {
        var neighbors = FindNeighbors();

        Vector3 cohesion   = ComputeCohesion(neighbors)   * cohesionWeight;
        Vector3 separation = ComputeSeparation(neighbors)  * separationWeight;
        Vector3 alignment  = ComputeAlignment(neighbors)   * alignmentWeight;

        Vector3 steering = cohesion + separation + alignment;

        if (leaderTarget != null)
        {
            Vector3 toLeader = SteeringBehaviors.Arrive(
                transform.position, leaderTarget.position,
                maxSpeed, slowRadius: 5f, targetRadius: 1f);
            steering += toLeader * leaderWeight;
        }

        _velocity += steering * Time.deltaTime;
        _velocity.y = 0f;
        _velocity = Vector3.ClampMagnitude(_velocity, maxSpeed);

        transform.position += _velocity * Time.deltaTime;

        if (_velocity.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Lerp(
                transform.forward, _velocity.normalized, 10f * Time.deltaTime);
    }


    List<FlockingAgent> FindNeighbors()
    {
        if (_allAgents == null || _allAgents.Length == 0)
            _allAgents = FindObjectsByType<FlockingAgent>(FindObjectsSortMode.None);

        var neighbors = new List<FlockingAgent>();
        foreach (var agent in _allAgents)
        {
            if (agent == this) continue;
            if (Vector3.Distance(transform.position, agent.transform.position) < neighborRadius)
                neighbors.Add(agent);
        }
        return neighbors;
    }


    Vector3 ComputeCohesion(List<FlockingAgent> neighbors)
    {
        if (neighbors.Count == 0) return Vector3.zero;

        Vector3 centerOfMass = Vector3.zero;
        foreach (var n in neighbors)
            centerOfMass += n.transform.position;
        centerOfMass /= neighbors.Count;

        return (centerOfMass - transform.position).normalized;
    }

    Vector3 ComputeSeparation(List<FlockingAgent> neighbors)
    {
        Vector3 steer = Vector3.zero;
        int count = 0;

        foreach (var n in neighbors)
        {
            float dist = Vector3.Distance(transform.position, n.transform.position);
            if (dist < separationRadius && dist > 0.001f)
            {
                steer += (transform.position - n.transform.position).normalized / dist;
                count++;
            }
        }

        return count > 0 ? (steer / count).normalized : Vector3.zero;
    }

    Vector3 ComputeAlignment(List<FlockingAgent> neighbors)
    {
        if (neighbors.Count == 0) return transform.forward;

        Vector3 avgVelocity = Vector3.zero;
        foreach (var n in neighbors)
            avgVelocity += n.Velocity;
        avgVelocity /= neighbors.Count;

        return avgVelocity.sqrMagnitude > 0.001f ? avgVelocity.normalized : transform.forward;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.12f);
        Gizmos.DrawWireSphere(transform.position, neighborRadius);

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        if (Application.isPlaying && _velocity.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + _velocity.normalized * 1.2f);
        }
    }
}
