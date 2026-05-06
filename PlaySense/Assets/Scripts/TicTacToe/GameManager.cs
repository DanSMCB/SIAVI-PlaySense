using Mediapipe.Unity.Sample.HandLandmarkDetection;
using UnityEngine;
using UnityEngine.Rendering;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public HandLandmarkerRunner handRunner;
    public GameObject handTrack;
    public GameObject handCursor;

    public int player1Wins;
    public int player2Wins;
    public int draws;

    bool handTracking = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        handCursor.SetActive(false);
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
        handTrack.SetActive(true);

        handTracking = !handTracking;

        if (handTracking)
        {
            handCursor.SetActive(true);
            handRunner.Play();
        }
        else
        {
            handCursor.SetActive(false);
            handRunner.Stop();
        }

        
    }
}