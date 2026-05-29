// ============================================================
//  AgenteAtacante
// ============================================================
//
//  Agente individual que recibe órdenes del CoordinadorAtaque.
//  Tiene su propia máquina de estados simple:
//
//  Esperar → MoviéndoseAPos → Listo → Atacar → Completado
//
//  · Esperar       : aguarda la asignación de posición.
//  · MoviéndoseAPos: se desplaza a su punto de ataque.
//  · Listo         : en posición, esperando la señal de ataque.
//  · Atacar        : avanza y ataca al objetivo.
//  · Completado    : ataque terminado, espera reseteo.
//
//  EJERCICIO:
//  · Observa la diferencia visual entre ataque Simultaneo y Secuencial.
//  · Añade un Gizmo que dibuje el vector de ataque en rojo durante Atacar.

using System.Collections.Generic;
using UnityEngine;

public class AgenteAtacante : MonoBehaviour
{
    enum Estado { Esperar, MoviéndoseAPos, Listo, Atacar, Completado }

    [Header("Movimiento")]
    public float velocidad       = 4f;
    public float radioLlegada    = 0.5f;
    public float rangoAtaque     = 1.5f;
    public float avoidRadius     = 1f;
    public float avoidStrength   = 1f;

    static readonly List<AgenteAtacante> _allAgents = new List<AgenteAtacante>();

    [Header("Debug visual")]
    public Color colorEsperar    = Color.gray;
    public Color colorMovimiento = Color.yellow;
    public Color colorListo      = Color.green;
    public Color colorAtaque     = Color.red;
    public Color colorCompletado = Color.black;

    Estado    _estado  = Estado.Esperar;
    Vector3   _posAtaque;
    Transform _objetivo;
    Renderer  _renderer;

    void OnEnable() => _allAgents.Add(this);
    void OnDisable() => _allAgents.Remove(this);

    void Awake() => _renderer = GetComponent<Renderer>();

    void Update()
    {
        switch (_estado)
        {
            case Estado.MoviéndoseAPos:
                MoverHacia(_posAtaque);
                if (Vector3.Distance(transform.position, _posAtaque) < radioLlegada)
                {
                    _estado = Estado.Listo;
                    ActualizarColor();
                }
                break;

            case Estado.Atacar:
                if (_objetivo == null) { _estado = Estado.Completado; break; }
                MoverHacia(_objetivo.position);
                Debug.DrawLine(transform.position, _objetivo.position, Color.red);
                if (Vector3.Distance(transform.position, _objetivo.position) < rangoAtaque)
                {
                    Debug.Log($"[{name}] ¡Golpe al objetivo!");
                    _estado = Estado.Completado;
                    ActualizarColor();
                }
                break;
        }
    }

    // ── API pública para el coordinador ──────────────────────────────────

    // Asigna la posición táctica de ataque y el objetivo a golpear
    public void AsignarPosicionAtaque(Vector3 posicion, Transform objetivo)
    {
        _posAtaque = posicion;
        _objetivo  = objetivo;
        _estado    = Estado.MoviéndoseAPos;
        ActualizarColor();
        Debug.Log($"[{name}] Posición asignada: {posicion}");
    }

    // El coordinador ordena iniciar el ataque
    public void OrdenaAtacar()
    {
        if (_estado != Estado.Listo) return;
        Debug.Log($"[{name}] ¡Atacando!");
        _estado = Estado.Atacar;
        ActualizarColor();
    }

    // Devuelve true si el agente está en su posición y listo
    public bool EstaEnPosicion() => _estado == Estado.Listo;

    // Devuelve true cuando el ataque ha concluido
    public bool AtaqueCompletado() => _estado == Estado.Completado;

    // Reinicia el agente para el siguiente ciclo
    public void Resetear()
    {
        _estado    = Estado.Esperar;
        _posAtaque = Vector3.zero;
        _objetivo  = null;
        ActualizarColor();
    }

    // ── Utilidades ────────────────────────────────────────────────────────

    void MoverHacia(Vector3 destino)
    {
        Vector3 dir = (destino - transform.position).normalized;
        Vector3 separation = Vector3.zero;
        int count = 0;

        foreach (var other in _allAgents)
        {
            if (other == this) continue;
            float distance = Vector3.Distance(transform.position, other.transform.position);
            if (distance > 0f && distance < avoidRadius)
            {
                separation += (transform.position - other.transform.position).normalized * (avoidRadius - distance);
                count++;
            }
        }

        if (count > 0)
        {
            separation = separation / count;
            dir += separation.normalized * avoidStrength;
        }

        dir = dir.normalized;
        transform.position += dir * velocidad * Time.deltaTime;
        if (dir.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Lerp(transform.forward, dir, 10f * Time.deltaTime);
    }

    void ActualizarColor()
    {
        if (_renderer == null) return;
        _renderer.material.color = _estado switch
        {
            Estado.Esperar         => colorEsperar,
            Estado.MoviéndoseAPos  => colorMovimiento,
            Estado.Listo           => colorListo,
            Estado.Atacar          => colorAtaque,
            Estado.Completado      => colorCompletado,
            _                      => Color.white
        };
    }

    void OnDrawGizmos()
    {
        if (_estado == Estado.MoviéndoseAPos || _estado == Estado.Listo)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_posAtaque, 0.3f);
            Gizmos.DrawLine(transform.position, _posAtaque);
        }

        if (_estado == Estado.Atacar && _objetivo != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _objetivo.position);
            Gizmos.DrawWireSphere(transform.position, rangoAtaque);
        }
    }
}
