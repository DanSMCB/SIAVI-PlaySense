using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject HandTrackingMarker;
    public GameObject HandTrackingObject;
    public GameObject HandTrackingObject2;
    public Image PanelBG;
    private bool isHandTrackingActive;
    private bool isTrackersEnabled;
    private bool isBackgroundEnabled;
    public Toggle HandTrackingToggle;
    public Toggle CameraToggle;
    public Toggle LandmarkToggle;

    public GameObject MenuObject;
    public GameObject WinScreen;
    public GameObject ColoursMenu;
    public HandPaddleInputBridge HandPaddleInputBridge;

    void Start()
    {
        LoadModuleSettings();

        if (HandTrackingObject != null)
        {
            HandTrackingObject.SetActive(isHandTrackingActive);
            HandTrackingToggle.SetIsOnWithoutNotify(isHandTrackingActive);
        }
        if (HandTrackingObject2 != null) {
            HandTrackingObject2.SetActive(isHandTrackingActive);
        }
        if (HandTrackingMarker != null)
        {
            HandTrackingMarker.SetActive(isTrackersEnabled);
            LandmarkToggle.SetIsOnWithoutNotify(isTrackersEnabled);
        }
        if (PanelBG != null)
        {
            PanelBG.color = isBackgroundEnabled ? new Color(PanelBG.color.r, PanelBG.color.g, PanelBG.color.b, 1f) : new Color(PanelBG.color.r, PanelBG.color.g, PanelBG.color.b, 0.7f);
            CameraToggle.SetIsOnWithoutNotify(!isBackgroundEnabled);
        }

        ActivateCameraSettings();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnMenu();
        }
    }

    void LoadModuleSettings()
    {
        isHandTrackingActive = PlayerPrefs.GetInt("HandTracking", 0) == 1;
        isTrackersEnabled = PlayerPrefs.GetInt("Trackers", 0) == 1;
        isBackgroundEnabled = PlayerPrefs.GetInt("Background", 0) == 1;
    }

    public void LoadMainMenu() => SceneManager.LoadScene("Main Menu");
    public void StartGame()
    {
        MenuObject.SetActive(false);
        HandTrackingObject.SetActive(false);
        HandTrackingObject2.SetActive(false);
        ColoursMenu.SetActive(false);
        HandPaddleInputBridge.SetHandTrackingActive(isHandTrackingActive);
        HandPaddleInputBridge.ResetGame();
    }

    public void ReturnMenu()
    {
        MenuObject.SetActive(true);
        HandTrackingObject.SetActive(isHandTrackingActive);
        HandTrackingObject2.SetActive(isHandTrackingActive);
        WinScreen.SetActive(false);
        ColoursMenu.SetActive(false);
        HandPaddleInputBridge.StopGame();
    }

    public void OpenColoursMenu()
    {
        ColoursMenu.SetActive(true);
        MenuObject.SetActive(false);
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
        if (HandTrackingObject2 != null)
        {
            HandTrackingObject2.SetActive(isHandTrackingActive);
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
            CameraToggle.SetIsOnWithoutNotify(false);
            isBackgroundEnabled = true;
            PanelBG.color = new Color(PanelBG.color.r, PanelBG.color.g, PanelBG.color.b, 1f);
            PlayerPrefs.SetInt("Trackers", isTrackersEnabled ? 1 : 0);
            PlayerPrefs.Save();

            LandmarkToggle.interactable = false;
            LandmarkToggle.SetIsOnWithoutNotify(false);
            isTrackersEnabled = false;
            HandTrackingMarker.SetActive(isTrackersEnabled);
            PlayerPrefs.SetInt("Background", isBackgroundEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}