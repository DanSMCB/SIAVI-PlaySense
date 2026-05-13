using Mediapipe.Unity.Sample.HandLandmarkDetection;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public HandLandmarkerRunner handRunner;
    //public HandLandmarkerDualUiBridge bridge;
    public GameObject handTrack;

    public int player1Wins;
    public int player2Wins;
    public int draws;

    bool handTracking = false;

    [SerializeField] private UnityEngine.UI.Toggle handTrackingToggle;
    [SerializeField] private GameObject cursor1;
    [SerializeField] private GameObject cursor2;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        handRunner = Object.FindFirstObjectByType<HandLandmarkerRunner>();
        //bridge = FindFirstObjectByType<HandLandmarkerDualUiBridge>();

        StartCoroutine(SetupHandTracking());
    }

    IEnumerator SetupHandTracking()
    {
        while (handRunner == null)
        {
            handRunner = Object.FindFirstObjectByType<HandLandmarkerRunner>();
            yield return null;
        }

        yield return null;

        if (PlayerPrefs.GetInt("HandTracking", 0) == 1)
        {
            handTrackingToggle.isOn = true;
            handTracking = true;
            handRunner.Play();
        }
        else
        {
            handRunner.Stop();
        }
    }

    public void AddWin(int player)
    {
        if (player == 1) player1Wins++;
        else if (player == 2) player2Wins++;
    }

    public void AddDraw()
    {
        draws++;
    }

    public void ToggleHandTracking()
    {
        handTracking = !handTracking;

        if (handTracking)
        {
            //bridge.enabled = true;
            cursor1.SetActive(true);
            cursor2.SetActive(true);
            handRunner.Play();
        }
        else
        {
            //bridge.enabled = false;
            cursor1.SetActive(false);
            cursor2.SetActive(false);
            handRunner.Stop();
        }
    }
}