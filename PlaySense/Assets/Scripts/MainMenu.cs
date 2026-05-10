using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject HandTrackingMarker;
    public GameObject HandTrackingObject;
    public Image PanelBG;
    public bool isHandTrackingActive = false;
    public bool isTrackersEnabled = false;
    public bool isBackgroundEnabled = false;

    void Start()
    {

        isHandTrackingActive = PlayerPrefs.GetInt("HandTracking", 0) == 1;
        isTrackersEnabled = PlayerPrefs.GetInt("Trackers", 0) == 1;
        isBackgroundEnabled = PlayerPrefs.GetInt("Background", 0) == 1;

        if (HandTrackingObject != null)
        {
            HandTrackingObject.SetActive(isHandTrackingActive);
        }
        if (HandTrackingMarker != null)
        {
            HandTrackingMarker.SetActive(isTrackersEnabled);
        }
        if (PanelBG != null)
        {
            PanelBG.color = isBackgroundEnabled ? new Color(PanelBG.color.r, PanelBG.color.g, PanelBG.color.b, 1f) : new Color(PanelBG.color.r, PanelBG.color.g, PanelBG.color.b, 0.7f);
        }
    }

    public void LoadTicTacToe() => SceneManager.LoadScene("TicTacToe");
    public void LoadPong() => SceneManager.LoadScene("PingPong");
    public void LoadReaction() => SceneManager.LoadScene("Reaction");
    public void ExitGame() => Application.Quit();

    public void ToggleHandTracking()
    {
        isHandTrackingActive = !isHandTrackingActive;
        PlayerPrefs.SetInt("HandTracking", isHandTrackingActive ? 1 : 0);
        PlayerPrefs.Save();
        if (HandTrackingObject != null)
        {
            HandTrackingObject.SetActive(isHandTrackingActive);
        }
    }

    public void ToggleBackground()
    {
        if (PanelBG != null)
        {
            isBackgroundEnabled = !isBackgroundEnabled;
            PlayerPrefs.SetInt("Background", isBackgroundEnabled ? 1 : 0);
            PlayerPrefs.Save();
            PanelBG.color = isBackgroundEnabled ? new Color(PanelBG.color.r, PanelBG.color.g, PanelBG.color.b, 1f) : new Color(PanelBG.color.r, PanelBG.color.g, PanelBG.color.b, 0.7f);
        }
    }

    public void ToggleHandTrackingMarker()
    {
        if (HandTrackingMarker != null)
        {
            isTrackersEnabled = !isTrackersEnabled;
            PlayerPrefs.SetInt("Trackers", isTrackersEnabled ? 1 : 0);
            PlayerPrefs.Save();
            HandTrackingMarker.SetActive(isTrackersEnabled);
        }
    }
}