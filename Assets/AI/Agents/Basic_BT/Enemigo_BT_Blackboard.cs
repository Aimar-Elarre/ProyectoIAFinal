// ============================================================
//  EJERCICIO EXTRA: BT + BLACKBOARD — Árbol de comportamiento con pizarra
// ============================================================
//
//  Hasta ahora hemos visto dos patrones por separado:
//
//    · Behavior Tree (Enemigo_BT):
//        Usa nodos Selector/Sequence/Condition/BTAction para decidir.
//        Las condiciones comprueban el mundo DIRECTAMENTE
//        (p. ej. Vector3.Distance al jugador).
//
//    · Blackboard (Enemigo_Blackboard):
//        Los sensores escriben en la pizarra; la decisión se toma
//        con un switch, igual que en los scripts monolíticos.
//
//  Este ejercicio une ambos mundos:
//
//    Mundo → [Sensores] → Pizarra → [Árbol BT] → Acciones
//
//  Las CONDICIONES del árbol leen de la pizarra (no perciben el mundo).
//  Las ACCIONES también leen de la pizarra para saber adónde moverse.
//  Los SENSORES son los únicos que perciben el mundo directamente.
//
// ============================================================
//  ESTRUCTURA DEL ÁRBOL (igual que Enemigo_BT, pero usando la pizarra)
// ============================================================
//
//    Selector  ────────────────────── evalúa en orden (prioridad ↓)
//    ├── Sequence  "si vida baja → huir"
//    │   ├── Condition: LowHealth         (lee BB.LowHealth de la pizarra)
//    │   └── BTAction:  Flee
//    ├── Sequence  "si veo al jugador → perseguir"
//    │   ├── Condition: CanSeePlayer      (lee BB.CanSeePlayer de la pizarra)
//    │   └── BTAction:  Chase             (lee BB.LastKnownPosition)
//    ├── Sequence  "si tengo pista → investigar"
//    │   ├── Condition: HasClue           (lee BB.HasClue de la pizarra)
//    │   └── BTAction:  Investigate       (lee BB.LastKnownPosition)
//    └── BTAction: Patrol   (fallback: siempre devuelve Running)
//
//  Compara HasLastKnownPosition() de Enemigo_BT con HasClue() aquí:
//    Antes:  bool HasLastKnownPosition() => _hasLastKnownPos;  (variable local)
//    Ahora:  bool HasClue()              => _blackboard.Get<bool>(BB.HasClue);
//
// ============================================================
//  PARTES DEL EJERCICIO
// ============================================================
//
//  [PARTE 1 — OBLIGATORIO]
//    Implementa BuildTree() ensamblando las condiciones y acciones
//    ya escritas con los nodos Selector y Sequence.
//    Misma tarea que en Enemigo_BT, pero ahora las condiciones leen
//    de la pizarra en lugar de acceder directamente al mundo.
//
//  [PARTE 2 — AMPLIACIÓN]
//    Añade el comportamiento ATACAR cuando el jugador está muy cerca.
//    a) Implementa EstaCerca(): debe leer la distancia sin tocar "player";
//       puedes usar BB.LastKnownPosition o añadir una clave nueva al sensor.
//    b) Implementa Attack().
//    c) Añade la rama al árbol con la prioridad correcta.
//
//  [PARTE 3 — BONUS]
//    Integra SoundSensor: ya está declarado e inicializado.
//    a) Añade una Condition que lea BB.HeardNoise de la pizarra.
//    b) Añade una BTAction que mueva al enemigo hacia BB.NoisePosition.
//    c) Añade la rama al árbol con la prioridad adecuada.
//    Reflexión: ¿con qué prioridad debería ir frente a Patrol e Investigate?
//
// ============================================================

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

    // ── Pizarra y sensores ────────────────────────────────────────────────
    Blackboard   _blackboard;
    VisionSensor _visionSensor;
    HealthSensor _healthSensor;
    SoundSensor  _soundSensor;   // Parte 3

    // ── Árbol BT ─────────────────────────────────────────────────────────
    BTNode _tree;
    int    _waypointIndex;

    // ── Ciclo de vida ─────────────────────────────────────────────────────

    void Start()
    {
        _blackboard = new Blackboard();

        _visionSensor = new VisionSensor(transform, player, detectionRange, _blackboard);
        _healthSensor = new HealthSensor(() => health, maxHealth, _blackboard);
        _soundSensor  = new SoundSensor(transform, player, hearingRange, _blackboard);

        BuildTree();
    }

    void Update()
    {
        // 1. Los sensores perciben el mundo y actualizan la pizarra.
        _visionSensor.Sense();
        _healthSensor.Sense();
        _soundSensor.Sense();   // Parte 3

        SimulateDamage();
        Regenerate();

        // 2. El árbol lee la pizarra y decide qué acción ejecutar.
        _tree?.Tick();
    }

    // ── Construcción del árbol ────────────────────────────────────────────

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

    // ── Condiciones (leen de la pizarra, no del mundo) ────────────────────

    // Ejemplo completo para que veas el patrón:
    bool LowHealth() => _blackboard.Get<bool>(BB.LowHealth);

    // TODO [PARTE 1]: Implementa las siguientes condiciones.
    // Cada una debe leer una clave de la pizarra, igual que LowHealth().
    // Consulta BlackboardKeys.cs para ver las claves disponibles.

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

    // ── Acciones (leen de la pizarra para saber adónde moverse) ──────────

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

        // A diferencia de Enemigo_BT (que accede a jugador.position directamente),
        // aquí leemos la posición de la pizarra.
        // VisionSensor la actualiza cada frame mientras el jugador es visible.
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

        return NodeStatus.Running;
    }

    // ── Utilidades ────────────────────────────────────────────────────────

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
