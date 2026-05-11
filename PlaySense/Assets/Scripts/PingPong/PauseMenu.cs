using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject HandTrackingMarker;
    public GameObject HandTrackingObject;
    public Image PanelBG;
    private bool isHandTrackingActive = false;
    private bool isTrackersEnabled = false;
    private bool isBackgroundEnabled = false;
    public Toggle HandTrackingToggle;
    public Toggle CameraToggle;
    public Toggle LandmarkToggle;

    public GameObject MenuObject;
    public GameObject WinScreen;
    public HandPaddleInputBridge HandPaddleInputBridge;

    void Start()
    {

        isHandTrackingActive = PlayerPrefs.GetInt("HandTracking", 0) == 1;
        isTrackersEnabled = PlayerPrefs.GetInt("Trackers", 0) == 1;
        isBackgroundEnabled = PlayerPrefs.GetInt("Background", 0) == 1;

        if (HandTrackingObject != null)
        {
            if (isHandTrackingActive)  
                HandTrackingObject.SetActive(true);
            else
                HandTrackingObject.SetActive(false);
            HandTrackingToggle.isOn = isHandTrackingActive;
        }
        if (HandTrackingMarker != null)
        {
            HandTrackingMarker.SetActive(isTrackersEnabled);
            LandmarkToggle.isOn = isTrackersEnabled;
        }
        if (PanelBG != null)
        {
            PanelBG.color = isBackgroundEnabled ? new Color(PanelBG.color.r, PanelBG.color.g, PanelBG.color.b, 1f) : new Color(PanelBG.color.r, PanelBG.color.g, PanelBG.color.b, 0.3f);
            CameraToggle.isOn = !isBackgroundEnabled;
        }

        ActivateCameraSettings();
    }

    public void LoadMainMenu() => SceneManager.LoadScene("Main Menu");
    public void StartGame()
    {
        MenuObject.SetActive(false);
        HandTrackingObject.SetActive(false);
        HandPaddleInputBridge.SetHandTrackingActive(isHandTrackingActive);
        HandPaddleInputBridge.ResetGame();
    }

    public void ReturnMenu()
    {
        MenuObject.SetActive(true);
        HandTrackingObject.SetActive(isHandTrackingActive);
        WinScreen.SetActive(false);
    }

    public void ToggleHandTracking()
    {
        isHandTrackingActive = !isHandTrackingActive;
        PlayerPrefs.SetInt("HandTracking", isHandTrackingActive ? 1 : 0);
        PlayerPrefs.Save();
        if (HandTrackingObject != null)
        {
            HandTrackingObject.SetActive(isHandTrackingActive);
        }
        ActivateCameraSettings();
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

    public void ActivateCameraSettings()
    {
        if (isHandTrackingActive)
        {
            CameraToggle.interactable = true;
            LandmarkToggle.interactable = true;
        }
        else
        {
            CameraToggle.interactable = false;
            CameraToggle.isOn = false;
            isBackgroundEnabled = true;
            PanelBG.color = new Color(PanelBG.color.r, PanelBG.color.g, PanelBG.color.b, 1f);
            PlayerPrefs.SetInt("Trackers", isTrackersEnabled ? 1 : 0);
            PlayerPrefs.Save();

            LandmarkToggle.interactable = false;
            LandmarkToggle.isOn = false;
            isTrackersEnabled = false;
            HandTrackingMarker.SetActive(isTrackersEnabled);
            PlayerPrefs.SetInt("Background", isBackgroundEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}