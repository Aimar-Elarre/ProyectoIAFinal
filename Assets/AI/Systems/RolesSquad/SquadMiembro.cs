
using System.Collections.Generic;
using UnityEngine;

public class SquadMiembro : MonoBehaviour
{
    public enum Rol { Tank, DPS, Support }

    public const string TankEnganchado = "TankEnganchado";

    [Header("Rol e identidad")]
    public Rol   rol           = Rol.Tank;
    public string nombreAgente = "Miembro";

    [Header("Referencias")]
    public Transform jugador;

    [Header("Salud")]
    public float vida;
    public float vidaMaxima = 100f;

    [Header("Detección")]
    public float rangoDeteccion   = 6f;
    public float rangoEnganche    = 2.5f;   // distancia a la que el Tank "engancha"
    public float rangoCuracion    = 4f;     // radio en el que el Support cura
    public float umbralCuracion   = 0.6f;   // cura a aliados con vida < 60%

    [Header("Movimiento")]
    public float velocidad = 3f;

    [Header("Patrulla")]
    public Transform[] waypoints;
    public float distanciaWaypoint = 0.5f;

    [Header("Support")]
    public float curacionPorSeg = 10f;

    enum EstadoTank    { Patrulla, Avanzar, Aguantar, Retroceder }
    enum EstadoDPS     { Patrulla, EsperarTank, Flanquear, Atacar }
    enum EstadoSupport { Patrulla, SeguirSquad, Curar, Replegarse }

    EstadoTank    _estadoTank    = EstadoTank.Patrulla;
    EstadoDPS     _estadoDPS     = EstadoDPS.Patrulla;
    EstadoSupport _estadoSupport = EstadoSupport.Patrulla;

    int _waypointIndex;
    bool _tankHaEnganchado;

    void Awake() => vida = vidaMaxima;

    void OnEnable()
    {
        EventBus.Suscribir(TankEnganchado,         AlTankEnganchado);
        EventBus.Suscribir(EventBus.BajoFuego,     AlBajoFuego);
        EventBus.Suscribir(EventBus.AliadoCaido,   AlAliadoCaido);
    }

