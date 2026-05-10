using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReactionUIManager : MonoBehaviour
{
    public static ReactionUIManager Instance { get; private set; }

    [Header("Screens")]
    public GameObject mainMenuScreen;
    public GameObject level1Screen;
    public GameObject level2Screen;
    public GameObject statsScreen;

    [Header("Main Menu")]
    public Button btnLevel1;
    public Button btnLevel2;
    public Button btnQuit;

    [Header("Stats Screen")]
    public TMP_Text txtCurrentLevel;
    public TMP_Text txtTotalColored;
    public TMP_Text txtHitBoxes;
    public TMP_Text txtBestScore;
    public TMP_Text txtCurrentScore;
    public Button   btnPlayAgain;
    public Button   btnNextLevel;
    public Button   btnMainMenu;

    [Header("Round HUD (optional)")]
    public TMP_Text txtRoundHUD;

    [Header("Button Containers")]
    public Transform level1ButtonsContainer;
    public Transform level2ButtonsContainer;

    private ButtonController[] _level1Buttons;
    private ButtonController[] _level2Buttons;
    private HashSet<int> _tappedColoredIndices = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _level1Buttons = GetButtons(level1ButtonsContainer);
        _level2Buttons = GetButtons(level2ButtonsContainer);

        btnLevel1.onClick.AddListener(() => OnClickStartLevel(1));
        btnLevel2.onClick.AddListener(() => OnClickStartLevel(2));
        btnQuit.onClick.AddListener(OnClickQuit);

        btnPlayAgain.onClick.AddListener(OnClickPlayAgain);
        btnNextLevel.onClick.AddListener(OnClickNextLevel);
        btnMainMenu.onClick.AddListener(OnClickMainMenu);

        ReactionGameManager.Instance.OnRoundStarted   += HandleRoundStarted;
        ReactionGameManager.Instance.OnRoundEnded     += HandleRoundEnded;
        ReactionGameManager.Instance.OnLevelCompleted += HandleLevelCompleted;

        ShowScreen(mainMenuScreen);
    }

    void OnDestroy()
    {
        if (ReactionGameManager.Instance == null) return;
        ReactionGameManager.Instance.OnRoundStarted   -= HandleRoundStarted;
        ReactionGameManager.Instance.OnRoundEnded     -= HandleRoundEnded;
        ReactionGameManager.Instance.OnLevelCompleted -= HandleLevelCompleted;
    }

    void OnClickStartLevel(int level)
    {
        ShowLevelScreen(level);
        ReactionGameManager.Instance.StartLevel(level);
    }

    void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnClickPlayAgain()
    {
        int level = ReactionGameManager.Instance.CurrentLevel;
        ShowLevelScreen(level);
        ReactionGameManager.Instance.StartLevel(level);
    }

    void OnClickNextLevel()
    {
        int nextLevel = ReactionGameManager.Instance.CurrentLevel + 1;
        ShowLevelScreen(nextLevel);
        ReactionGameManager.Instance.StartLevel(nextLevel);
    }

    void OnClickMainMenu()
    {
        ShowScreen(mainMenuScreen);
    }

    void HandleRoundStarted(List<int> coloredIndices)
    {
        _tappedColoredIndices.Clear();

        ButtonController[] buttons = ActiveButtons();

        foreach (var btn in buttons)
            btn.SetNeutral();

        foreach (int idx in coloredIndices)
        {
            if (idx < buttons.Length)
                buttons[idx].SetColored();
        }

        if (txtRoundHUD != null)
            txtRoundHUD.text = $"Round {ReactionGameManager.Instance.CurrentRound} / {ReactionGameManager.Instance.TotalRounds}";
    }

    void HandleRoundEnded()
    {
        ButtonController[] buttons = ActiveButtons();
        List<int> colored = ReactionGameManager.Instance.ColoredIndicesThisRound;

        foreach (var btn in buttons)
            btn.SetNeutral();

        foreach (int idx in colored)
        {
            if (idx < buttons.Length)
                buttons[idx].SetMissed();
        }
    }

    public void HandleLevelCompleted()
    {
        PopulateStatsScreen();
        ShowScreen(statsScreen);
    }

    public void OnButtonTapped(int index, bool wasColored)
    {
        if (wasColored)
        {
            _tappedColoredIndices.Add(index);
            ActiveButtons()[index].SetHit();
        }
        else
        {
            ActiveButtons()[index].SetWrong();
        }

        ReactionGameManager.Instance.RegisterHit(index, wasColored);
    }

    public bool AllColoredTapped(List<int> coloredIndices)
    {
        foreach (int idx in coloredIndices)
            if (!_tappedColoredIndices.Contains(idx)) return false;
        return true;
    }

    void ShowScreen(GameObject target)
    {
        if (mainMenuScreen != null) mainMenuScreen.SetActive(target == mainMenuScreen);
        if (level1Screen != null) level1Screen.SetActive(target == level1Screen);
        if (level2Screen != null) level2Screen.SetActive(target == level2Screen);
        if (statsScreen != null) statsScreen.SetActive(target == statsScreen);
    }

    void ShowLevelScreen(int level)
    {
        ShowScreen(level == 1 ? level1Screen : level2Screen);
    }

    ButtonController[] ActiveButtons()
    {
        return ReactionGameManager.Instance.CurrentLevel == 1 ? _level1Buttons : _level2Buttons;
    }

    static ButtonController[] GetButtons(Transform container)
    {
        if (container == null)
        {
            Debug.LogWarning("[ReactionUIManager] Button container não atribuído no Inspector!");
            return new ButtonController[0];
        }
        return container.GetComponentsInChildren<ButtonController>();
    }

    void PopulateStatsScreen()
    {
        var gm = ReactionGameManager.Instance;

        txtCurrentLevel.text  = $"Level {gm.CurrentLevel}";
        txtTotalColored.text  = $"Total Colored Boxes: {gm.TotalColored}";
        txtHitBoxes.text      = $"Boxes Hit: {gm.TotalHit}";
        txtBestScore.text     = $"Best Score: {gm.BestScore:F0}";
        txtCurrentScore.text  = $"Score: {gm.CurrentScore:F0}";

        btnNextLevel.gameObject.SetActive(gm.HasNextLevel());
    }
}