// ============================================================
//  EJERCICIO: ROLES EN SQUAD
// ============================================================
//
//  Cada miembro del squad tiene un rol que define su comportamiento
//  en combate. Los roles se coordinan entre sí a través del EventBus.
//
//  ROLES DISPONIBLES:
//  ──────────────────────────────────────────────────────────────────
//  · Tank    : avanza hacia el enemigo, absorbe daño, protege al equipo.
//              FSM: Patrulla → Provocar → Aguantar Fuego → Retroceder
//  · DPS     : espera a que el tank enganche al enemigo y luego flanquea.
//              FSM: Patrulla → Esperar_Tank → Flanquear → Atacar
//  · Support : permanece alejado, cura a aliados con poca vida cercanos.
//              FSM: Patrulla → Seguir_Squad → Curar → Replegarse
//
//  COORDINACIÓN CON EVENTBUS:
//  ──────────────────────────────────────────────────────────────────
//  · Tank publica "TankEncontrado" cuando llega a rango del enemigo.
//  · DPS escucha "TankEncontrado" para iniciar el flanqueo.
//  · Support escucha "BajoFuego" para priorizar curas.
//
//  PARTES DEL EJERCICIO
//  ──────────────────────────────────────────────────────────────────
//
//  [PARTE 1 — OBLIGATORIO]
//    Crea 3 GameObjects con este componente.
//    Asigna rol Tank a uno, DPS a otro y Support al tercero.
//    Observa cómo el DPS espera al Tank antes de atacar.
//    ¿Qué pasa si hay dos Tanks?
//
//  [PARTE 2 — AMPLIACIÓN]
//    Implementa la lógica de curación del Support:
//    · Busca al aliado más cercano con vida < umbralCuracion.
//    · Se mueve hacia él y restaura vida a razón de curacionPorSeg.
//    · Si no hay aliados que curar, patrulla.
//
//  [PARTE 3 — BONUS]
//    Añade un rol "Scout":
//    · Explora el mapa en busca del jugador.
//    · Al detectarlo, publica EnemigoDetectado y vuelve al squad.
//    · Tiene más velocidad pero menos vida que los demás roles.

using System.Collections.Generic;
using UnityEngine;

public class SquadMiembro : MonoBehaviour
{
    // ── Definición de roles ───────────────────────────────────────────────
    public enum Rol { Tank, DPS, Support }

    // Evento interno para que el DPS sepa cuándo el Tank ha enganchado
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

    // Estado interno por rol
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
        // Simula daño con Q
        if (UnityEngine.InputSystem.Keyboard.current.qKey.isPressed)
            vida = Mathf.Max(0f, vida - 10f * Time.deltaTime);

        switch (rol)
        {
            case Rol.Tank:    ActualizarTank();    break;
            case Rol.DPS:     ActualizarDPS();     break;
            case Rol.Support: ActualizarSupport(); break;
        }
    }

    // ── Comportamiento TANK ───────────────────────────────────────────────

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
                    // Tank ha llegado a rango — avisa al squad
                    Debug.Log($"[{nombreAgente}] Tank enganchó al enemigo.");
                    EventBus.Publicar(new DatosEvento(TankEnganchado, jugador.position, gameObject));
                    _estadoTank = EstadoTank.Aguantar;
                }
                if (!veJugador) _estadoTank = EstadoTank.Patrulla;
                break;

            case EstadoTank.Aguantar:
                // Se queda parado absorbiendo daño (en escena real haría animación de bloqueo)
                GetComponent<Renderer>().material.color = Color.red;
                if (vida < vidaMaxima * 0.3f) _estadoTank = EstadoTank.Retroceder;
                if (!veJugador) _estadoTank = EstadoTank.Patrulla;
                break;

            case EstadoTank.Retroceder:
                GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0f);
                // Retrocede en dirección opuesta al jugador
                if (jugador != null)
                {
                    Vector3 huida = (transform.position - jugador.position).normalized;
                    transform.position += huida * velocidad * Time.deltaTime;
                }
                if (vida > vidaMaxima * 0.6f) _estadoTank = EstadoTank.Avanzar;
                break;
        }
    }

    // ── Comportamiento DPS ────────────────────────────────────────────────

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
                // Espera cerca del centro del squad sin avanzar
                GetComponent<Renderer>().material.color = Color.yellow;
                Debug.Log($"[{nombreAgente}] DPS esperando al Tank...");
                if (_tankHaEnganchado) _estadoDPS = EstadoDPS.Flanquear;
                break;

            case EstadoDPS.Flanquear:
                GetComponent<Renderer>().material.color = new Color(0.8f, 1f, 0f);
                // Mueve hacia la posición lateral del jugador para flanquear
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

    // ── Comportamiento SUPPORT ────────────────────────────────────────────

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
                // Se mantiene detrás del squad
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

    // ── Callbacks de eventos ──────────────────────────────────────────────

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
        // Opcional: reposicionar o cambiar de objetivo
    }

    // ── Utilidades ────────────────────────────────────────────────────────

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
