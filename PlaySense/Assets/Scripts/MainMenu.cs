using Mediapipe.Unity.Sample.HandLandmarkDetection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public GameObject HandTrackingMarker;
    public GameObject HandTrackingObject;
    public Image PanelBG;
    public GameObject SpeechModule;
    private bool isHandTrackingActive;
    private bool isTrackersEnabled;
    private bool isBackgroundEnabled;
    private bool isSpeechEnabled;
    public Toggle HandTrackingToggle;
    public Toggle CameraToggle;
    public Toggle LandmarkToggle;
    public Toggle VoiceToggle;

    public HandLandmarkerRunner handRunner;

    void Start()
    {
        LoadModuleSettings();

        if (HandTrackingObject != null)
        {
            HandTrackingObject.SetActive(isHandTrackingActive);
            HandTrackingToggle.SetIsOnWithoutNotify(isHandTrackingActive);
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
        if (SpeechModule != null)
        {
            SpeechModule.SetActive(isSpeechEnabled);
            VoiceToggle.SetIsOnWithoutNotify(isSpeechEnabled);
        }

        if (handRunner != null)
        {
            StartCoroutine(InitHandRunner());
        }

        ActivateCameraSettings();
    }



    private System.Collections.IEnumerator InitHandRunner()
    {
        yield return null;

        bool enabled = PlayerPrefs.GetInt("HandTracking", 0) == 1;

        if (!enabled)
        {
            handRunner.Stop();
        }
        else
        {
            handRunner.Play();
        }
    }

    void LoadModuleSettings()
    {
        isHandTrackingActive = PlayerPrefs.GetInt("HandTracking", 0) == 1;
        isTrackersEnabled = PlayerPrefs.GetInt("Trackers", 0) == 1;
        isBackgroundEnabled = PlayerPrefs.GetInt("Background", 0) == 1;
        isSpeechEnabled = PlayerPrefs.GetInt("VoiceMode", 0) == 1;
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
            if (isHandTrackingActive)
                handRunner.Play();
            else
                handRunner.Stop();
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

    public void ToggleVoiceControl()
    {
        if (SpeechModule == null) { return; }

        isSpeechEnabled = !isSpeechEnabled;
        PlayerPrefs.SetInt("VoiceMode", isSpeechEnabled ? 1 : 0);
        PlayerPrefs.Save();
        SpeechModule.SetActive(isSpeechEnabled);
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