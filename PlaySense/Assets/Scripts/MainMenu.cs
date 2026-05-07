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

    public void LoadTicTacToe()
    {
        SceneManager.LoadScene("TicTacToe");
    }

    public void LoadPong()
    {
        SceneManager.LoadScene("PingPong");
    }

    public void LoadGame3()
    {
        SceneManager.LoadScene("Game3");
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void ToggleHandTracking()
    {
        isHandTrackingActive = !isHandTrackingActive;
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
            PanelBG.color = isBackgroundEnabled ? new Color(PanelBG.color.r, PanelBG.color.g, PanelBG.color.b, 1f) : new Color(PanelBG.color.r, PanelBG.color.g, PanelBG.color.b, 0.7f);
        }
    }

    public void ToggleHandTrackingMarker()
    {
        if (HandTrackingMarker != null)
        {
            isTrackersEnabled = !isTrackersEnabled;
            HandTrackingMarker.SetActive(isTrackersEnabled);
        }
    }
}