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
    private int scoreAtShot;
    // ÇIKIŞ SÜRESİ: çemberden çıkan misket bu kadar saniye sonra durdurulur.
    // Atıcı çizgiye döner, çıkan hedefler olduğu yerde donar; haritanın sonuna
    // kadar yuvarlanmasını beklemek gerekmez.
    private const float OutsideStopDelay = 1.5f;
    private float shooterOutsideTime;
    private readonly System.Collections.Generic.Dictionary<TargetMarble, float> scoredTimes =
        new System.Collections.Generic.Dictionary<TargetMarble, float>();
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

    public MarbleArena Arena => arena;

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

        if (GameSession.EndlessMode)
        {
            EndlessWave = 1; EndlessScore = 0;
            level = EndlessLevel.Build(EndlessWave);
            LevelIndex = level.district * 12 + 5;   // sadece dekor için
        }
        else if (GameSession.DailyMode && !GameSession.TutorialMode && DailyLevel.Today() != null)
        {
            level = DailyLevel.Today();
            LevelIndex = level.district * 12 + 4; // dekor için; park köşeleri (0-2) dışında
        }

        if (GameSession.TutorialMode)
        {
            level = TutorialLevel.Get();
            LevelIndex = TutorialLevel.LevelIndex;
        }

        if (level == null)
        {
            Debug.LogError("[LevelController] Level data is not assigned.");
            return;
        }

        if (arena != null)
        {
            arena.Configure(level.shape, level.arenaSize, level.rings, level.triangleRows, level.marbles);
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
        scoredTimes.Clear();
        shooterOutsideTime = 0f;
        settleTimer = 0f;
        State = LevelState.Playing;
        IsPaused = false;
        Time.timeScale = 1f;
        StateChanged?.Invoke();
    }

    private void HandleShotFired()
    {
        shotsUsed++;
        scoreAtShot = Score;
        shooterOutsideTime = 0f;
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
        StopMarblesOutside();
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

    private void StopMarblesOutside()
    {
        if (arena == null) return;
        float dt = Time.deltaTime;

        if (shooter != null && shooter.Body != null && arena.IsOutside(shooter.transform.position))
        {
            shooterOutsideTime += dt;
            if (shooterOutsideTime >= OutsideStopDelay && !shooter.Body.isKinematic)
            {
                shooter.Body.linearVelocity = Vector3.zero;
                shooter.Body.angularVelocity = Vector3.zero;
                shooter.Body.Sleep();
            }
        }
        else shooterOutsideTime = 0f;

        // Çıkan hedef: hareket ettiği sürece sayaç işler, 1.5 sn dolunca
        // yerinde durdurulur. Çarpışması açık kalır; atıcı çarparsa misket
        // yine kayar ve sayaç baştan başlar.
        var list = arena.SpawnedMarbles;
        for (int i = 0; i < list.Count; i++)
        {
            var m = list[i];
            if (m == null || !m.IsScored || m.Body == null) continue;
            if (m.Body.IsSleeping() || m.Body.linearVelocity.magnitude < 0.05f) { scoredTimes.Remove(m); continue; }
            scoredTimes.TryGetValue(m, out float t);
            t += dt;
            if (t < OutsideStopDelay) { scoredTimes[m] = t; continue; }
            m.Body.linearVelocity = Vector3.zero;
            m.Body.angularVelocity = Vector3.zero;
            m.Body.Sleep();
            scoredTimes.Remove(m);
        }
    }

    private void EvaluateTurn()
    {
        if (GameSession.EndlessMode) { EvaluateEndlessTurn(); return; }
        if (GameSession.TutorialMode) { EvaluateTutorialTurn(); return; }
        MahalleProfile.RecordShot(Score - scoreAtShot);
        bool allMarblesOut = arena != null && arena.RemainingMarbles == 0;
        bool outOfShots = ShotsLeft <= 0;

        if (allMarblesOut || outOfShots)
        {
            State = Score >= level.oneStarTarget ? LevelState.Won : LevelState.Lost;

            if (State == LevelState.Won)
            {
                // Rewards and unlocks are persisted together by the campaign profile.
            }

            LastReward = GameSession.DailyMode
                ? MahalleProfile.FinishDaily(State == LevelState.Won ? Stars : 0, Score)
                : MahalleProfile.Finish(LevelIndex, State == LevelState.Won ? Stars : 0, Score);

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
            SettleShooter();
            shooter.ShootingEnabled = true;
        }
        StateChanged?.Invoke();
    }

    // Öğretici (1. bölüm üstünde): kaybetmek yok. Bütün misketler çıkınca
    // 1. bölüm normal şekilde kaydedilir, oyuncu 2. bölüme geçer.
    // Öğreticinin güç denemeleri sırasında misketler biterse saha yeniden
    // dizilir; bölüm sadece son aşamada (TutorialFinalPhase) biter.
    public int EndlessScore { get; private set; }
    public int EndlessWave { get; private set; }
    // SONSUZ ÇEMBER turu: saha boşalınca bir sonraki dizilim kurulur, puan devam eder.
    private void EvaluateEndlessTurn()
    {
        int gained = Mathf.Max(0, Score - scoreAtShot);
        EndlessScore += gained;
        MahalleProfile.RecordShot(gained);
        shotsUsed = Mathf.Max(0, shotsUsed - gained);   // her çıkan misket bir atış kazandırır

        if (arena != null && arena.RemainingMarbles == 0)
        {
            EndlessWave++;
            level = EndlessLevel.Build(EndlessWave);
            arena.Configure(level.shape, level.arenaSize, level.rings, level.triangleRows, level.marbles);
            arena.Rebuild();
            MahalleWorld.Apply(this);
            scoredTimes.Clear();
        }
        scoreAtShot = 0;

        if (ShotsLeft <= 0)
        {
            State = LevelState.Lost;
            LastEndlessRecord = MahalleProfile.FinishEndless(EndlessScore);
            if (SfxPlayer.Instance != null) SfxPlayer.Instance.PlayLose();
            if (shooter != null) shooter.ShootingEnabled = false;
            StateChanged?.Invoke();
            return;
        }
        if (shooter != null) { SettleShooter(); shooter.ShootingEnabled = true; }
        StateChanged?.Invoke();
    }
    public bool LastEndlessRecord { get; private set; }
    public bool TutorialFinalPhase { get; private set; }
    public int TutorialKnocked { get; private set; }
    public void StartTutorialFinal()
    {
        TutorialFinalPhase = true;
        ResetTutorialBoard();
    }
    private void ResetTutorialBoard()
    {
        if (arena != null) arena.Rebuild();
        shotsUsed = 0;
        scoredTimes.Clear();
        if (shooter != null) { shooter.ResetTo(level.shooterStartPosition); shooter.ShootingEnabled = true; }
        StateChanged?.Invoke();
    }
    private void EvaluateTutorialTurn()
    {
        MahalleProfile.RecordShot(Score - scoreAtShot);
        TutorialKnocked += Mathf.Max(0, Score - scoreAtShot);
        if (arena != null && arena.RemainingMarbles == 0 && !TutorialFinalPhase)
        {
            ResetTutorialBoard();
            return;
        }
        if (arena != null && arena.RemainingMarbles == 0)
        {
            State = LevelState.Won;
            LastReward = MahalleProfile.Finish(LevelIndex, Stars, Score);
            if (SfxPlayer.Instance != null) SfxPlayer.Instance.PlayWin();
            if (shooter != null) shooter.ShootingEnabled = false;
            StateChanged?.Invoke();
            return;
        }
        if (ShotsLeft <= 0) shotsUsed = 0;
        if (shooter != null)
        {
            SettleShooter();
            shooter.ShootingEnabled = true;
        }
        StateChanged?.Invoke();
    }

    // "Yerinde Kal" sadece misket işe yarar bir yerde durduysa geçerlidir.
    // Çizgiden daha kötü bir noktada kaldıysa hak boşa gitmez, iade edilir.
    private void SettleShooter()
    {
        if (!shooter.KeepsPosition) { shooter.ResetTo(level.shooterStartPosition); return; }

        Vector3 centre = arena != null ? arena.transform.position : Vector3.zero;
        float fromLine = Vector3.Distance(centre, level.shooterStartPosition);
        float fromRest = Vector3.Distance(centre, shooter.transform.position);
        bool worthwhile = fromRest < fromLine - .15f;

        if (worthwhile) { shooter.HoldPosition(); return; }

        MahalleProfile.Refund(MarblePower.Anchor, shooter.AnchorUsedStock);
        AnchorRefunded?.Invoke();
        shooter.ResetTo(level.shooterStartPosition);
    }

    public event Action AnchorRefunded;
}
