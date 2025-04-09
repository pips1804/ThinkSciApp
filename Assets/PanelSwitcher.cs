using UnityEngine;

public class PanelSwitcher : MonoBehaviour
{
    public GameObject panelToActivate;
    public GameObject[] panelsToDeactivate;

    public void ActivatePanel()
    {
        foreach (var panel in panelsToDeactivate)
        {
            panel.SetActive(false);
        }

        if (panelToActivate != null)
        {
            panelToActivate.SetActive(true);
        }
    }
}
