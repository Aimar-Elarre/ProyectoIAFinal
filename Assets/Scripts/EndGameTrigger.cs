using UnityEngine;

public class EndGameTrigger : MonoBehaviour
{
    [Tooltip("Layers that can activate this trigger (select the Player layer)")]
    public LayerMask triggerLayer;

    [Tooltip("UI GameObject to activate when the trigger fires. Should start disabled.")]
    public GameObject uiToActivate;

    public bool ensureDisabledAtStart = true;
    public bool pauseAudio = true;

    void Start()
    {
        if (ensureDisabledAtStart && uiToActivate != null)
            uiToActivate.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if ((triggerLayer.value & (1 << other.gameObject.layer)) == 0)
            return;

        if (uiToActivate != null)
            uiToActivate.SetActive(true);

        Time.timeScale = 0f;

        if (pauseAudio)
            AudioListener.pause = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        try
        {
            EventBus.Publicar(new DatosEvento("PausaJuego", transform.position, gameObject, true));
        }
        catch { }
    }
}
