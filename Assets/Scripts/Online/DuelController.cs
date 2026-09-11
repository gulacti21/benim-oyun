using System;
using System.Collections.Generic;
using UnityEngine;

// Kural motorunu oyunun atis sistemine baglayan katman.
// Kurallar DuelMatch'te, fizik mevcut MarbleArena/ShotController'da;
// burasi ikisinin arasinda tercumanlik yapar.
public class DuelController : MonoBehaviour
{
    public static DuelController Instance { get; private set; }

    private LevelController level;
    private MarbleArena arena;
    private ShotController shooter;
    private LevelData data;
    private PhysicsMaterial duelMaterial;

    private readonly List<int> knocked = new List<int>();
    private readonly Dictionary<TargetMarble, int> indexOf = new Dictionary<TargetMarble, int>();

    private bool waiting;         // atis yapildi, misketlerin durmasi bekleniyor
    private float settleTimer, elapsed;
    private const float SettleDelay = .4f;

    public DuelMatch Match { get; private set; }
    public event Action Changed;

    private void Awake() { Instance = this; }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Unhook();
        if (duelMaterial != null) { Destroy(duelMaterial); duelMaterial = null; }
    }

    // ---------------- Kurulum ----------------

    public void StartMatch(IList<Vector2> playerOne, IList<Vector2> playerTwo)
    {
        level = FindFirstObjectByType<LevelController>();
        if (level == null) { Debug.LogError("DUELLO: LevelController bulunamadi."); return; }
        arena = level.Arena;
        shooter = level.Shooter;

        Match = new DuelMatch(DuelSession.StartingPlayer);
        Match.Place(0, ToTuples(playerOne));
        Match.Place(1, ToTuples(playerTwo));

        BuildLevelData();
        level.ConfigureForDuel(data);
        AssignOwners();
        ApplyDuelPhysics();
        Hook();

        waiting = false; settleTimer = 0f; elapsed = 0f;
        PlaceShooterOnLine();
        Changed?.Invoke();
    }

    private static List<(float x, float z)> ToTuples(IList<Vector2> spots)
    {
        var list = new List<(float, float)>();
        foreach (var s in spots) list.Add((s.x, s.y));
        return list;
    }

    // Duello bolumu: cember, engelsiz, elle dizilmis 14 misket.
    // Atis hakki kural motorunda tutuluyor, burada yuksek verilir ki
    // LevelController "atis bitti" diye maci kesmesin.
    private void BuildLevelData()
    {
        if (data == null) data = ScriptableObject.CreateInstance<LevelData>();
        data.levelName = "DÜELLO";
        data.shape = ArenaShape.Circle;
        data.arenaSize = DuelSession.ArenaSize;
        data.shotCount = 999;
        data.oneStarTarget = data.twoStarTarget = data.threeStarTarget = 99;
        data.starsToPass = 1;
        data.obstacles = null;
        data.obstacleCount = 0;
        data.shooterHalfWidth = 0f;
        data.shooterOffsetX = 0f;
        data.shooterStartPosition = new Vector3(0f, .25f, DuelSession.ShooterZ);

        var spots = new MarbleSpot[Match.Marbles.Count];
        for (int i = 0; i < spots.Length; i++)
            spots[i] = new MarbleSpot(Match.Marbles[i].x, Match.Marbles[i].z);
        data.marbles = spots;
    }

    // Arena misketleri dizilis sirasiyla uretiyor; sahiplik o siraya gore atanir.
    // Kampanyanin MarbleMaterialPhysics.asset dosyasina DOKUNULMAZ: ona
    // yazmak 60 bolumun dengesini bozar. Duello icin calisma aninda ayri
    // bir materyal uretilir ve sadece duello misketlerine takilir.
    private void ApplyDuelPhysics()
    {
        var source = arena != null && arena.SpawnedMarbles.Count > 0
            ? arena.SpawnedMarbles[0].GetComponent<Collider>()?.sharedMaterial : null;
        if (source == null) return;

        if (duelMaterial == null) duelMaterial = new PhysicsMaterial("Duello misketi");
        duelMaterial.dynamicFriction = source.dynamicFriction * DuelSession.FrictionMul;
        duelMaterial.staticFriction = source.staticFriction * DuelSession.FrictionMul;
        duelMaterial.bounciness = DuelSession.Bounciness;
        duelMaterial.frictionCombine = source.frictionCombine;
        duelMaterial.bounceCombine = source.bounceCombine;

        foreach (var m in arena.SpawnedMarbles)
        {
            if (m == null) continue;
            var c = m.GetComponent<Collider>();
            if (c != null) c.sharedMaterial = duelMaterial;
        }
        var shooterCollider = shooter != null ? shooter.GetComponent<Collider>() : null;
        if (shooterCollider != null) shooterCollider.sharedMaterial = duelMaterial;
        if (shooter != null) shooter.DuelImpulseScale = DuelSession.Impulse / .65f;
    }

    private void AssignOwners()
    {
        indexOf.Clear();
        var spawned = arena.SpawnedMarbles;
        for (int i = 0; i < spawned.Count && i < Match.Marbles.Count; i++)
        {
            var m = spawned[i];
            if (m == null) continue;
            m.Owner = Match.Marbles[i].owner;
            indexOf[m] = i;
            Tint(m, DuelSession.PlayerColor(m.Owner));
        }
    }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorId = Shader.PropertyToID("_Color");

    // Iki oyuncunun misketi renkle ayrilir. Bu SADECE gorsel: fizik ikisinde de ayni.
    private static void Tint(TargetMarble marble, Color color)
    {
        var renderer = marble.GetComponent<MeshRenderer>();
        if (renderer == null) return;
        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        block.SetColor(LegacyColorId, color);
        renderer.SetPropertyBlock(block);
    }

    private void Hook()
    {
        Unhook();
        if (arena != null) arena.MarbleLeft += OnMarbleLeft;
        if (shooter != null) shooter.ShotFired += OnShotFired;
    }

    private void Unhook()
    {
        if (arena != null) arena.MarbleLeft -= OnMarbleLeft;
        if (shooter != null) shooter.ShotFired -= OnShotFired;
    }

    // ---------------- Atis dongusu ----------------

    private void OnShotFired()
    {
        if (Match == null || Match.State != DuelMatch.Phase.Shooting) return;
        knocked.Clear();
        waiting = true; settleTimer = 0f; elapsed = 0f;
        Changed?.Invoke();
    }

    private void OnMarbleLeft(TargetMarble marble)
    {
        if (marble == null) return;
        if (indexOf.TryGetValue(marble, out int i) && !knocked.Contains(i)) knocked.Add(i);
    }

    private void Update()
    {
        if (!waiting || Match == null) return;

        elapsed += Time.deltaTime;
        // Cok uzun suren atislar oyunu kilitlemesin: her seyi durdurup cozeriz.
        if (elapsed > 12f)
        {
            foreach (var b in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            { b.linearVelocity = Vector3.zero; b.angularVelocity = Vector3.zero; }
        }

        bool resting = (shooter == null || shooter.AtRest) && (arena == null || arena.AllMarblesAtRest());
        if (!resting) { settleTimer = 0f; return; }

        settleTimer += Time.deltaTime;
        if (settleTimer < SettleDelay) return;

        waiting = false;
        Resolve();
    }

    private void Resolve()
    {
        bool sameTurn = Match.ResolveShot(knocked);
        knocked.Clear();

        if (Match.State == DuelMatch.Phase.Finished)
        {
            if (shooter != null) shooter.ShootingEnabled = false;
            Changed?.Invoke();
            return;
        }

        // Zincir: cikardiysan atici kaldigi yerde kalir ve oradan atarsin.
        // Sira gectiyse atici cizgiye doner.
        if (sameTurn) shooter.HoldPosition();
        else PlaceShooterOnLine();

        Changed?.Invoke();
    }

    private void PlaceShooterOnLine()
    {
        if (shooter == null) return;
        shooter.ResetTo(new Vector3(0f, .25f, DuelSession.ShooterZ));
        shooter.ShootingEnabled = true;
        var visual = shooter.GetComponent<MarbleVisual>();
        if (visual != null) visual.SetSkin(DuelSession.Skin[Mathf.Clamp(Match.Turn, 0, 1)]);
    }

    // ---------------- Rovans ----------------

    public void Rematch(IList<Vector2> playerOne, IList<Vector2> playerTwo)
    {
        DuelSession.StartingPlayer = 1 - DuelSession.StartingPlayer;
        DuelSession.MatchNumber++;
        StartMatch(playerOne, playerTwo);
    }
}
