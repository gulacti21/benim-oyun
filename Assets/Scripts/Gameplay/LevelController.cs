using UnityEngine;
using UnityEngine.InputSystem;

public class LevelController : MonoBehaviour
{
    public enum LevelState
    {
        Playing,
        Won,
        Lost
    }

    [SerializeField] private LevelData level;
    [SerializeField] private CircleArena arena;
    [SerializeField] private ShotController shooter;
    [SerializeField] private float settleDelay = 0.4f;

    private int shotsUsed;
    private bool waitingForSettle;
    private float settleTimer;

    public LevelState State { get; private set; } = LevelState.Playing;
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

    public void RestartLevel()
    {
        if (level == null)
        {
            Debug.LogError("[LevelController] Level data is not assigned.");
            return;
        }

        if (arena != null)
        {
            arena.Configure(level.circleRadius, level.rings);
            arena.Rebuild();
        }

        if (shooter != null)
        {
            shooter.ResetTo(level.shooterStartPosition);
            shooter.ShootingEnabled = true;
        }

        shotsUsed = 0;
        waitingForSettle = false;
        settleTimer = 0f;
        State = LevelState.Playing;
    }

    private void HandleShotFired()
    {
        shotsUsed++;
        waitingForSettle = true;
        settleTimer = 0f;
    }

    private void UpdateSettle()
    {
        if (!waitingForSettle)
        {
            return;
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

            if (shooter != null)
            {
                shooter.ShootingEnabled = false;
            }

            return;
        }

        if (shooter != null)
        {
            shooter.ResetTo(level.shooterStartPosition);
        }
    }
}
