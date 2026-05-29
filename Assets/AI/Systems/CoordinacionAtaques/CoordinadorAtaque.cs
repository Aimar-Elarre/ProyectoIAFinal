// ============================================================
//  EJERCICIO: COORDINACIÓN DE ATAQUES
// ============================================================
//
//  CoordinadorAtaque es el cerebro táctico del squad.
//  Decide cuándo y cómo atacar, y da la señal a cada AgenteAtacante.
//
//  MODOS DE COORDINACIÓN:
//  ──────────────────────────────────────────────────────────────────
//  · Simultaneo  : todos atacan al mismo tiempo (embestida masiva).
//  · Secuencial  : los agentes atacan en turnos, uno por uno.
//  · PorOlas     : grupos de agentes atacan en oleadas sucesivas.
//
//  FASES DEL COORDINADOR:
//  ──────────────────────────────────────────────────────────────────
//  Esperar → Preparar → Atacar → Reorganizar → Esperar
//
//  · Esperar     : aguarda hasta que el jugador entre en rango.
//  · Preparar    : posiciona a los agentes en sus puntos de ataque.
//  · Atacar      : emite la señal según el modo de coordinación.
//  · Reorganizar : espera a que todos vuelvan y reinicia el ciclo.
//
//  PARTES DEL EJERCICIO
//  ──────────────────────────────────────────────────────────────────
//
//  [PARTE 1 — OBLIGATORIO]
//    Crea 4 AgenteAtacante.
//    Asigna este CoordinadorAtaque.
//    Prueba los modos Simultaneo y Secuencial.
//    ¿Cuál es más peligroso tácticamente? ¿Por qué?
//
//  [PARTE 2 — AMPLIACIÓN]
//    Implementa el modo PorOlas:
//    · Divide los agentes en grupos de tamanoOla.
//    · Cada ola espera a que la anterior llegue a rango antes de avanzar.
//    · Usa una coroutine o un timer para el intervalo entre olas.
//
//  [PARTE 3 — BONUS]
//    Añade condición de abortar el ataque:
//    Si más de la mitad de los agentes mueren durante el ataque,
//    el coordinador emite la señal de retirada y todos se repliegan.
//    Usa el EventBus para la señal de retirada.

using System.Collections.Generic;
using UnityEngine;

public class CoordinadorAtaque : MonoBehaviour
{
    public enum ModoAtaque { Simultaneo, Secuencial, PorOlas }
    public enum FaseCordinador { Esperar, Preparar, Atacar, Reorganizar }

    [Header("Modo de ataque")]
    public ModoAtaque modo = ModoAtaque.Simultaneo;

    [Header("Detección del jugador")]
    public Transform jugador;
    public float rangoActivacion = 10f;

    [Header("Secuencial")]
    [Tooltip("Tiempo entre señales en modo Secuencial (seg).")]
    public float intervaloSecuencial = 1.5f;

    [Header("Por Olas")]
    [Tooltip("Número de agentes por ola.")]
    public int tamanoOla = 2;
    [Tooltip("Tiempo entre olas (seg).")]
    public float intervaloOla = 2f;

    [Header("Reorganización")]
    [Tooltip("Tiempo de espera tras el ataque antes de reiniciar.")]
    public float tiempoReorganizacion = 4f;

    // Lista de agentes registrados al coordinador
    List<AgenteAtacante> _agentes = new List<AgenteAtacante>();

    FaseCordinador _fase = FaseCordinador.Esperar;
    float          _timer;
    int            _turnoActual;       // usado en modo Secuencial
    int            _olaActual;         // usado en modo PorOlas

    // Evento publicado cuando el coordinador ordena retirada
    public const string EventoRetirada = "Retirada";

    void Start()
    {
        // Registra automáticamente todos los AgenteAtacante en la escena
        _agentes.AddRange(FindObjectsByType<AgenteAtacante>(FindObjectsSortMode.None));
        Debug.Log($"[Coordinador] {_agentes.Count} agentes registrados.");
    }

