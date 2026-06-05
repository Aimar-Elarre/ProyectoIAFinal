
using UnityEngine;

public class FormacionLider : MonoBehaviour
{
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
        if (jugador != null && Vector3.Distance(transform.position, jugador.position) < rangoDeteccion)
            MoverHacia(jugador.position, velocidad * 1.5f);
        else
            Patrullar();
    }


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

    Vector3 SlotLinea(int indice, int total)
    {
        float offset = (indice - (total - 1) * 0.5f) * separacion;
        return transform.position + transform.right * offset - transform.forward * separacion;
    }

    Vector3 SlotCuña(int indice, int total)
    {
        int fila  = indice / 2 + 1;
        float lado = (indice % 2 == 0) ? -1f : 1f;
        return transform.position
               - transform.forward * (fila * separacion)
               + transform.right   * (lado * fila * separacion * 0.6f);
    }

    Vector3 SlotCirculo(int indice, int total)
    {
        float angulo = indice * (360f / total) * Mathf.Deg2Rad;
        float x = Mathf.Sin(angulo) * separacion * 1.5f;
        float z = Mathf.Cos(angulo) * separacion * 1.5f;
        return transform.position + new Vector3(x, 0f, z);
    }


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
