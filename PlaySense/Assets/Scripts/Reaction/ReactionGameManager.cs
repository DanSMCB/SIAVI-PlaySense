using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using UnityEngine.SceneManagement;

public class ReactionGameManager : MonoBehaviour
{
    public static ReactionGameManager Instance { get; private set; }

    [Header("Modules")]
    public bool useVoiceMode;
    public bool useHandTracking;
    public bool useAnnotations;
    public bool useBackground;
    [Space]
    public GameObject audioModule;
    public GameObject mediapipeModule;
    public HandLandmarkerRunner handRunner;
    public GameObject handAnnotations;
    public GameObject handCursor;
    public UnityEngine.UI.Image[] backgrounds;
    public GameObject cameraScreen;

    [Header("Level Configuration")]
    public int level1ButtonCount = 6;
    public int level2ButtonCount = 9;
    public int maxRoundsPerLevel = 10;

    [Header("Round Configuration")]
    public int minColoredPerRound = 1;
    public int maxColoredPerRound = 4;
    public float reactionWindowSeconds = 3f;

    public int CurrentLevel { get; private set; }
    public int TotalRounds { get; private set; }
    public int CurrentRound { get; private set; }
    public int TotalColored { get; private set; }
    public int TotalHit { get; private set; }
    public float CurrentScore { get; private set; }
    public float BestScore { get; private set; }

    public List<int> ColoredIndicesThisRound { get; private set; } = new();
    public bool RoundActive { get; private set; }

    public event System.Action<List<int>> OnRoundStarted;
    public event System.Action OnRoundEnded;
    public event System.Action OnLevelCompleted;

    [Header("Scoring")]
    public float scorePerHit = 100f;
    public float reactionTimeBonus = 50f;
    private float _roundStartTime;

    private Coroutine _reactionCoroutine;
    private bool _handTracking = false;
    private bool _speechActive = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BestScore = PlayerPrefs.GetFloat("BestScore", 0f);

        if (handCursor != null) handCursor.SetActive(false);
        if ( handAnnotations != null) handAnnotations.SetActive(false);
    }

    void Start()
    {
        LoadModuleSettings();

        ConfigureModules();

        ApplyVisualSettings();

        if (useHandTracking)
            StartCoroutine(DelayedHandTrackingStart());

        Debug.Log($"HandTracking: {useHandTracking}, Annotations: {PlayerPrefs.GetInt("Trackers", 0)}");
    }

    void LoadModuleSettings()
    {
        useHandTracking = PlayerPrefs.GetInt("HandTracking", 0) == 1;
        useVoiceMode = PlayerPrefs.GetInt("VoiceMode", 0) == 1;
        useAnnotations = PlayerPrefs.GetInt("Trackers", 0) == 1;
        useBackground = PlayerPrefs.GetInt("Background", 0) == 1;
    }

    void ConfigureModules()
    {
        if (mediapipeModule != null)
            mediapipeModule.SetActive(useHandTracking);

        if (audioModule != null)
        {
            audioModule.SetActive(useVoiceMode);
            if (useVoiceMode)
            {
                var sm = audioModule.GetComponent<ReactionGoogleSpeechManager>();
                if (sm != null) sm.ToggleVoiceMode();
            }
        }
    }

    void ApplyVisualSettings()
    {
        if ( handAnnotations != null)
            handAnnotations.SetActive(useAnnotations);

        if (backgrounds != null)
            foreach (var bg in backgrounds)
                if (bg != null) bg.enabled = useBackground;

        if (cameraScreen != null)
            cameraScreen.SetActive(!useBackground);
    }

    IEnumerator DelayedHandTrackingStart()
    {
        yield return new WaitForSeconds(1f);

        if (mediapipeModule != null && mediapipeModule.activeSelf)
        {
            _handTracking = true;
            if (handCursor != null) handCursor.SetActive(true);

            if (handRunner != null) handRunner.Play();

            if ( handAnnotations != null)
                handAnnotations.SetActive(useAnnotations);
        }
    }

    void ApplyNonMediaPipeSettings()
    {
        

        if ( handAnnotations != null) handAnnotations.SetActive(useAnnotations);

        if (backgrounds != null)
            foreach (var bg in backgrounds)
                if (bg != null) bg.enabled = useBackground;

        if (cameraScreen != null)
            cameraScreen.SetActive(!useBackground);
    }

    public void ToggleHandTracking()
    {
        _handTracking = !_handTracking;

        if (_handTracking)
        {
            if ( handAnnotations != null) handAnnotations.SetActive(true);
            if (handCursor != null) handCursor.SetActive(true);
            if (handRunner != null) handRunner.Play();
        }
        else
        {
            if (handCursor != null) handCursor.SetActive(false);
            if (handRunner != null) handRunner.Stop();
        }
    }

    public void ToggleSpeech()
    {
        _speechActive = !_speechActive;

        if (audioModule != null)
        {
            var speechManager = audioModule.GetComponent<ReactionGoogleSpeechManager>();
            if (speechManager != null)
                speechManager.ToggleVoiceMode();
        }

        Debug.Log($"[ReactionGameManager] Speech {(_speechActive ? "ON" : "OFF")}");
    }

    public void GoBack()
    {
        SceneManager.LoadScene("Main Menu");
    }

    public void StartLevel(int level)
    {
        CurrentLevel = level;
        TotalRounds = Random.Range(3, maxRoundsPerLevel + 1);
        CurrentRound = 0;
        TotalColored = 0;
        TotalHit = 0;
        CurrentScore = 0f;

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
        }
        else
        {
            CurrentScore = Mathf.Max(0f, CurrentScore - scorePerHit * 0.5f);
        }

        if (AllColoredHit()) EndRound();
    }

    void StartNextRound()
    {
        CurrentRound++;
        if (CurrentRound > TotalRounds) { FinishLevel(); return; }

        int totalButtons = CurrentLevel == 1 ? level1ButtonCount : level2ButtonCount;
        int maxAllowed = Mathf.Min(maxColoredPerRound, totalButtons);
        int colored = Random.Range(minColoredPerRound, maxAllowed + 1);

        ColoredIndicesThisRound = PickRandomIndices(totalButtons, colored);
        TotalColored += colored;

        RoundActive = true;
        _roundStartTime = Time.time;

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

        OnLevelCompleted?.Invoke();
    }

    bool AllColoredHit()
    {
        return ReactionUIManager.Instance != null
            && ReactionUIManager.Instance.AllColoredTapped(ColoredIndicesThisRound);
    }

    public void OnPlayAgain()
    {
        ReactionGameManager.Instance.StartLevel(ReactionGameManager.Instance.CurrentLevel);
        ReactionUIManager.Instance.HandleLevelCompleted();
    }

    public void OnNextLevel()
    {
        if (ReactionGameManager.Instance.HasNextLevel())
            ReactionGameManager.Instance.StartLevel(2);
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

    public bool HasNextLevel() => CurrentLevel < 2;
    public void ResetBestScore() { BestScore = 0; PlayerPrefs.DeleteKey("BestScore"); }
}