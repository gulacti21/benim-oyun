using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LevelController : MonoBehaviour
{
    public enum LevelState
    {
        Playing,
        Won,
        Lost
    }

    [SerializeField] private LevelDatabase database;
    [SerializeField] private LevelData level;
    [SerializeField] private MarbleArena arena;
    [SerializeField] private ShotController shooter;
    [SerializeField] private float settleDelay = 0.4f;

    private int shotsUsed;
    private bool waitingForSettle;
    private float settleTimer;
    private float shotElapsed;
    public RoundReward LastReward { get; private set; }
    public ShotController Shooter => shooter;
    public bool WaitingForSettle => waitingForSettle;
    private void Awake() { database = Campaign.Database; }

    public event Action StateChanged;

    public LevelState State { get; private set; } = LevelState.Playing;
    public bool IsPaused { get; private set; }
    public int LevelIndex { get; private set; }
    public bool HasNextLevel => database != null && LevelIndex + 1 < database.Count;
    public LevelData Level => level;
    public int ShotsUsed => shotsUsed;
    public int ShotsLeft => level != null ? Mathf.Max(0, level.shotCount - shotsUsed) : 0;
    public int Score => arena != null ? arena.Score : 0;
    public int TotalMarbles => arena != null ? arena.TotalMarbles : 0;
    public int Stars => level != null ? level.GetStars(Score) : 0;

    private void OnEnable()
    {
        if (shooter != null)
        {
            shooter.ShotFired += HandleShotFired;
        }
    }

    private void OnDisable()
    {
        if (shooter != null)
        {
            shooter.ShotFired -= HandleShotFired;
        }
    }

    private void Start()
    {
        RestartLevel();
    }

    private void Update()
    {
        if (State == LevelState.Playing)
        {
            UpdateSettle();
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            RestartLevel();
        }
    }

    public void SetPaused(bool paused)
    {
        if (State != LevelState.Playing && paused)
        {
            return;
        }

        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (shooter != null)
        {
            shooter.ShootingEnabled = !paused && !waitingForSettle && State == LevelState.Playing;
        }

        StateChanged?.Invoke();
    }

    public void TogglePause()
    {
        SetPaused(!IsPaused);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    public void LoadNextLevel()
    {
        if (!HasNextLevel)
        {
            return;
        }

        Time.timeScale = 1f;
        GameSession.SelectedLevelIndex = LevelIndex + 1;
        SceneManager.LoadScene(GameSession.GameSceneName);
    }

    public void OpenLevelSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameSession.LevelSelectSceneName);
    }

    public void RestartLevel()
    {
        LevelIndex = Mathf.Clamp(GameSession.SelectedLevelIndex, 0, database.Count - 1);
        if (!MahalleProfile.Unlocked(LevelIndex)) LevelIndex = MahalleProfile.NextLevel;

        if (database != null)
        {
            LevelData fromDatabase = database.Get(LevelIndex);

            if (fromDatabase != null)
            {
                level = fromDatabase;
            }
        }

        if (level == null)
        {
            Debug.LogError("[LevelController] Level data is not assigned.");
            return;
        }

        if (arena != null)
        {
            arena.Configure(level.shape, level.arenaSize, level.rings, level.triangleRows);
            arena.Rebuild();
        }

        if (shooter != null)
        {
            shooter.ResetTo(level.shooterStartPosition);
            shooter.ShootingEnabled = true;
        }

        MahalleWorld.Apply(this);
        LastReward = new RoundReward();
        shotsUsed = 0;
        waitingForSettle = false;
        settleTimer = 0f;
        State = LevelState.Playing;
        IsPaused = false;
        Time.timeScale = 1f;
        StateChanged?.Invoke();
    }

    private void HandleShotFired()
    {
        shotsUsed++;
        shotElapsed = 0f;
        waitingForSettle = true;
        settleTimer = 0f;
    }

    private void UpdateSettle()
    {
        if (!waitingForSettle)
        {
            return;
        }

        if (IsPaused) return;
        shotElapsed += Time.deltaTime;
        if (shotElapsed > 12f)
        {
            foreach (var b in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None)) { b.linearVelocity = Vector3.zero; b.angularVelocity = Vector3.zero; b.Sleep(); }
        }
        bool shooterResting = shooter == null || shooter.AtRest;
        bool marblesResting = arena == null || arena.AllMarblesAtRest();

        if (!shooterResting || !marblesResting)
        {
            settleTimer = 0f;
            return;
        }

        settleTimer += Time.deltaTime;

        if (settleTimer < settleDelay)
        {
            return;
        }

        waitingForSettle = false;
        EvaluateTurn();
    }

    private void EvaluateTurn()
    {
        bool allMarblesOut = arena != null && arena.RemainingMarbles == 0;
        bool outOfShots = ShotsLeft <= 0;

        if (allMarblesOut || outOfShots)
        {
            State = Score >= level.oneStarTarget ? LevelState.Won : LevelState.Lost;

            if (State == LevelState.Won)
            {
                // Rewards and unlocks are persisted together by the campaign profile.
            }

            LastReward = MahalleProfile.Finish(LevelIndex, State == LevelState.Won ? Stars : 0, Score);

            if (SfxPlayer.Instance != null)
            {
                if (State == LevelState.Won)
                {
                    SfxPlayer.Instance.PlayWin();
                }
                else
                {
                    SfxPlayer.Instance.PlayLose();
                }
            }

            if (shooter != null)
            {
                shooter.ShootingEnabled = false;
            }

            StateChanged?.Invoke();
            return;
        }

        if (shooter != null)
        {
            shooter.ResetTo(level.shooterStartPosition);
            shooter.ShootingEnabled = true;
        }
        StateChanged?.Invoke();
    }
}
