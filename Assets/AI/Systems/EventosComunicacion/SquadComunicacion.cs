
using UnityEngine;

public class SquadComunicacion : MonoBehaviour
{
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
        EventBus.Suscribir(EventBus.EnemigoDetectado, AlRecibirAlerta);
        EventBus.Suscribir(EventBus.SolicitarAyuda, AlRecibirSolicitudAyuda);
        EventBus.Suscribir(EventBus.ObjetivoEliminado, AlObjetivoEliminado);
    }

    void OnDisable()
    {
        EventBus.Desuscribir(EventBus.EnemigoDetectado, AlRecibirAlerta);
        EventBus.Desuscribir(EventBus.SolicitarAyuda, AlRecibirSolicitudAyuda);
        EventBus.Desuscribir(EventBus.ObjetivoEliminado, AlObjetivoEliminado);
    }

    void Update()
    {
        _timerCooldownDeteccion -= Time.deltaTime;
        _timerCooldownAyuda -= Time.deltaTime;

        if (jugador != null && Vector3.Distance(transform.position, jugador.position) < rangoDeteccion)
            PercibioJugador();

        if (UnityEngine.InputSystem.Keyboard.current.qKey.wasPressedThisFrame)
            RecibirDaño(30f);

        ActualizarComportamiento();
    }


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


    void AlRecibirAlerta(DatosEvento e)
    {
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
