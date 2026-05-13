using Mediapipe.Unity.Sample.HandLandmarkDetection;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int player1Wins;
    public int player2Wins;
    public int draws;

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

    public void AddWin(int player)
    {
        if (player == 1) player1Wins++;
        else if (player == 2) player2Wins++;
    }

    public void AddDraw()
    {
        draws++;
    }
}