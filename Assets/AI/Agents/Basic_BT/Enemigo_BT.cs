
using UnityEngine;

public class Enemigo_BT : MonoBehaviour
{
    [Header("Referencias")]
    public Transform jugador;

    [Header("Salud")]
    public float vida = 100f;
    public float vidaMaxima = 100f;

    [Header("Detección")]
    public float rangoDeteccion = 5f;

    [Header("Patrulla")]
    public Transform[] waypoints;
    public float velocidadPatrulla = 2f;
    public float distanciaWaypoint = 0.5f;

    [Header("Persecución")]
    public float velocidadPersecucion = 4f;

    [Header("Investigación")]
    public float velocidadInvestigacion = 3f;
    public float distanciaInvestigacion = 0.5f;

    [Header("Huida")]
    public float velocidadHuida = 7f;

    [Header("Ataque")]
    public float rangoAtaque = 1.5f;

    [Header("Game Over")]
    public GameObject uiToActivate;
    public bool ensureDisabledAtStart = true;
    public bool pauseAudio = true;

    bool _gameOverTriggered;

    Vector3 _lastKnownPos;
    bool _hasLastKnownPos;
    int _waypointIndex;

    BTNode _tree;
    public BTNode Tree => _tree;


    void Start()
    {
        if (ensureDisabledAtStart && uiToActivate != null)
            uiToActivate.SetActive(false);

        BuildTree();
    }

    void BuildTree()
    {

        _tree = new Selector(
            new Sequence(
                new Condition(LowHealth,             "VidaBaja?"),
                new BTAction(Flee,                   "Huir")
            ) { Name = "Huir si vida baja" },
            new Sequence(
                new Condition(EstaCerca,             "EstaCerca?"),
                new BTAction(Attack,                 "Atacar")
            ) { Name = "Atacar si cerca" },
            new Sequence(
                new Condition(CanSeePlayer,          "VeoJugador?"),
                new BTAction(Chase,                  "Perseguir")
            ) { Name = "Perseguir si veo" },
            new Sequence(
                new Condition(HasLastKnownPosition,  "TengoPista?"),
                new BTAction(Investigate,            "Investigar")
            ) { Name = "Investigar pista" },
            new Sequence(
                new BTAction(Patrol,                 "Patrullar")
            ) { Name = "Patrullar (fallback)" }
        ) { Name = "Raíz (Selector)" };

    }


    void Update()
    {
        if (Time.timeScale == 0f) return;
        _tree?.Tick();
        SimulateDamage();
        Regenerate();
    }


    bool LowHealth() => vida < vidaMaxima * 0.5f;

    bool CanSeePlayer()
    {
        if (jugador == null) return false;
        return Vector3.Distance(transform.position, jugador.position) < rangoDeteccion;
    }

    bool HasLastKnownPosition() => _hasLastKnownPos;

    bool EstaCerca()
    {
        if (jugador == null) return false;
        return Vector3.Distance(transform.position, jugador.position) < rangoAtaque;
    }


    NodeStatus Flee()
    {
        GetComponent<Renderer>().material.color = Color.magenta;

        _hasLastKnownPos = false;

        Vector3 dir = (transform.position - jugador.position).normalized;
        transform.position += dir * (velocidadHuida * Time.deltaTime);
        transform.LookAt(transform.position + dir);

        return NodeStatus.Running;
    }

    NodeStatus Chase()
    {
        GetComponent<Renderer>().material.color = Color.yellow;

        _lastKnownPos = jugador.position;
        _hasLastKnownPos = true;

        Vector3 dir = (jugador.position - transform.position).normalized;
        transform.position += dir * (velocidadPersecucion * Time.deltaTime);
        transform.LookAt(jugador);

        return NodeStatus.Running;
    }

    NodeStatus Investigate()
    {
        GetComponent<Renderer>().material.color = Color.red;

        Vector3 dir = (_lastKnownPos - transform.position).normalized;
        transform.position += dir * (velocidadInvestigacion * Time.deltaTime);
        transform.LookAt(_lastKnownPos);

        if (Vector3.Distance(transform.position, _lastKnownPos) < distanciaInvestigacion)
        {
            _hasLastKnownPos = false;
            Debug.Log("[BT] Investigación sin resultado.");
            return NodeStatus.Success; // Success → el Selector pasa a Patrol
        }

        return NodeStatus.Running;
    }

    NodeStatus Patrol()
    {
        GetComponent<Renderer>().material.color = Color.cyan;

        if (waypoints == null || waypoints.Length == 0)
            return NodeStatus.Running;

        Transform destino = waypoints[_waypointIndex];
        Vector3 dir = (destino.position - transform.position).normalized;
        transform.position += dir * (velocidadPatrulla * Time.deltaTime);
        transform.LookAt(destino);

        if (Vector3.Distance(transform.position, destino.position) < distanciaWaypoint)
            _waypointIndex = (_waypointIndex + 1) % waypoints.Length;

        return NodeStatus.Running;
    }

    NodeStatus Attack()
    {
        GetComponent<Renderer>().material.color = Color.black;

        _lastKnownPos = jugador.position;
        _hasLastKnownPos = true;

        transform.LookAt(jugador);

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


    void SimulateDamage()
    {
        if (UnityEngine.InputSystem.Keyboard.current.qKey.isPressed)
            vida -= 1f;
        vida = Mathf.Max(vida, 0f);
    }

    void Regenerate()
    {
        if (vida < vidaMaxima)
            vida += 5f * Time.deltaTime;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);

        if (_hasLastKnownPos)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_lastKnownPos, 0.3f);
            Gizmos.DrawLine(transform.position, _lastKnownPos);
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
