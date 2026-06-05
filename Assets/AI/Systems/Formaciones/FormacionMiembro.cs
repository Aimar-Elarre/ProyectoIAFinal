
using UnityEngine;

public class FormacionMiembro : MonoBehaviour
{
    [Header("Identificación en el squad")]
    [Tooltip("Índice único dentro de la formación (0, 1, 2, …).")]
    public int indice = 0;

    [Header("Movimiento")]
    public float velocidadMax  = 4f;
    public float fuerzaSteering = 8f;
    public float radioFrenado   = 1.5f;
    public float radioLlegada   = 0.3f;

    FormacionLider _lider;
    Vector3        _velocidad;

    void Start()
    {
        _lider = FindFirstObjectByType<FormacionLider>();
        if (_lider == null)
            Debug.LogWarning($"[FormacionMiembro] No se encontró ningún FormacionLider en la escena.");
    }

    void Update()
    {
        if (_lider == null) return;

        int totalMiembros = FindObjectsByType<FormacionMiembro>(FindObjectsSortMode.None).Length;
        Vector3 slotObjetivo = _lider.ObtenerPosicionSlot(indice, totalMiembros);

        Vector3 deseada = Arrive(slotObjetivo);
        _velocidad = Vector3.MoveTowards(_velocidad, deseada, fuerzaSteering * Time.deltaTime);
        transform.position += _velocidad * Time.deltaTime;

        if (_velocidad.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Lerp(transform.forward, _velocidad.normalized, 10f * Time.deltaTime);
    }

    Vector3 Arrive(Vector3 objetivo)
    {
        Vector3 diff = objetivo - transform.position;
        float dist   = diff.magnitude;
        if (dist < 0.01f) return Vector3.zero;

        float velocidadEscalada = (dist < radioFrenado)
            ? velocidadMax * (dist / radioFrenado)
            : velocidadMax;

        return diff.normalized * velocidadEscalada;
    }

    void OnDrawGizmos()
    {
        if (_lider == null) return;
        int total = FindObjectsByType<FormacionMiembro>(FindObjectsSortMode.None).Length;
        Vector3 slot = _lider.ObtenerPosicionSlot(indice, total);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, slot);
    }
}
