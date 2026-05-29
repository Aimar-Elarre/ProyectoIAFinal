// ============================================================
//  EJERCICIO: COMUNICACIÓN BASADA EN EVENTOS
// ============================================================
//
//  Cada SquadComunicacion es un agente del squad que:
//  · Publica eventos cuando detecta amenazas o recibe daño.
//  · Reacciona a eventos de otros miembros del squad.
//
//  FLUJO DE COMUNICACIÓN:
//  ──────────────────────────────────────────────────────────────────
//  AgentA detecta jugador  →  Publica "EnemigoDetectado"
//  AgentB y AgentC escuchan →  Cambian estado a Alerta
//  AgentA recibe daño       →  Publica "SolicitarAyuda"
//  AgentC (support)         →  Se mueve hacia AgentA
//
//  PARTES DEL EJERCICIO
//  ──────────────────────────────────────────────────────────────────
//
//  [PARTE 1 — OBLIGATORIO]
//    Añade este componente a 3 agentes en la escena.
//    Observa en la consola cómo los eventos se propagan
//    cuando uno de ellos detecta al jugador.
//    ¿Todos reaccionan aunque no vean al jugador?
//
//  [PARTE 2 — AMPLIACIÓN]
//    Implementa el evento "AliadoCaido":
//    Cuando la vida llega a 0, publica el evento con la posición.
//    Los demás agentes deben registrar la muerte y ajustar su
//    comportamiento (p.ej., el support deja de ayudar al caído).
//
//  [PARTE 3 — BONUS]
//    Añade un tiempo de "silencio de radio": después de publicar
//    un evento, ese agente no puede volver a publicar el mismo
//    tipo durante cooldownRadio segundos. Evita spam de mensajes.

using UnityEngine;

public class SquadComunicacion : MonoBehaviour
{
    // Estados internos del agente reactivos a eventos
    public enum EstadoAgente { Patrulla, Alerta, AcudiendoAyuda, Combate }

    [Header("Identificación")]
    public string nombreAgente = "Agente";

    [Header("Referencias")]
    public Transform jugador;

    [Header("Detección")]
    public float rangoDeteccion = 5f;

    [Header("Salud")]
    public float vida = 100f;
    public float vidaMaxima = 100f;

    [Header("Movimiento")]
    public float velocidad = 3.5f;

    [Header("Cooldown de radio (seg)")]
    [Tooltip("Tiempo mínimo entre publicaciones del mismo evento.")]
    public float cooldownRadio = 2f;

    EstadoAgente _estado = EstadoAgente.Patrulla;
    Vector3 _posicionAyuda;
    float _timerCooldownDeteccion;
    float _timerCooldownAyuda;

    void OnEnable()
    {
        // Suscripción a eventos del bus al activarse
        EventBus.Suscribir(EventBus.EnemigoDetectado, AlRecibirAlerta);
        EventBus.Suscribir(EventBus.SolicitarAyuda, AlRecibirSolicitudAyuda);
        EventBus.Suscribir(EventBus.ObjetivoEliminado, AlObjetivoEliminado);
    }

    void OnDisable()
    {
        // Cancelar suscripción al desactivarse para evitar memory leaks
        EventBus.Desuscribir(EventBus.EnemigoDetectado, AlRecibirAlerta);
        EventBus.Desuscribir(EventBus.SolicitarAyuda, AlRecibirSolicitudAyuda);
        EventBus.Desuscribir(EventBus.ObjetivoEliminado, AlObjetivoEliminado);
    }

    void Update()
    {
        _timerCooldownDeteccion -= Time.deltaTime;
        _timerCooldownAyuda -= Time.deltaTime;

        // Detección propia del jugador
        if (jugador != null && Vector3.Distance(transform.position, jugador.position) < rangoDeteccion)
            PercibioJugador();

        // Simulación de daño con tecla Q
        if (UnityEngine.InputSystem.Keyboard.current.qKey.wasPressedThisFrame)
            RecibirDaño(30f);

        ActualizarComportamiento();
    }

    // ── Lógica de publicación ─────────────────────────────────────────────

    void PercibioJugador()
    {
        Debug.Log($"[{nombreAgente}] Enemigo detectado — publicando evento.");
        EventBus.Publicar(new DatosEvento(
            EventBus.EnemigoDetectado,
            jugador.position,
            gameObject
        ));
        _estado = EstadoAgente.Combate;
    }

    void RecibirDaño(float cantidad)
    {
        vida = Mathf.Max(0f, vida - cantidad);
        Debug.Log($"[{nombreAgente}] Recibió daño. Vida: {vida}");

        Debug.Log($"[{nombreAgente}] Solicitando ayuda en {transform.position}.");
        EventBus.Publicar(new DatosEvento(
            EventBus.SolicitarAyuda,
            transform.position,
            gameObject
        ));

        if (vida <= 0f)
        {
            Debug.Log($"[{nombreAgente}] Aliado caído. Publicando evento.");
            EventBus.Publicar(new DatosEvento(
                EventBus.AliadoCaido,
                transform.position,
                gameObject
            ));
        }
    }

    // ── Callbacks de eventos recibidos ────────────────────────────────────

    void AlRecibirAlerta(DatosEvento e)
    {
        // No reaccionamos a nuestros propios eventos
        if (e.emisor == gameObject) return;

        Debug.Log($"[{nombreAgente}] ¡Alerta recibida de {e.emisor?.name}! Posición del enemigo: {e.posicion}");
        _estado = EstadoAgente.Alerta;
    }

    void AlRecibirSolicitudAyuda(DatosEvento e)
    {
        if (e.emisor == gameObject) return;

        Debug.Log($"[{nombreAgente}] Acudiendo a ayudar a {e.emisor?.name} en {e.posicion}.");
        _posicionAyuda = e.posicion;
        _estado = EstadoAgente.AcudiendoAyuda;
    }

    void AlObjetivoEliminado(DatosEvento e)
    {
        Debug.Log($"[{nombreAgente}] Objetivo eliminado. Volviendo a patrulla.");
        _estado = EstadoAgente.Patrulla;
    }

    // ── Comportamiento por estado ─────────────────────────────────────────

    void ActualizarComportamiento()
    {
        switch (_estado)
        {
            case EstadoAgente.AcudiendoAyuda:
                MoverHacia(_posicionAyuda);
                if (Vector3.Distance(transform.position, _posicionAyuda) < 1f)
                    _estado = EstadoAgente.Alerta;
                break;

            case EstadoAgente.Combate:
                if (jugador != null) MoverHacia(jugador.position);
                break;
        }
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
        // Color según estado actual
        Gizmos.color = _estado switch
        {
            EstadoAgente.Combate => Color.red,
            EstadoAgente.Alerta => Color.yellow,
            EstadoAgente.AcudiendoAyuda => Color.green,
            _ => Color.white
        };
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);

        if (_estado == EstadoAgente.AcudiendoAyuda)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _posicionAyuda);
        }
    }
}
