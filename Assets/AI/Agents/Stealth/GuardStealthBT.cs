using System.Collections.Generic;
using UnityEngine;

public class GuardStealthBT : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;

    [Header("Detección")]
    public float detectionRange = 6f;
    public float hiddenDetectionMultiplier = 0.35f;
    public float attackRange = 1.5f;

    [Header("Movimiento")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float investigateSpeed = 3f;
    public float nodeReachDistance = 0.2f;
    public float pathRecalcInterval = 0.5f;

    [Header("Alerta")]
    public float alertCooldown = 3f;
    public float alertInvestigationTime = 10f;

    Blackboard _blackboard;
    VisionSensor _visionSensor;

    BTNode _tree;
    int _patrolIndex;

    List<Node> _currentPath;
    int _pathIndex;
    float _nextPathRequestTime;
    Vector3 _lastPathTarget;

    float _nextAlertTime;
    float _alertExpireTime;

    void OnEnable()
    {
        EventBus.Suscribir(EventBus.EnemigoDetectado, OnAlertReceived);
    }

    void OnDisable()
    {
        EventBus.Desuscribir(EventBus.EnemigoDetectado, OnAlertReceived);
    }

    void Start()
    {
        _blackboard = new Blackboard();
        _visionSensor = new VisionSensor(transform, player, detectionRange, _blackboard, hiddenDetectionMultiplier);
        BuildTree();
    }

    void Update()
    {
        if (player == null) return;

        _visionSensor.Sense();
        UpdateClueState();
        _tree?.Tick();
    }

    void BuildTree()
    {
        _tree = new Selector(
            new Sequence(
                new Condition(CanSeePlayer, "VeoJugador?"),
                new Condition(EstaCerca, "EstaCerca?"),
                new BTAction(Attack, "Atacar")
            ) { Name = "Atacar si cerca" },
            new Sequence(
                new Condition(CanSeePlayer, "VeoJugador?"),
                new BTAction(Chase, "Perseguir")
            ) { Name = "Perseguir si veo" },
            new Sequence(
                new Condition(HasClue, "TengoPista?"),
                new BTAction(Investigate, "Investigar")
            ) { Name = "Investigar pista" },
            new Sequence(
                new BTAction(Patrol, "Patrullar")
            ) { Name = "Patrullar (fallback)" }
        ) { Name = "Raíz" };
    }

    void UpdateClueState()
    {
        bool canSee = _blackboard.Get<bool>(BB.CanSeePlayer);
        if (canSee)
        {
            Vector3 lastPos = _blackboard.Get<Vector3>(BB.LastKnownPosition);
            _blackboard.Set<bool>(BB.HasClue, true);
            _blackboard.Set<Vector3>(BB.LastKnownPosition, lastPos);
            _alertExpireTime = Time.time + alertInvestigationTime;

            if (Time.time >= _nextAlertTime)
            {
                EventBus.Publicar(new DatosEvento(EventBus.EnemigoDetectado, lastPos, gameObject));
                _nextAlertTime = Time.time + alertCooldown;
            }
        }
        else if (Time.time >= _alertExpireTime)
        {
            _blackboard.Set<bool>(BB.HasClue, false);
        }
    }

    void OnAlertReceived(DatosEvento evt)
    {
        if (evt.emisor == gameObject) return;
        if (player == null) return;

        _blackboard.Set<bool>(BB.HasClue, true);
        _blackboard.Set<Vector3>(BB.LastKnownPosition, evt.posicion);
        _alertExpireTime = Time.time + alertInvestigationTime;
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;
        return _blackboard.Get<bool>(BB.CanSeePlayer);
    }

    bool EstaCerca()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) < attackRange;
    }

    bool HasClue()
    {
        return _blackboard.Get<bool>(BB.HasClue);
    }

    NodeStatus Attack()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = Color.black;

        if (player != null)
            transform.LookAt(player);

        return NodeStatus.Running;
    }

    NodeStatus Chase()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = Color.yellow;

        Vector3 target = player.position;
        MoveAlongPath(target, chaseSpeed);
        return NodeStatus.Running;
    }

    NodeStatus Investigate()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = Color.red;

        if (!_blackboard.Has(BB.LastKnownPosition))
            return NodeStatus.Failure;

        Vector3 target = _blackboard.Get<Vector3>(BB.LastKnownPosition);
        MoveAlongPath(target, investigateSpeed);

        if (Vector3.Distance(transform.position, target) < 0.5f)
        {
            _blackboard.Set<bool>(BB.HasClue, false);
            return NodeStatus.Success;
        }

        return NodeStatus.Running;
    }

    NodeStatus Patrol()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = Color.cyan;

        if (patrolPoints == null || patrolPoints.Length == 0)
            return NodeStatus.Running;

        Transform destination = patrolPoints[_patrolIndex];
        if (destination == null) return NodeStatus.Running;

        MoveAlongPath(destination.position, patrolSpeed);
        transform.LookAt(destination.position);

        if (Vector3.Distance(transform.position, destination.position) < 0.5f)
            _patrolIndex = (_patrolIndex + 1) % patrolPoints.Length;

        return NodeStatus.Running;
    }

    bool MoveAlongPath(Vector3 destination, float speed)
    {
        if (AStarPathfinder.instance == null)
        {
            MoveDirect(destination, speed);
            return false;
        }

        bool needRequest = _currentPath == null
            || _currentPath.Count == 0
            || Time.time >= _nextPathRequestTime
            || Vector3.Distance(destination, _lastPathTarget) > 1f;

        if (needRequest)
            RequestPath(destination);

        if (_currentPath == null || _currentPath.Count == 0 || _pathIndex >= _currentPath.Count)
        {
            MoveDirect(destination, speed);
            return false;
        }

        Node node = _currentPath[_pathIndex];
        Vector3 targetNode = new Vector3(node.worldPosition.x, transform.position.y, node.worldPosition.z);
        transform.position = Vector3.MoveTowards(transform.position, targetNode, speed * Time.deltaTime);
        transform.LookAt(targetNode);

        if (Vector3.Distance(transform.position, targetNode) < nodeReachDistance)
            _pathIndex++;

        return true;
    }

    void RequestPath(Vector3 destination)
    {
        _lastPathTarget = destination;
        _currentPath = AStarPathfinder.instance.FindPath(transform.position, destination);
        _pathIndex = 0;
        _nextPathRequestTime = Time.time + pathRecalcInterval;
    }

    void MoveDirect(Vector3 destination, float speed)
    {
        Vector3 dir = (destination - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.magenta;
        if (_blackboard != null && _blackboard.Has(BB.LastKnownPosition))
        {
            Vector3 lastPos = _blackboard.Get<Vector3>(BB.LastKnownPosition);
            Gizmos.DrawSphere(lastPos, 0.2f);
            Gizmos.DrawLine(transform.position, lastPos);
        }
    }
}
