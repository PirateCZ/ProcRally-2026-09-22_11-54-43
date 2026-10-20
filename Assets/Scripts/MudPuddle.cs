using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class MudPuddle : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;

    private void Awake()
    {
        ConfigureTrigger();
    }

    private void OnValidate()
    {
        ConfigureTrigger();
    }

    private void ConfigureTrigger()
    {
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            return;
        }

        sphereCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }

        if (uiManager == null)
        {
            Debug.LogError("MudPuddle could not find a UIManager to show the overlay.", this);
            return;
        }

        uiManager.ShowScreenOverlay();
    }
}