    void Update()
    {
        switch (_fase)
        {
            case FaseCordinador.Esperar:
                FaseEsperar();
                break;
            case FaseCordinador.Preparar:
                FasePreparar();
                break;
            case FaseCordinador.Atacar:
                FaseAtacar();
                break;
            case FaseCordinador.Reorganizar:
                FaseReorganizar();
                break;
        }
    }

    // ── Fases del coordinador ─────────────────────────────────────────────

    void FaseEsperar()
    {
        if (jugador == null) return;
        if (Vector3.Distance(transform.position, jugador.position) < rangoActivacion)
        {
            Debug.Log("[Coordinador] Jugador detectado. Preparando ataque.");
            OrdenarPosicionamiento();
            _fase = FaseCordinador.Preparar;
        }
    }

    void FasePreparar()
    {
        // Espera a que todos los agentes estén en posición
        bool todosPosicionados = true;
        foreach (var a in _agentes)
            if (!a.EstaEnPosicion()) { todosPosicionados = false; break; }

        if (todosPosicionados)
        {
            Debug.Log($"[Coordinador] Todos en posición. Iniciando ataque en modo {modo}.");
            IniciarAtaque();
            _fase = FaseCordinador.Atacar;
        }
    }

    void FaseAtacar()
    {
        switch (modo)
        {
            case ModoAtaque.Secuencial:
                ActualizarAtaqueSecuencial();
                break;
            case ModoAtaque.PorOlas:
                ActualizarAtaquePorOlas();
                break;
        }

        // La fase termina cuando todos los agentes hayan completado su ataque
        bool todosCompletados = true;
        foreach (var a in _agentes)
            if (!a.AtaqueCompletado()) { todosCompletados = false; break; }

        if (todosCompletados)
        {
            Debug.Log("[Coordinador] Ataque completado. Reorganizando.");
            _timer = tiempoReorganizacion;
            _fase  = FaseCordinador.Reorganizar;
        }
    }

    void FaseReorganizar()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            Debug.Log("[Coordinador] Reorganización completa. Volviendo a esperar.");
            ResetearAgentes();
            _fase = FaseCordinador.Esperar;
        }
    }

    // ── Control de agentes ────────────────────────────────────────────────

    void OrdenarPosicionamiento()
    {
        // Asigna a cada agente una posición de ataque alrededor del jugador
        for (int i = 0; i < _agentes.Count; i++)
        {
            float angulo = i * (360f / _agentes.Count) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(angulo), 0f, Mathf.Cos(angulo)) * 5f;
            Vector3 posAtaque = jugador.position + offset;
            _agentes[i].AsignarPosicionAtaque(posAtaque, jugador);
        }
    }

    void IniciarAtaque()
    {
        _turnoActual = 0;
        _olaActual   = 0;
        _timer       = 0f;

        if (modo == ModoAtaque.Simultaneo)
        {
            // Todos atacan a la vez
            foreach (var a in _agentes)
                a.OrdenaAtacar();
        }
    }

    void ActualizarAtaqueSecuencial()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        if (_turnoActual >= _agentes.Count) return;

        _agentes[_turnoActual].OrdenaAtacar();
        Debug.Log($"[Coordinador] Turno {_turnoActual + 1}/{_agentes.Count} — atacando.");
        _turnoActual++;
        _timer = intervaloSecuencial;
    }

    void ActualizarAtaquePorOlas()
    {
        if (tamanoOla <= 0) tamanoOla = 1;
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        int startIndex = _olaActual * tamanoOla;
        if (startIndex >= _agentes.Count) return;

        int endIndex = Mathf.Min(_agentes.Count, startIndex + tamanoOla);
        for (int i = startIndex; i < endIndex; i++)
            _agentes[i].OrdenaAtacar();

        Debug.Log($"[Coordinador] Ola {_olaActual + 1} atacando ({startIndex + 1}-{endIndex}).");
        _olaActual++;
        _timer = intervaloOla;
    }

    void ResetearAgentes()
    {
        foreach (var a in _agentes)
            a.Resetear();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, rangoActivacion);

        if (jugador != null)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
            Gizmos.DrawLine(transform.position, jugador.position);
        }
    }
}
