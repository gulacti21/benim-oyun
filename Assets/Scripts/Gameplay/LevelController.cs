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
    // BEKLEMEYİ KISALTMA. Kural: sonucu değiştirebilecek hiçbir misketi durdurma.
    // Sert dondurma denendi (CrawlSpeed .5) ve kaldırıldı: gözle görülür hızda
    // yuvarlanan misketler yolun ortasında çakılıyor, bug gibi duruyordu ve
    // bir başkasına çarpıp çıkarma ihtimalini de yok ederek dengeyi bozuyordu.
    // Artık: önce yumuşak frenleme, sonra sadece gerçekten sürünenler durdurulur.
    // (Otomatik hızlandırma da denendi ve kaldırıldı: kötü görünüyordu.)
    private const float DampAfter = 1.2f;         // bu andan sonra yavaşları nazikçe frenle
    private const float DampSpeed = .6f;          // sadece bu hızın altındakiler frenlenir
    private const float DampFactor = .9f;         // 60 fps referanslı kare başına hız çarpanı
    private const float CrawlStopAfter = 1.8f;    // gerçekten sürünenleri durdurma anı
    private const float CrawlSpeed = .25f;        // bu hızın altı "sürünüyor" sayılır
    private const float RestThreshold = .22f;     // arenanın "durdu" eşiği (varsayılan .15)
    private const float QuickSettleDelay = .25f;  // atış sonrası bekleme payı (varsayılan .4)
    // SON TUR: bu atis bolumu bitiriyorsa acele etme. Cemberin kenarinda hala
    // yuvarlanan bir misket sayilmadan oyun bitmesin.
    private const float FinalSettleDelay = 1f;
    private float shooterOutsideTime;
    private readonly System.Collections.Generic.Dictionary<TargetMarble, float> scoredTimes =
        new System.Collections.Generic.Dictionary<TargetMarble, float>();
    private float shotElapsed;
    public RoundReward LastReward { get; private set; }
    public ShotController Shooter => shooter;
    public bool WaitingForSettle => waitingForSettle;
    private void Awake() { database = Campaign.Database; }
    // Oynanan bölümün haritası (LevelIndex global).
    public int Map => Maps.MapOf(LevelIndex);

    public event Action StateChanged;

    public LevelState State { get; private set; } = LevelState.Playing;
    public bool IsPaused { get; private set; }
    public int LevelIndex { get; private set; }
    // Sonraki bölüm aynı haritada olmalı: haritanın son bölümünden sonrakine otomatik geçilmez.
    public bool HasNextLevel => Maps.Valid(LevelIndex + 1) && Maps.MapOf(LevelIndex + 1) == Map;
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
        if (GameSession.EndlessMode) UpdateEndless();
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
        LevelIndex = Mathf.Clamp(GameSession.SelectedLevelIndex, 0, Maps.TotalLevels - 1);
        if (!MahalleProfile.Unlocked(LevelIndex)) LevelIndex = MahalleProfile.NextLevelIn(Maps.MapOf(LevelIndex));
        if (!MahalleProfile.Unlocked(LevelIndex)) LevelIndex = MahalleProfile.NextLevel;

        {
            LevelData fromDatabase = Maps.Get(LevelIndex);

            if (fromDatabase != null)
            {
                level = fromDatabase;
            }
        }

        if (GameSession.EndlessMode)
        {
            EndlessStage = 1; EndlessScore = 0; EndlessElapsed = 0f; endlessSeen = 0;
            EndlessTimeLeft = EndlessLevel.StartTime;
            level = EndlessLevel.Build(EndlessStage);
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
            arena.RestThreshold = RestThreshold;
            arena.Configure(level.shape, level.arenaSize, level.rings, level.triangleRows, level.marbles);
            arena.Rebuild();
        }

        if (shooter != null)
        {
            shooter.ResetTo(level.shooterStartPosition);
            shooter.ShootingEnabled = true;
        }

        // HARİTA 2: kum, çamur, çukur ve eğim. Kuralı olmayan bölümde hiçbir şey kurmaz.
        GroundZones.Setup(arena, shooter != null ? shooter.Body : null, level);

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
        // Bu atis bolumu bitiriyor mu? Bitiriyorsa esikler gevsetilir.
        bool sonTur = !GameSession.EndlessMode &&
                      (ShotsLeft <= 0 || (arena != null && arena.RemainingMarbles == 0));
        StopMarblesOutside();
        if (shotElapsed > (GameSession.EndlessMode ? EndlessLevel.SettleCutoff : 12f))
        {
            foreach (var b in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None)) { b.linearVelocity = Vector3.zero; b.angularVelocity = Vector3.zero; b.Sleep(); }
        }
        // Bekleme kısaltma: önce yumuşak frenleme, sonra tam durdurma.
        // Hızlı misketlere dokunulmaz, sonuç değişmez.
        if (shotElapsed > DampAfter)
        {
            float k = Mathf.Pow(DampFactor, Time.deltaTime * 60f);
            foreach (var b in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                if (b.isKinematic) continue;
                float hiz = b.linearVelocity.magnitude;
                if (hiz >= DampSpeed) continue;
                // Son turda gec ve daha dusuk esikte durdur: cemberden cikmak
                // uzere olan misketi erken dondurup sayidan dusurmeyelim.
                if (shotElapsed > (sonTur ? CrawlStopAfter + 1f : CrawlStopAfter)
                    && hiz < (sonTur ? CrawlSpeed * .5f : CrawlSpeed))
                {
                    b.linearVelocity = Vector3.zero;
                    b.angularVelocity = Vector3.zero;
                    b.Sleep();
                    continue;
                }
                b.linearVelocity *= k;
                b.angularVelocity *= k;
            }
        }
        bool shooterResting = shooter == null || shooter.AtRest;
        // SONSUZ ÇEMBER: hedeflerin durmasını bekleme. Atıcı durunca sıra biter,
        // diğer misketler yuvarlanmaya devam ederken yeni atış yapılabilir.
        bool marblesResting = GameSession.EndlessMode || arena == null || arena.AllMarblesAtRest();

        if (!shooterResting || !marblesResting)
        {
            settleTimer = 0f;
            return;
        }

        settleTimer += Time.deltaTime;

        float bekleme = GameSession.EndlessMode ? EndlessLevel.SettleDelay
                      : sonTur ? FinalSettleDelay : QuickSettleDelay;
        if (settleTimer < bekleme)
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
    public int EndlessStage { get; private set; } = 1;
    public float EndlessTimeLeft { get; private set; }
    public float EndlessElapsed { get; private set; }
    public float EndlessGainFlash { get; private set; }   // "+2.5 sn" yazısı için

    // Süre akışı ve kademe atlama.
    private void UpdateEndless()
    {
        if (IsPaused || State != LevelState.Playing) return;
        float dt = Time.deltaTime;
        EndlessElapsed += dt;
        EndlessTimeLeft -= dt;

        // Çıkan misketler ANINDA süreye yazılır: sıranın bitmesini beklemez.
        if (arena != null && arena.Score > endlessSeen)
        {
            int gained = arena.Score - endlessSeen;
            endlessSeen = arena.Score;
            EndlessScore = arena.Score;
            MahalleProfile.RecordShot(gained);
            EndlessTimeLeft += gained * EndlessLevel.TimePerMarble;
            EndlessGainFlash = 1.2f;
        }
        // Saha azaldıysa beklemeden dolum yapılır.
        if (arena != null && arena.RemainingMarbles < EndlessLevel.RefillBelow) RefillEndless();
        if (EndlessGainFlash > 0f) EndlessGainFlash -= dt;

        int stage = EndlessLevel.StageAt(EndlessElapsed);
        if (stage != EndlessStage)
        {
            // Kademe atladı: zemin değişir, gerekiyorsa yeni engel gelir.
            // Kademe atladı. Zemin ve mahalle AYNI kalır (renk atlaması kötü duruyordu);
            // sadece engel sayısı değişirse dünya yeniden kurulur.
            int oncekiEngel = level.obstacles == null ? 0 : level.obstacles.Length;
            EndlessStage = stage;
            var next = EndlessLevel.Build(stage);
            level.obstacles = next.obstacles;
            level.obstacleCount = next.obstacleCount;
            if ((level.obstacles == null ? 0 : level.obstacles.Length) != oncekiEngel) MahalleWorld.Apply(this);
        }

        if (EndlessTimeLeft <= 0f)
        {
            EndlessTimeLeft = 0f;
            EndFromTime();
        }
    }

    private void EndFromTime()
    {
        State = LevelState.Lost;
        LastEndlessRecord = MahalleProfile.FinishEndless(EndlessScore);
        if (SfxPlayer.Instance != null) SfxPlayer.Instance.PlayLose();
        if (shooter != null) shooter.ShootingEnabled = false;
        StateChanged?.Invoke();
    }
    // SONSUZ ÇEMBER turu: saha boşalınca bir sonraki dizilim kurulur, puan devam eder.
    private int endlessSeen;
    // Yeni misketler, duran misketlerin ve engellerin üstüne gelmeyecek şekilde dizilir.
    private void RefillEndless()
    {
        var busy = new System.Collections.Generic.List<Vector2>();
        foreach (var m in arena.SpawnedMarbles)
            if (m != null && !m.IsScored) busy.Add(new Vector2(m.transform.position.x, m.transform.position.z));
        if (shooter != null) busy.Add(new Vector2(shooter.transform.position.x, shooter.transform.position.z));
        int need = EndlessLevel.BoardCount - arena.RemainingMarbles;
        if (need <= 0) return;
        arena.AddMarbles(EndlessLevel.Spots(EndlessStage, need, busy,
                          Mathf.RoundToInt(EndlessElapsed * 1000f) + EndlessScore));
    }

    private void EvaluateEndlessTurn()
    {
        shotsUsed = 0;                       // atış sınırsız
        scoreAtShot = Score;
        if (EndlessTimeLeft <= 0f) { EndFromTime(); return; }
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
