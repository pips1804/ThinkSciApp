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

            
            if (panelToActivate.name.Contains("Swipe"))
            {
                Screen.orientation = ScreenOrientation.LandscapeLeft;
            }
            else
            {
                Screen.orientation = ScreenOrientation.Portrait;
            }

            // Optional alternative using tag:
            // if (panelToActivate.CompareTag("QuizPanel")) {
            //     Screen.orientation = ScreenOrientation.Landscape;
            // } else {
            //     Screen.orientation = ScreenOrientation.Portrait;
            // }
        }
    }
}
