using UnityEngine;

public class HumanModelVisibility : MonoBehaviour
{
    private Renderer[] modelRenderers;

    private void Awake()
    {
        modelRenderers = GetComponentsInChildren<Renderer>(true);
        HideModel();
    }

    public void ShowModel()
    {
        SetVisible(true);
    }

    public void HideModel()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer modelRenderer in modelRenderers)
        {
            if (modelRenderer != null)
            {
                modelRenderer.enabled = visible;
            }
        }
    }
}