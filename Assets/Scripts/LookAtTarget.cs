using UnityEngine;

[DisallowMultipleComponent]
public class LookAtTarget : MonoBehaviour
{
    [Tooltip("Transform al que debe mirar este objeto.")]
    public Transform target;

    [Tooltip("Velocidad de rotación en grados por segundo.")]
    public float rotationSpeed = 720f;

    [Tooltip("Si está activado, el objeto rotará solo alrededor del eje Y.")]
    public bool onlyRotateY = false;

    void Update()
    {
        if (target == null)
            return;

        Vector3 direction = target.position - transform.position;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        if (onlyRotateY)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
