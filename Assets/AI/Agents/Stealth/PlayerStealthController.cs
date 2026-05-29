using UnityEngine;

[DisallowMultipleComponent]
public class PlayerStealthController : MonoBehaviour
{
    [Header("Sigilo")]
    public bool IsHidden { get; private set; }
    public bool IsInDarkZone { get; private set; }

    void Update()
    {
        if (GridManager.instance == null)
        {
            IsInDarkZone = false;
            IsHidden = false;
            return;
        }

        Node node = GridManager.instance.NodeFromWorldPoint(transform.position);
        IsInDarkZone = node != null && node.isDark;
        IsHidden = IsInDarkZone;
    }

    void OnDrawGizmosSelected()
    {
        if (!IsHidden) return;
        Gizmos.color = new Color(0f, 0f, 0f, 0.35f);
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}