    void OnDisable()
    {
        EventBus.Desuscribir(TankEnganchado,         AlTankEnganchado);
        EventBus.Desuscribir(EventBus.BajoFuego,     AlBajoFuego);
        EventBus.Desuscribir(EventBus.AliadoCaido,   AlAliadoCaido);
    }

    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current.qKey.isPressed)
            vida = Mathf.Max(0f, vida - 10f * Time.deltaTime);

        switch (rol)
        {
            case Rol.Tank:    ActualizarTank();    break;
            case Rol.DPS:     ActualizarDPS();     break;
            case Rol.Support: ActualizarSupport(); break;
        }
    }


    void ActualizarTank()
    {
        bool veJugador = VerJugador();

        switch (_estadoTank)
        {
            case EstadoTank.Patrulla:
                GetComponent<Renderer>().material.color = Color.blue;
                Patrullar();
                if (veJugador) _estadoTank = EstadoTank.Avanzar;
                break;

            case EstadoTank.Avanzar:
                GetComponent<Renderer>().material.color = new Color(0.2f, 0.4f, 1f);
                MoverHacia(jugador.position);
                float dist = Vector3.Distance(transform.position, jugador.position);
                if (dist < rangoEnganche)
                {
                    Debug.Log($"[{nombreAgente}] Tank enganchó al enemigo.");
                    EventBus.Publicar(new DatosEvento(TankEnganchado, jugador.position, gameObject));
                    _estadoTank = EstadoTank.Aguantar;
                }
                if (!veJugador) _estadoTank = EstadoTank.Patrulla;
                break;

            case EstadoTank.Aguantar:
                GetComponent<Renderer>().material.color = Color.red;
                if (vida < vidaMaxima * 0.3f) _estadoTank = EstadoTank.Retroceder;
                if (!veJugador) _estadoTank = EstadoTank.Patrulla;
                break;

            case EstadoTank.Retroceder:
                GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0f);
                if (jugador != null)
                {
                    Vector3 huida = (transform.position - jugador.position).normalized;
                    transform.position += huida * velocidad * Time.deltaTime;
                }
                if (vida > vidaMaxima * 0.6f) _estadoTank = EstadoTank.Avanzar;
                break;
        }
    }


    void ActualizarDPS()
    {
        switch (_estadoDPS)
        {
            case EstadoDPS.Patrulla:
                GetComponent<Renderer>().material.color = Color.green;
                Patrullar();
                if (VerJugador()) _estadoDPS = EstadoDPS.EsperarTank;
                break;

            case EstadoDPS.EsperarTank:
                GetComponent<Renderer>().material.color = Color.yellow;
                Debug.Log($"[{nombreAgente}] DPS esperando al Tank...");
                if (_tankHaEnganchado) _estadoDPS = EstadoDPS.Flanquear;
                break;

            case EstadoDPS.Flanquear:
                GetComponent<Renderer>().material.color = new Color(0.8f, 1f, 0f);
                if (jugador != null)
                {
                    Vector3 flanco = jugador.position + jugador.right * 3f;
                    MoverHacia(flanco);
                    if (Vector3.Distance(transform.position, flanco) < 1f)
                        _estadoDPS = EstadoDPS.Atacar;
                }
                break;

            case EstadoDPS.Atacar:
                GetComponent<Renderer>().material.color = Color.red;
                if (jugador != null)
                {
                    Debug.DrawLine(transform.position, jugador.position, Color.red);
                    MoverHacia(jugador.position);
                }
                if (!VerJugador()) _estadoDPS = EstadoDPS.Patrulla;
                break;
        }
    }


    void ActualizarSupport()
    {
        switch (_estadoSupport)
        {
            case EstadoSupport.Patrulla:
                GetComponent<Renderer>().material.color = Color.white;
                Patrullar();
                if (HayAliadoQueNecesitaCura()) _estadoSupport = EstadoSupport.Curar;
                break;

            case EstadoSupport.SeguirSquad:
                GetComponent<Renderer>().material.color = new Color(0.8f, 0.8f, 1f);
                SquadMiembro tank = BuscarPorRol(Rol.Tank);
                if (tank != null)
                {
                    Vector3 posSegura = tank.transform.position - tank.transform.forward * 4f;
                    MoverHacia(posSegura);
                }
                if (HayAliadoQueNecesitaCura()) _estadoSupport = EstadoSupport.Curar;
                break;

            case EstadoSupport.Curar:
                GetComponent<Renderer>().material.color = Color.cyan;
                CurarAliadoMasHerido();
                if (!HayAliadoQueNecesitaCura()) _estadoSupport = EstadoSupport.SeguirSquad;
                break;

            case EstadoSupport.Replegarse:
                GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0.5f);
                if (jugador != null)
                {
                    Vector3 huida = (transform.position - jugador.position).normalized;
                    transform.position += huida * velocidad * 1.5f * Time.deltaTime;
                }
                if (!VerJugador()) _estadoSupport = EstadoSupport.SeguirSquad;
                break;
        }
    }


    void AlTankEnganchado(DatosEvento e)
    {
        if (rol == Rol.DPS)
        {
            Debug.Log($"[{nombreAgente}] Tank enganchó. ¡Iniciando flanqueo!");
            _tankHaEnganchado = true;
        }
    }

    void AlBajoFuego(DatosEvento e)
    {
        if (rol == Rol.Support)
        {
            Debug.Log($"[{nombreAgente}] Aliado bajo fuego. Priorizando cura.");
            _estadoSupport = EstadoSupport.Curar;
        }
    }

    void AlAliadoCaido(DatosEvento e)
    {
        Debug.Log($"[{nombreAgente}] Un aliado ha caído. Ajustando estrategia.");
    }


    bool VerJugador()
    {
        if (jugador == null) return false;
        return Vector3.Distance(transform.position, jugador.position) < rangoDeteccion;
    }

    bool HayAliadoQueNecesitaCura()
    {
        foreach (var m in FindObjectsByType<SquadMiembro>(FindObjectsSortMode.None))
        {
            if (m == this) continue;
            float distancia = Vector3.Distance(transform.position, m.transform.position);
            if (distancia < rangoCuracion && m.vida < m.vidaMaxima * umbralCuracion)
                return true;
        }
        return false;
    }

    void CurarAliadoMasHerido()
    {
        SquadMiembro objetivo = null;
        float peorRatio = 1f;

        foreach (var m in FindObjectsByType<SquadMiembro>(FindObjectsSortMode.None))
        {
            if (m == this) continue;
            float distancia = Vector3.Distance(transform.position, m.transform.position);
            if (distancia >= rangoCuracion) continue;
            if (m.vida >= m.vidaMaxima * umbralCuracion) continue;

            float ratio = m.vida / m.vidaMaxima;
            if (ratio < peorRatio)
            {
                peorRatio = ratio;
                objetivo = m;
            }
        }

        if (objetivo == null) return;

        MoverHacia(objetivo.transform.position);
        objetivo.vida = Mathf.Min(objetivo.vidaMaxima, objetivo.vida + curacionPorSeg * Time.deltaTime);
    }

    SquadMiembro BuscarPorRol(Rol rolBuscado)
    {
        foreach (var m in FindObjectsByType<SquadMiembro>(FindObjectsSortMode.None))
            if (m != this && m.rol == rolBuscado) return m;
        return null;
    }

    void Patrullar()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        MoverHacia(waypoints[_waypointIndex].position);
        if (Vector3.Distance(transform.position, waypoints[_waypointIndex].position) < distanciaWaypoint)
            _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
    }

    void MoverHacia(Vector3 destino)
    {
        Vector3 dir = (destino - transform.position).normalized;
        transform.position += dir * velocidad * Time.deltaTime;
        if (dir.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Lerp(transform.forward, dir, 10f * Time.deltaTime);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = rol switch
        {
            Rol.Tank    => Color.blue,
            Rol.DPS     => Color.green,
            Rol.Support => Color.white,
            _           => Color.gray
        };
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);

        if (rol == Rol.Support)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, rangoCuracion);
        }
    }
}
