using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    // Dizme asamasi: sirayla her oyuncu kendi misketlerini cembere koyar.
    private readonly List<Vector2>[] spots = { new List<Vector2>(), new List<Vector2>() };
    public bool Placing { get; private set; }
    public int PlacingPlayer { get; private set; }
    public bool HandOver { get; private set; }   // telefonu diger oyuncuya verme ekrani
    public int PlacedCount => spots[Mathf.Clamp(PlacingPlayer, 0, 1)].Count;
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

    // ---------------- Dizme ----------------

    public void BeginPlacement()
    {
        level = FindFirstObjectByType<LevelController>();
        if (level == null) { Debug.LogError("DUELLO: LevelController bulunamadi."); return; }
        arena = level.Arena; shooter = level.Shooter;

        spots[0].Clear(); spots[1].Clear();
        PlacingPlayer = DuelSession.FirstPlacer;
        Placing = true; HandOver = false;
        Match = null;

        BuildLevelData();
        level.ConfigureForDuel(data);
        if (shooter != null) shooter.ShootingEnabled = false;   // dizerken atis yok
        RefreshPreview();
        Changed?.Invoke();
    }

    // Parmagin dokundugu noktayi zemine dusurur.
    private bool PointerToGround(out Vector3 world)
    {
        world = default;
        var cam = Camera.main; var pointer = Pointer.current;
        if (cam == null || pointer == null) return false;
        var plane = new Plane(Vector3.up, new Vector3(0f, .25f, 0f));
        var ray = cam.ScreenPointToRay(pointer.position.ReadValue());
        if (!plane.Raycast(ray, out float d)) return false;
        world = ray.GetPoint(d);
        return true;
    }

    public bool AddSpot(Vector2 p)
    {
        int me = Mathf.Clamp(PlacingPlayer, 0, 1);
        if (!Placing || spots[me].Count >= DuelMatch.MarblesPerPlayer) return false;
        p = DuelPlacement.Clamp(p.x, p.y, DuelSession.ArenaSize);

        // Var olan misketlerin uzerine konamaz; oyuncu bos yer secmeli.
        foreach (var q in AllSpots())
            if (Vector2.Distance(q, p) < DuelPlacement.MinGap) return false;
        // Ortadaki buyuk misketin yeri ayrilmistir.
        if (Vector2.Distance(DuelSession.BigMarbleSpot, p) < DuelPlacement.MinGap * DuelSession.BigScale) return false;

        spots[me].Add(p);
        RefreshPreview();
        Changed?.Invoke();
        return true;
    }

    public void UndoSpot()
    {
        int me = Mathf.Clamp(PlacingPlayer, 0, 1);
        if (!Placing || spots[me].Count == 0) return;
        spots[me].RemoveAt(spots[me].Count - 1);
        RefreshPreview();
        Changed?.Invoke();
    }

    public void FillRemaining()
    {
        int me = Mathf.Clamp(PlacingPlayer, 0, 1);
        var hazir = DuelPlacement.DefaultLayout(me, DuelMatch.MarblesPerPlayer, DuelSession.ArenaSize);
        foreach (var q in hazir)
        {
            if (spots[me].Count >= DuelMatch.MarblesPerPlayer) break;
            bool cakisma = false;
            foreach (var r in AllSpots()) if (Vector2.Distance(r, q) < DuelPlacement.MinGap) { cakisma = true; break; }
            if (!cakisma) spots[me].Add(q);
        }
        RefreshPreview();
        Changed?.Invoke();
    }

    // "Hazirim". Ilk oyuncudan sonra telefon devri ekrani gelir.
    public void ConfirmPlacement()
    {
        int me = Mathf.Clamp(PlacingPlayer, 0, 1);
        if (!Placing || spots[me].Count < DuelMatch.MarblesPerPlayer) return;

        int other = 1 - me;
        if (spots[other].Count < DuelMatch.MarblesPerPlayer) { HandOver = true; Changed?.Invoke(); return; }

        Placing = false; HandOver = false;
        StartMatch(spots[0], spots[1]);
    }

    // Telefon devredildi: sira diger oyuncunun dizmesinde.
    public void HandOverDone()
    {
        HandOver = false;
        PlacingPlayer = 1 - PlacingPlayer;
        RefreshPreview();
        Changed?.Invoke();
    }

    private List<Vector2> AllSpots()
    {
        var all = new List<Vector2>(spots[0]); all.AddRange(spots[1]);
        return all;
    }

    // Dizilen misketleri cemberde gosterir. Rakibin misketleri GIZLI:
    // sadece siradaki oyuncununkiler cizilir.
    private void RefreshPreview()
    {
        int me = Mathf.Clamp(PlacingPlayer, 0, 1);
        var show = spots[me];
        // Buyuk misket en sona: oyuncu odulun nerede durdugunu dizerken gorsun.
        var list = new MarbleSpot[show.Count + 1];
        for (int i = 0; i < show.Count; i++) list[i] = new MarbleSpot(show[i].x, show[i].y);
        list[show.Count] = new MarbleSpot(DuelSession.BigMarbleSpot.x, DuelSession.BigMarbleSpot.y);
        data.marbles = list;
        arena.Configure(data.shape, data.arenaSize, data.rings, data.triangleRows, data.marbles);
        arena.Rebuild();
        var spawned = arena.SpawnedMarbles;
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] == null) continue;
            bool big = i == show.Count;
            Tint(spawned[i], big ? BigColor : DuelSession.PlayerColor(me));
            if (big) MakeBig(spawned[i]);
        }
    }

    private void Update()
    {
        if (Placing) { UpdatePlacing(); return; }
        UpdateShooting();
    }

    private void UpdatePlacing()
    {
        if (HandOver) return;
        var pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame) return;
        // Ekranin alt seridi arayuz butonlarina ait; oraya dokunmak misket koymasin.
        if (pointer.position.ReadValue().y < Screen.height * .22f) return;
        if (!PointerToGround(out Vector3 w)) return;
        if (!DuelPlacement.Inside(w.x, w.z, DuelSession.ArenaSize)) return;
        AddSpot(new Vector2(w.x, w.z));
    }

    // ---------------- Kurulum ----------------

    public void StartMatch(IList<Vector2> playerOne, IList<Vector2> playerTwo)
    {
        level = FindFirstObjectByType<LevelController>();
        if (level == null) { Debug.LogError("DUELLO: LevelController bulunamadi."); return; }
        arena = level.Arena;
        shooter = level.Shooter;

        Match = new DuelMatch(DuelSession.FirstPlacer);
        Match.Place(0, ToTuples(playerOne));
        Match.Place(1, ToTuples(playerTwo));
        Match.PlaceBigMarble(DuelSession.BigMarbleSpot.x, DuelSession.BigMarbleSpot.y);

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

        if (Match != null)
        {
            var list = new MarbleSpot[Match.Marbles.Count];
            for (int i = 0; i < list.Length; i++)
                list[i] = new MarbleSpot(Match.Marbles[i].x, Match.Marbles[i].z);
            data.marbles = list;
        }
        else data.marbles = new MarbleSpot[0];
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
            if (Match.Marbles[i].big) { Tint(m, BigColor); MakeBig(m); }
            else Tint(m, DuelSession.PlayerColor(m.Owner));
        }
    }

    // Buyuk misket ne senin ne rakibin: ayri bir renk.
    private static readonly Color BigColor = new Color(.94f, .90f, .78f);

    // Buyuk ve agir. Olcum agirlastirmanin misketi neredeyse sabitledigini
    // gosterdi; odul olmasinin sebebi tam olarak bu -- kolay cikmamali.
    private static void MakeBig(TargetMarble marble)
    {
        marble.transform.localScale *= DuelSession.BigScale;
        var body = marble.GetComponent<Rigidbody>();
        if (body != null) body.mass *= DuelSession.BigMass;
        var p = marble.transform.position;
        p.y = marble.transform.localScale.y * .5f;
        marble.transform.position = p;
        if (body != null) body.position = p;
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

    private void UpdateShooting()
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
        // AMA cemberin disina savrulduysa pozisyon hakkini kaybeder ve cizgiye
        // doner. Gercek misket oyununda da cemberi terk eden atici avantajini
        // yitirir; ayrica bu olmadan atici ekran disinda kalip oyunu kilitliyordu.
        if (sameTurn && ShooterInsideRing()) shooter.HoldPosition();
        else PlaceShooterOnLine();

        Changed?.Invoke();
    }

    private bool ShooterInsideRing()
    {
        if (shooter == null || arena == null) return false;
        return !arena.IsOutside(shooter.transform.position);
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

    public void Rematch()
    {
        DuelSession.FirstPlacer = 1 - DuelSession.FirstPlacer;
        DuelSession.MatchNumber++;
        BeginPlacement();
    }
}
