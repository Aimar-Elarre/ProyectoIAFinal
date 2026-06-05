
using UnityEngine;

public class Enemigo_Cover_BT : MonoBehaviour
{
    [Header("Referencias")]
    public Transform jugador;

    [Header("Salud")]
    public float vida = 100f;
    public float vidaMaxima = 100f;

    [Header("Thresholds")]
    public float vidaCritica = 0.25f;     // < 25% → busca cover urgente
    public float vidaBajoFuego = 0.75f;   // < 75% + jugador visible → busca cover táctico

    [Header("Detección")]
    public float rangoDeteccion = 7f;

    [Header("Patrulla")]
    public Transform[] waypoints;
    public float velocidadPatrulla = 2f;
    public float distanciaWaypoint = 0.5f;

    [Header("Movimiento")]
    public float maxSpeed = 4f;
    public float steeringForce = 8f;
    public float slowRadius = 2f;
    public float arrivalDistance = 0.4f;

    BTNode _tree;
    Vector3 _velocity;
    int _waypointIndex;

    CoverPoint _currentCover;
    TacticalPoint _currentFlank;
    bool _inCover;

    enum PeekState { Peek, Shoot, Duck }
    PeekState _peekState = PeekState.Peek;
    float _peekTimer;

    static TacticalPoint[] _tacticalPoints;

    public BTNode Tree => _tree;

    void Start()
    {
        BuildTree();
        _tacticalPoints = FindObjectsByType<TacticalPoint>(FindObjectsSortMode.None);
    }

    void BuildTree()
    {
        _tree = new Selector(
            new ConditionalSequence(
                new Condition(CriticalHealth,       "VidaCrítica?"),
                new BTAction(SeekNearestCover,      "Cover-Urgente")
            ) { Name = "Cover urgente [Self Abort]" },

            new ConditionalSequence(
                new Condition(UnderFire,            "BajoFuego?"),
                new BTAction(SeekTacticalCover,     "Cover-Táctico")
            ) { Name = "Cover táctico [Self Abort]" },

            new ConditionalSequence(
                new Condition(InCoverAndSeePlayer,  "EnCoverYVeo?"),
                new BTAction(PeekAndShoot,          "Asomar-Disparar")
            ) { Name = "Peek & Shoot [Self Abort]" },

            new ConditionalSequence(
                new Condition(CanSeePlayer,         "VeoJugador?"),
                new BTAction(MoveToFlankPoint,      "Flanquear")
            ) { Name = "Flanqueo [Self Abort]" },


            new BTAction(Patrol, "Patrullar")
            { Name = "Patrullar (fallback)" }
        )
        { Name = "Raíz — Cover + Táctico" };
    }

    void Update()
    {
        _tree?.Tick();
        SimulateDamage();
        Regenerate();

        transform.position += _velocity * Time.deltaTime;
        if (_velocity.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Lerp(
                transform.forward, _velocity.normalized, 10f * Time.deltaTime);
    }


    bool CriticalHealth() => vida < vidaMaxima * vidaCritica;

    bool UnderFire()
    {
        if (!CanSeePlayer()) return false;
        return vida < vidaMaxima * vidaBajoFuego;
    }

    bool CanSeePlayer()
    {
        if (jugador == null) return false;
        return Vector3.Distance(transform.position, jugador.position) < rangoDeteccion;
    }

    bool InCoverAndSeePlayer() => _inCover && CanSeePlayer();


    NodeStatus SeekNearestCover()
    {
        GetComponent<Renderer>().material.color = Color.red;

        if (_currentCover == null || _currentCover.Occupant != this)
        {
            CoverSystem.Instance?.ReleaseCover(this);
            _currentCover = CoverSystem.Instance?.FindNearestCover(transform.position);
            _currentCover?.Occupy(this);
            _inCover = false;
        }

        if (_currentCover == null) return NodeStatus.Failure;

        return MoveTowards(_currentCover.transform.position, Color.red);
    }

    NodeStatus SeekTacticalCover()
    {
        GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0f);

        if (_currentCover == null || _currentCover.Occupant != this)
        {
            CoverSystem.Instance?.ReleaseCover(this);
            _currentCover = CoverSystem.Instance?.FindBestCover(
                transform.position, jugador.position);
            _currentCover?.Occupy(this);
            _inCover = false;
        }

        if (_currentCover == null) return NodeStatus.Failure;

        return MoveTowards(_currentCover.transform.position, new Color(1f, 0.5f, 0f));
    }

