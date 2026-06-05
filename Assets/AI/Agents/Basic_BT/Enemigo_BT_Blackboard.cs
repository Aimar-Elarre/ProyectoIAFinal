
using UnityEngine;

public class Enemigo_BT_Blackboard : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;

    [Header("Salud")]
    public float health    = 100f;
    public float maxHealth = 100f;

    [Header("Detección visual")]
    public float detectionRange = 5f;

    [Header("Escucha (Parte 3)")]
    public float hearingRange = 8f;

    [Header("Patrulla")]
    public Transform[] waypoints;
    public float patrolSpeed      = 2f;
    public float waypointDistance = 0.5f;

    [Header("Persecución")]
    public float chaseSpeed = 4f;

    [Header("Investigación")]
    public float investigateSpeed       = 3f;
    public float investigationDistance  = 0.5f;

    [Header("Huida")]
    public float fleeSpeed = 7f;

    [Header("Ataque (Parte 2)")]
    public float attackRange = 1.5f;

    [Header("Game Over")]
    public GameObject uiToActivate;
    public bool ensureDisabledAtStart = true;
    public bool pauseAudio = true;

    bool _gameOverTriggered;

    Blackboard   _blackboard;
    VisionSensor _visionSensor;
    HealthSensor _healthSensor;
    SoundSensor  _soundSensor;

    BTNode _tree;
    int    _waypointIndex;

    void Start()
    {
        _blackboard = new Blackboard();

        if (ensureDisabledAtStart && uiToActivate != null)
            uiToActivate.SetActive(false);

        _visionSensor = new VisionSensor(transform, player, detectionRange, _blackboard);
        _healthSensor = new HealthSensor(() => health, maxHealth, _blackboard);
        _soundSensor  = new SoundSensor(transform, player, hearingRange, _blackboard);

        BuildTree();
    }

    void Update()
    {
        _visionSensor.Sense();
        _healthSensor.Sense();
        _soundSensor.Sense();   // Parte 3

        SimulateDamage();
        Regenerate();

        _tree?.Tick();
    }

    void BuildTree()
    {
        _tree = new Selector(
            new Sequence(
                new Condition(LowHealth,    "VidaBaja?"),
                new BTAction(Flee,          "Huir")
            ) { Name = "Huir si vida baja" },
            new Sequence(
                new Condition(EstaCerca,    "EstaCerca?"),
                new BTAction(Attack,        "Atacar")
            ) { Name = "Atacar si cerca" },
            new Sequence(
                new Condition(CanSeePlayer, "VeoJugador?"),
                new BTAction(Chase,         "Perseguir")
            ) { Name = "Perseguir si veo" },
            new Sequence(
                new Condition(HasClue,           "TengoPista?"),
                new BTAction(Investigate,       "Investigar")
            ) { Name = "Investigar pista" },
            new Sequence(
                new BTAction(Patrol,            "Patrullar")
            ) { Name = "Patrullar (fallback)" }
        ) { Name = "Raíz (Selector)" };
    }

    bool LowHealth() => _blackboard.Get<bool>(BB.LowHealth);

    bool CanSeePlayer()
    {
        return _blackboard.Get<bool>(BB.CanSeePlayer);
    }

    bool HasClue()
    {
        return _blackboard.Get<bool>(BB.HasClue);
    }

    bool EstaCerca()
    {
        if (!_blackboard.Has(BB.LastKnownPosition))
            return false;

        Vector3 lastPos = _blackboard.Get<Vector3>(BB.LastKnownPosition);
        return Vector3.Distance(transform.position, lastPos) < attackRange;
    }


    NodeStatus Flee()
    {
        GetComponent<Renderer>().material.color = Color.magenta;
        if (player == null) return NodeStatus.Running;

        Vector3 dir = (transform.position - player.position).normalized;
        transform.position += dir * (fleeSpeed * Time.deltaTime);
        transform.LookAt(transform.position + dir);

        return NodeStatus.Running;
    }

    NodeStatus Chase()
    {
        GetComponent<Renderer>().material.color = Color.yellow;

        Vector3 targetPos = _blackboard.Get<Vector3>(BB.LastKnownPosition);

        MoveTo(targetPos, chaseSpeed);
        transform.LookAt(targetPos);

        return NodeStatus.Running;
    }

    NodeStatus Investigate()
    {
        GetComponent<Renderer>().material.color = Color.red;

        Vector3 lastPos = _blackboard.Get<Vector3>(BB.LastKnownPosition);

        MoveTo(lastPos, investigateSpeed);
        transform.LookAt(lastPos);

        if (Vector3.Distance(transform.position, lastPos) < investigationDistance)
        {
            _blackboard.Remove(BB.HasClue);
            Debug.Log("[BT+BB] Investigación sin resultado.");
            return NodeStatus.Success;
        }

        return NodeStatus.Running;
    }

    NodeStatus Patrol()
    {
        GetComponent<Renderer>().material.color = Color.cyan;

        if (waypoints == null || waypoints.Length == 0)
            return NodeStatus.Running;

        Transform destino = waypoints[_waypointIndex];
        MoveTo(destino.position, patrolSpeed);
        transform.LookAt(destino);

        if (Vector3.Distance(transform.position, destino.position) < waypointDistance)
            _waypointIndex = (_waypointIndex + 1) % waypoints.Length;

        return NodeStatus.Running;
    }

    NodeStatus Attack()
    {
        GetComponent<Renderer>().material.color = Color.black;

        if (_blackboard.Has(BB.LastKnownPosition))
        {
            Vector3 targetPos = _blackboard.Get<Vector3>(BB.LastKnownPosition);
            transform.LookAt(targetPos);
        }

        TriggerGameOver();
        return NodeStatus.Running;
    }

    void TriggerGameOver()
    {
        if (_gameOverTriggered)
            return;

        _gameOverTriggered = true;

        if (uiToActivate != null)
            uiToActivate.SetActive(true);

        Time.timeScale = 0f;

        if (pauseAudio)
            AudioListener.pause = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        try
        {
            EventBus.Publicar(new DatosEvento("PausaJuego", transform.position, gameObject, true));
        }
        catch
        {
        }
    }


    void MoveTo(Vector3 destination, float speed)
    {
        Vector3 dir = (destination - transform.position).normalized;
        transform.position += dir * (speed * Time.deltaTime);
    }

    void SimulateDamage()
    {
        if (UnityEngine.InputSystem.Keyboard.current.qKey.isPressed)
            health -= 1f;
        health = Mathf.Max(health, 0f);
    }

    void Regenerate()
    {
        if (health < maxHealth)
            health += 5f * Time.deltaTime;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, hearingRange);

        if (_blackboard != null && _blackboard.Has(BB.LastKnownPosition))
        {
            Gizmos.color = Color.red;
            Vector3 p = _blackboard.Get<Vector3>(BB.LastKnownPosition);
            Gizmos.DrawSphere(p, 0.3f);
            Gizmos.DrawLine(transform.position, p);
        }

        if (waypoints == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.2f);
            Gizmos.DrawLine(waypoints[i].position, waypoints[(i + 1) % waypoints.Length].position);
        }
    }
}
