using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHudView : MonoBehaviour
{
    [Header("Controller")]
    [SerializeField] private LevelController controller;

    [Header("Top Bar")]
    [SerializeField] private TextMeshProUGUI levelLabel;
    [SerializeField] private TextMeshProUGUI scoreLabel;
    [SerializeField] private TextMeshProUGUI shotsLabel;
    [SerializeField] private Button pauseButton;

    [Header("Pause Panel")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseRestartButton;
    [SerializeField] private Button pauseMapButton;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTitle;
    [SerializeField] private TextMeshProUGUI resultScore;
    [SerializeField] private UnityEngine.UI.Image[] starIcons;
    [SerializeField] private Color starOnColor = new Color(0.96f, 0.74f, 0.26f, 1f);
    [SerializeField] private Color starOffColor = new Color(1f, 1f, 1f, 0.14f);
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button resultMapButton;

    private static readonly System.Globalization.CultureInfo TurkishCulture = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

    private int cachedScore = -1;
    private int cachedTotal = -1;
    private int cachedShots = -1;

    private void Awake()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(OnPausePressed);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResumePressed);
        }

        if (pauseRestartButton != null)
        {
            pauseRestartButton.onClick.AddListener(OnRestartPressed);
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRestartPressed);
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextPressed);
        }

        if (pauseMapButton != null)
        {
            pauseMapButton.onClick.AddListener(OnMapPressed);
        }

        if (resultMapButton != null)
        {
            resultMapButton.onClick.AddListener(OnMapPressed);
        }
    }

    private void OnEnable()
    {
        if (controller != null)
        {
            controller.StateChanged += RefreshPanels;
        }
    }

    private void OnDisable()
    {
        if (controller != null)
        {
            controller.StateChanged -= RefreshPanels;
        }
    }

    private void Start()
    {
        RefreshPanels();
        RefreshLevelLabel();
    }

    private void Update()
    {
        if (controller == null)
        {
            return;
        }

        if (controller.Score != cachedScore || controller.TotalMarbles != cachedTotal)
        {
            cachedScore = controller.Score;
            cachedTotal = controller.TotalMarbles;

            if (scoreLabel != null)
            {
                scoreLabel.SetText(cachedScore + " / " + cachedTotal);
            }
        }

        if (controller.ShotsLeft != cachedShots)
        {
            cachedShots = controller.ShotsLeft;

            if (shotsLabel != null)
            {
                shotsLabel.SetText(cachedShots.ToString());
            }
        }
    }

    private void RefreshLevelLabel()
    {
        if (levelLabel == null || controller == null)
        {
            return;
        }

        string name = controller.Level != null ? controller.Level.levelName : "Seviye";
        levelLabel.SetText(("Seviye " + (controller.LevelIndex + 1) + "  \u2022  " + name).ToUpper(TurkishCulture));
    }

    private void RefreshPanels()
    {
        if (controller == null)
        {
            return;
        }

        RefreshLevelLabel();

        bool finished = controller.State != LevelController.LevelState.Playing;

        if (pausePanel != null)
        {
            pausePanel.SetActive(controller.IsPaused && !finished);
        }

        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(!finished);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(finished);
        }

        if (!finished)
        {
            return;
        }

        bool won = controller.State == LevelController.LevelState.Won;

        if (resultTitle != null)
        {
            resultTitle.SetText(won ? "KAZANDIN" : "KAYBETTİN");
        }

        if (resultScore != null)
        {
            resultScore.SetText(controller.Score + " / " + controller.TotalMarbles + " misket çıkardın");
        }

        if (starIcons != null)
        {
            int stars = controller.Stars;

            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] != null)
                {
                    starIcons[i].color = i < stars ? starOnColor : starOffColor;
                }
            }
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(won && controller.HasNextLevel);
        }
    }

    private void OnPausePressed()
    {
        if (controller != null)
        {
            controller.SetPaused(true);
        }
    }

    private void OnResumePressed()
    {
        if (controller != null)
        {
            controller.SetPaused(false);
        }
    }

    private void OnRestartPressed()
    {
        if (controller != null)
        {
            controller.SetPaused(false);
            controller.RestartLevel();
        }
    }

    private void OnNextPressed()
    {
        if (controller != null)
        {
            controller.LoadNextLevel();
        }
    }

    private void OnMapPressed()
    {
        if (controller != null)
        {
            controller.OpenLevelSelect();
        }
    }
}