    NodeStatus PeekAndShoot()
    {
        if (jugador == null)
            return NodeStatus.Failure;

        switch (_peekState)
        {
            case PeekState.Peek:
                GetComponent<Renderer>().material.color = new Color(1f, 0.8f, 0.2f);
                _peekTimer += Time.deltaTime;
                transform.LookAt(jugador);
                if (_peekTimer >= 1f)
                {
                    _peekState = PeekState.Shoot;
                    _peekTimer = 0f;
                }
                break;

            case PeekState.Shoot:
                GetComponent<Renderer>().material.color = Color.red;
                Debug.DrawLine(transform.position, jugador.position, Color.red);
                _peekTimer += Time.deltaTime;
                if (_peekTimer >= 0.5f)
                {
                    _peekState = PeekState.Duck;
                    _peekTimer = 0f;
                }
                break;

            case PeekState.Duck:
                GetComponent<Renderer>().material.color = Color.gray;
                _peekTimer += Time.deltaTime;
                if (_peekTimer >= 1.5f)
                {
                    _peekState = PeekState.Peek;
                    _peekTimer = 0f;
                    _inCover = true;
                }
                break;
        }

        return NodeStatus.Running;
    }

    NodeStatus MoveToFlankPoint()
    {
        GetComponent<Renderer>().material.color = Color.blue;

        if (_currentFlank == null || _currentFlank.Occupant != this)
        {
            _currentFlank?.Vacate();
            _currentFlank = FindBestFlankPoint();
            _currentFlank?.Occupy(this);
        }

        if (_currentFlank == null) return NodeStatus.Failure;

        return MoveTowards(_currentFlank.transform.position, Color.blue);
    }

    NodeStatus Patrol()
    {
        GetComponent<Renderer>().material.color = Color.cyan;
        _inCover = false;
        CoverSystem.Instance?.ReleaseCover(this);
        _currentCover = null;

        if (waypoints == null || waypoints.Length == 0) return NodeStatus.Running;

        Transform destino = waypoints[_waypointIndex];
        Vector3 desired = SteeringBehaviors.Arrive(
            transform.position, destino.position,
            velocidadPatrulla, slowRadius: 1f, targetRadius: distanciaWaypoint);
        _velocity = Vector3.MoveTowards(_velocity, desired, steeringForce * Time.deltaTime);

        if (Vector3.Distance(transform.position, destino.position) < distanciaWaypoint)
            _waypointIndex = (_waypointIndex + 1) % waypoints.Length;

        return NodeStatus.Running;
    }


    NodeStatus MoveTowards(Vector3 destination, Color debugColor)
    {
        GetComponent<Renderer>().material.color = debugColor;

        Vector3 desired = SteeringBehaviors.Arrive(
            transform.position, destination, maxSpeed, slowRadius, arrivalDistance);
        _velocity = Vector3.MoveTowards(_velocity, desired, steeringForce * Time.deltaTime);

        float dist = Vector3.Distance(transform.position, destination);
        if (dist < arrivalDistance)
        {
            _velocity = Vector3.zero;
            _inCover = true;
            return NodeStatus.Success;
        }
        return NodeStatus.Running;
    }

    TacticalPoint FindBestFlankPoint()
    {
        if (_tacticalPoints == null) return null;

        TacticalPoint best = null;
        float bestScore = float.MinValue;

        foreach (var tp in _tacticalPoints)
        {
            if (tp.IsOccupied) continue;
            if (tp.type != TacticalPoint.PointType.Flank) continue;
            if (!tp.IsGoodFlankFor(transform.position, jugador.position)) continue;

            float score = tp.ScoreFor(transform.position);
            if (score > bestScore)
            {
                bestScore = score;
                best = tp;
            }
        }

        return best;
    }

    void SimulateDamage()
    {
        if (UnityEngine.InputSystem.Keyboard.current.qKey.isPressed)
            vida -= 5f;
        vida = Mathf.Max(vida, 0f);
    }

    void Regenerate()
    {
        if (vida < vidaMaxima) vida += 2f * Time.deltaTime;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);

        if (_currentCover != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _currentCover.transform.position);
            Gizmos.DrawWireSphere(_currentCover.transform.position, 0.5f);
        }

        if (_currentFlank != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, _currentFlank.transform.position);
        }

        if (Application.isPlaying && _velocity.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + _velocity);
        }

        if (waypoints == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.15f);
            Gizmos.DrawLine(waypoints[i].position,
                            waypoints[(i + 1) % waypoints.Length].position);
        }
    }
}
