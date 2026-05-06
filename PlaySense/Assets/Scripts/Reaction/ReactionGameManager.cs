using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mediapipe.Unity.Sample.HandLandmarkDetection;

public class ReactionGameManager : MonoBehaviour
{
    public static ReactionGameManager Instance { get; private set; }

    public HandLandmarkerRunner handRunner;
    public GameObject handTrack;
    public GameObject handCursor;

    [Header("Level Configuration")]
    [Tooltip("Total buttons in Level 1")]
    public int level1ButtonCount = 6;
    [Tooltip("Total buttons in Level 2")]
    public int level2ButtonCount = 9;
    [Tooltip("Maximum rounds per level")]
    public int maxRoundsPerLevel = 10;

    [Header("Round Configuration")]
    [Tooltip("Min colored (target) buttons per round")]
    public int minColoredPerRound = 1;
    [Tooltip("Max colored (target) buttons per round (capped by total buttons)")]
    public int maxColoredPerRound = 4;
    [Tooltip("Seconds before hiding colored buttons (reaction window)")]
    public float reactionWindowSeconds = 3f;

    public int CurrentLevel    { get; private set; }
    public int TotalRounds     { get; private set; }
    public int CurrentRound    { get; private set; }
    public int TotalColored    { get; private set; }
    public int TotalHit        { get; private set; }
    public float CurrentScore  { get; private set; }
    public float BestScore     { get; private set; }

    public List<int> ColoredIndicesThisRound { get; private set; } = new();
    public bool RoundActive { get; private set; }

    public event System.Action<List<int>> OnRoundStarted;
    public event System.Action           OnRoundEnded;
    public event System.Action           OnLevelCompleted;

    [Header("Scoring")]
    public float scorePerHit       = 100f;
    public float reactionTimeBonus = 50f;
    private float _roundStartTime;

    [Header("Speech")]
    public GameObject speechRecognizer;

    private Coroutine _reactionCoroutine;
    private bool _handTracking = false;
    private bool _speechActive = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BestScore = PlayerPrefs.GetFloat("BestScore", 0f);
    }

    public void ToggleHandTracking()
    {
        if (handTrack != null) handTrack.SetActive(true);

        _handTracking = !_handTracking;

        if (_handTracking)
        {
            if (handCursor != null) handCursor.SetActive(true);
            if (handRunner != null) handRunner.Play();
            Debug.Log("[ReactionGameManager] Hand Tracking ON");
        }
        else
        {
            if (handCursor != null) handCursor.SetActive(false);
            if (handRunner != null) handRunner.Stop();
            Debug.Log("[ReactionGameManager] Hand Tracking OFF");
        }
    }

    public void ToggleSpeech()
    {
        _speechActive = !_speechActive;

        if (speechRecognizer != null)
            speechRecognizer.SetActive(_speechActive);

        Debug.Log($"[ReactionGameManager] Speech {(_speechActive ? "ON" : "OFF")}");
    }

    public void StartLevel(int level)
    {
        CurrentLevel   = level;
        TotalRounds    = Random.Range(3, maxRoundsPerLevel + 1);
        CurrentRound   = 0;
        TotalColored   = 0;
        TotalHit       = 0;
        CurrentScore   = 0f;

        Debug.Log($"[GameManager] Level {level} started — {TotalRounds} rounds");
        StartNextRound();
    }

    public void RegisterHit(int buttonIndex, bool wasColored)
    {
        if (!RoundActive) return;

        if (wasColored)
        {
            float reactionTime = Time.time - _roundStartTime;
            float bonus = Mathf.Max(0f, reactionTimeBonus - reactionTime * 10f);
            CurrentScore += scorePerHit + bonus;
            TotalHit++;
            Debug.Log($"[GameManager] HIT index={buttonIndex} | reaction={reactionTime:F2}s | bonus={bonus:F0}");
        }
        else
        {
            CurrentScore = Mathf.Max(0f, CurrentScore - scorePerHit * 0.5f);
            Debug.Log($"[GameManager] MISS (wrong button) index={buttonIndex}");
        }

        if (AllColoredHit())
            EndRound();
    }

    void StartNextRound()
    {
        CurrentRound++;
        if (CurrentRound > TotalRounds)
        {
            FinishLevel();
            return;
        }

        int totalButtons = CurrentLevel == 1 ? level1ButtonCount : level2ButtonCount;
        int maxAllowed   = Mathf.Min(maxColoredPerRound, totalButtons);
        int colored      = Random.Range(minColoredPerRound, maxAllowed + 1);

        ColoredIndicesThisRound = PickRandomIndices(totalButtons, colored);
        TotalColored += colored;

        RoundActive = true;
        _roundStartTime = Time.time;

        Debug.Log($"[GameManager] Round {CurrentRound}/{TotalRounds} — colored: [{string.Join(",", ColoredIndicesThisRound)}]");
        OnRoundStarted?.Invoke(ColoredIndicesThisRound);

        if (_reactionCoroutine != null) StopCoroutine(_reactionCoroutine);
        _reactionCoroutine = StartCoroutine(ReactionWindowCoroutine());
    }

    void EndRound()
    {
        if (!RoundActive) return;
        RoundActive = false;

        if (_reactionCoroutine != null) StopCoroutine(_reactionCoroutine);

        OnRoundEnded?.Invoke();
        Debug.Log($"[GameManager] Round {CurrentRound} ended | score so far: {CurrentScore:F0}");

        StartCoroutine(DelayedNextRound(1.2f));
    }

    IEnumerator ReactionWindowCoroutine()
    {
        yield return new WaitForSeconds(reactionWindowSeconds);
        if (RoundActive) EndRound();
    }

    IEnumerator DelayedNextRound(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartNextRound();
    }

    void FinishLevel()
    {
        RoundActive = false;

        if (CurrentScore > BestScore)
        {
            BestScore = CurrentScore;
            PlayerPrefs.SetFloat("BestScore", BestScore);
            PlayerPrefs.Save();
        }

        Debug.Log($"[GameManager] Level {CurrentLevel} complete! Score={CurrentScore:F0} Best={BestScore:F0}");
        OnLevelCompleted?.Invoke();
    }

    bool AllColoredHit()
    {
        return ReactionUIManager.Instance != null && ReactionUIManager.Instance.AllColoredTapped(ColoredIndicesThisRound);
    }

    static List<int> PickRandomIndices(int total, int count)
    {
        var pool = new List<int>();
        for (int i = 0; i < total; i++) pool.Add(i);

        var result = new List<int>();
        for (int i = 0; i < count; i++)
        {
            int r = Random.Range(0, pool.Count);
            result.Add(pool[r]);
            pool.RemoveAt(r);
        }
        return result;
    }

    public bool HasNextLevel()    => CurrentLevel < 2;
    public void ResetBestScore()  { BestScore = 0; PlayerPrefs.DeleteKey("BestScore"); }
}