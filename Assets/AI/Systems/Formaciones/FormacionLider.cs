// ============================================================
//  EJERCICIO: FORMACIONES
// ============================================================
//
//  FormacionLider calcula las posiciones de los slots según
//  el tipo de formación activa y las expone a los miembros.
//
//  FORMACIONES DISPONIBLES:
//  ──────────────────────────────────────────────────────────
//  · Linea  : miembros alineados perpendicular al frente del líder.
//  · Cuña   : forma de V, líder al frente y miembros hacia atrás.
//  · Circulo: miembros distribuidos uniformemente alrededor del líder.
//
//  ÁRBOL DE COMPORTAMIENTO DEL LÍDER:
//  ──────────────────────────────────────────────────────────
//  El líder patrulla waypoints mientras actualiza los slots.
//  Al detectar al jugador cambia a persecución en formación.
//
//  PARTES DEL EJERCICIO
//  ──────────────────────────────────────────────────────────
//
//  [PARTE 1 — OBLIGATORIO]
//    Abre la escena Unit8_FormacionesScene.
//    Crea un líder y 3-5 miembros (FormacionMiembro).
//    Cambia el tipo de formación en el Inspector durante Play.
//    ¿Los miembros se redistribuyen suavemente?
//
//  [PARTE 2 — AMPLIACIÓN]
//    Implementa transición automática de formación:
//    · Linea   → al patrullar (calma).
//    · Cuña    → al ver al jugador (avance).
//    · Circulo → al recibir daño (defensa).
//    Usa el método CambiarFormacion() desde un BT o FSM.
//
//  [PARTE 3 — BONUS]
//    Añade una formación "Caja" (Box):
//    4 slots en las esquinas de un cuadrado alrededor del líder.
//    El slot del frente-izquierda y frente-derecha van delante,
//    los de atrás-izquierda y atrás-derecha van detrás.

using UnityEngine;

public class FormacionLider : MonoBehaviour
{
    // Tipos de formación soportados
    public enum TipoFormacion { Linea, Cuña, Circulo }

    [Header("Formación")]
    public TipoFormacion formacion = TipoFormacion.Linea;
    [Tooltip("Distancia entre slots en la formación.")]
    public float separacion = 1.5f;

    [Header("Patrulla")]
    public Transform[] waypoints;
    public float velocidad = 3f;
    public float distanciaWaypoint = 0.5f;

    [Header("Detección")]
    public Transform jugador;
    public float rangoDeteccion = 6f;

    int _waypointIndex;

    void Update()
    {
        // Movimiento del líder: patrulla o persigue al jugador
        if (jugador != null && Vector3.Distance(transform.position, jugador.position) < rangoDeteccion)
            MoverHacia(jugador.position, velocidad * 1.5f);
        else
            Patrullar();
    }

    // ── Cálculo de slots ──────────────────────────────────────────────────

    // Devuelve la posición mundial del slot indicado para el número total de miembros.
    public Vector3 ObtenerPosicionSlot(int indice, int totalMiembros)
    {
        return formacion switch
        {
            TipoFormacion.Linea   => SlotLinea(indice, totalMiembros),
            TipoFormacion.Cuña    => SlotCuña(indice, totalMiembros),
            TipoFormacion.Circulo => SlotCirculo(indice, totalMiembros),
            _                     => transform.position
        };
    }

    // Formación en línea: todos los miembros centrados a los lados del líder.
    Vector3 SlotLinea(int indice, int total)
    {
        // Offset lateral en el eje derecho del líder
        float offset = (indice - (total - 1) * 0.5f) * separacion;
        return transform.position + transform.right * offset - transform.forward * separacion;
    }

    // Formación en cuña (V): el líder va adelante, los miembros se abren en diagonal.
    Vector3 SlotCuña(int indice, int total)
    {
        // Mitad izquierda (índices pares) y mitad derecha (índices impares)
        int fila  = indice / 2 + 1;
        float lado = (indice % 2 == 0) ? -1f : 1f;
        return transform.position
               - transform.forward * (fila * separacion)
               + transform.right   * (lado * fila * separacion * 0.6f);
    }

    // Formación circular: miembros distribuidos equitativamente alrededor del líder.
    Vector3 SlotCirculo(int indice, int total)
    {
        float angulo = indice * (360f / total) * Mathf.Deg2Rad;
        float x = Mathf.Sin(angulo) * separacion * 1.5f;
        float z = Mathf.Cos(angulo) * separacion * 1.5f;
        return transform.position + new Vector3(x, 0f, z);
    }

    // ── Utilidades ────────────────────────────────────────────────────────

    public void CambiarFormacion(TipoFormacion nueva) => formacion = nueva;

    void Patrullar()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        Transform destino = waypoints[_waypointIndex];
        MoverHacia(destino.position, velocidad);
        if (Vector3.Distance(transform.position, destino.position) < distanciaWaypoint)
            _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
    }

    void MoverHacia(Vector3 destino, float vel)
    {
        Vector3 dir = (destino - transform.position).normalized;
        transform.position += dir * vel * Time.deltaTime;
        if (dir.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Lerp(transform.forward, dir, 10f * Time.deltaTime);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);

        // Muestra los slots en la escena para facilitar el debug
        var miembros = FindObjectsByType<FormacionMiembro>(FindObjectsSortMode.None);
        int total = miembros.Length;
        for (int i = 0; i < total; i++)
        {
            Vector3 slot = ObtenerPosicionSlot(i, total);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(slot, 0.25f);
            Gizmos.DrawLine(transform.position, slot);
        }
    }
}
