using UnityEngine;

public class PanelSwitcher : MonoBehaviour
{
    [Header("Single Panel (old system, still works)")]
    public GameObject panelToActivate;

    [Header("Multiple Panels (new system, optional)")]
    public GameObject[] panelsToActivate;

    [Header("Panels to Deactivate")]
    public GameObject[] panelsToDeactivate;

    public void ActivatePanel()
    {
        // 🔴 Deactivate all given panels
        foreach (var panel in panelsToDeactivate)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        // 🟢 Activate single panel (old system)
        if (panelToActivate != null)
        {
            panelToActivate.SetActive(true);
            HandleOrientation(panelToActivate);
        }

        // 🟢 Activate multiple panels (new system)
        foreach (var panel in panelsToActivate)
        {
            if (panel != null)
            {
                panel.SetActive(true);
                HandleOrientation(panel);
            }
        }
    }

    private void HandleOrientation(GameObject panel)
    {
        if (panel.name.Contains("Swipe"))
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }
        else
        {
            Screen.orientation = ScreenOrientation.Portrait;
        }
    }
}
