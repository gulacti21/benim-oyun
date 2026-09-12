using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Kural motorunu oyunun atis sistemine baglayan katman.
// Kurallar DuelMatch'te, fizik mevcut MarbleArena/ShotController'da;
// burasi ikisinin arasinda tercumanlik yapar.
//
// Bir elin akisi:
//   [hak teklifi] -> dizme (elle ya da otomatik) -> telefon devri -> atis
//   -> el sonu -> sonraki el
//
// Macin en basinda bir de SIRA BELIRLEME ATISI var (DuelToss): iki oyuncu
// bos sahaya birer atis yapar, uzak kenara en yakin duran once atar.
public class DuelController : MonoBehaviour
{
    public static DuelController Instance { get; private set; }

    public enum Step { Toss, Offer, Place, HandOver, Shoot, RoundOver, Done }

    private LevelController level;
    private MarbleArena arena;
    private ShotController shooter;
    private LevelData data;
    private PhysicsMaterial duelMaterial;

    private readonly List<int> knocked = new List<int>();
    private readonly Dictionary<TargetMarble, int> indexOf = new Dictionary<TargetMarble, int>();
    private readonly List<Vector2>[] spots = { new List<Vector2>(), new List<Vector2>() };
    // Elin dizme sirasi: once ilk dizen, sonra digeri.
    private readonly int[] order = new int[2];
    private int orderIndex;

    private bool waiting;
    private bool tossShotTaken;
    private float settleTimer, elapsed;
    private const float SettleDelay = .4f;

    public DuelMatch Match { get; private set; }
    public Step CurrentStep { get; private set; } = Step.Done;
    public int ActivePlayer => order[Mathf.Clamp(orderIndex, 0, 1)];
    public int PlacedCount => spots[Mathf.Clamp(ActivePlayer, 0, 1)].Count;
    public int NeedCount => Match != null ? Match.AnteFor(ActivePlayer) : 0;
    // Dizme ekraninda oyuncuya gosterilen kisa uyari. Teknik ayiklama
    // metni DEGIL: sadece oyuncunun duzeltebilecegi durumlari yazar.
    public string Hint = "";
    public event Action Changed;

    private void Awake() { Instance = this; }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Unhook();
        if (duelMaterial != null) { Destroy(duelMaterial); duelMaterial = null; }
    }

    // ---------------- Mac ----------------

    public void BeginMatch()
    {
        level = FindFirstObjectByType<LevelController>();
        if (level == null) { Debug.LogError("DUELLO: LevelController bulunamadi."); return; }
        arena = level.Arena; shooter = level.Shooter;

        if (data == null) data = ScriptableObject.CreateInstance<LevelData>();

        // Rovansta sira atisi tekrar yapilmaz: kural "ilk baslayan degisir".
        if (DuelSession.MatchNumber > 0) { StartMatchProper(); return; }
        BeginToss();
    }

    // ---------------- Sira belirleme atisi ----------------

    private void BeginToss()
    {
        CurrentStep = Step.Toss;
        DuelToss.Reset(0);
        tossShotTaken = false;
        BuildTossData();
        level.ConfigureForDuel(data);
        ApplyDuelPhysics();
        Hook();
        waiting = false; settleTimer = 0f; elapsed = 0f;
        PlaceTossShooter();
        Refresh();
    }

    private void BuildTossData()
    {
        data.levelName = "SIRA ATIŞI";
        data.shape = DuelSession.Triangle ? ArenaShape.Triangle : ArenaShape.Circle;
        data.arenaSize = DuelSession.ArenaSize;
        data.shotCount = 999;
        data.oneStarTarget = data.twoStarTarget = data.threeStarTarget = 99;
        data.starsToPass = 1;
        data.obstacles = null; data.obstacleCount = 0;
        data.shooterHalfWidth = 0f; data.shooterOffsetX = 0f;
        data.shooterStartPosition = new Vector3(0f, .25f, DuelSession.ShooterZ);
        data.marbles = new MarbleSpot[0];      // saha bos: sadece cizgiye atilir
    }

    private void PlaceTossShooter()
    {
        if (shooter == null) return;
        shooter.ResetTo(new Vector3(0f, .25f, DuelSession.ShooterZ));
        shooter.ShootingEnabled = true;
        // Sira atisi mac atisindan DAHA HAFIF: bkz. DuelSession.TossImpulseFactor.
        shooter.DuelImpulseScale = DuelSession.TossImpulse / .65f;
        var visual = shooter.GetComponent<MarbleVisual>();
        if (visual != null) visual.SetSkin(DuelSession.Skin[Mathf.Clamp(DuelToss.Shooter, 0, 1)]);
    }

    private void UpdateToss()
    {
        if (!tossShotTaken) return;

        elapsed += Time.deltaTime;
        bool zorla = elapsed > 8f;
        if (!zorla)
        {
            if (shooter != null)
            {
                var p = shooter.transform.position;
                // Dunyadan dusen atici asla durmaz; dustugu an olcup bitir.
                if (p.y < -1f) zorla = true;
            }
            if (!zorla)
            {
                if (shooter != null && !shooter.AtRest) { settleTimer = 0f; return; }
                settleTimer += Time.deltaTime;
                if (settleTimer < SettleDelay) return;
            }
        }

        tossShotTaken = false;
        Vector3 sp = shooter != null ? shooter.transform.position : Vector3.zero;
        // Cizgiyi gecmek yaniktir; kisa kalmak degil. Karar DuelToss'ta.
        bool dustu = shooter == null || sp.y < -1f;
        int atan = DuelToss.Shooter;
        DuelToss.Record(atan, DuelToss.Measure(sp.x, sp.z, dustu));

        if (DuelToss.Done)
        {
            if (shooter != null) shooter.ShootingEnabled = false;
            Refresh();          // UI sonucu gosterir, oyuncu MACA BASLA'ya basar
            return;
        }
        PlaceTossShooter();
        Refresh();
    }

    // Sira atisi bitti; oyuncu onayladi.
    public void TossDone()
    {
        if (CurrentStep != Step.Toss || !DuelToss.Done) return;
        DuelSession.FirstPlacer = DuelToss.FirstPlacer;
        StartMatchProper();
    }

    private void StartMatchProper()
    {
        Unhook();
        Match = new DuelMatch(DuelSession.FirstPlacer);
        BeginRound();
    }

    private void BeginRound()
    {
        spots[0].Clear(); spots[1].Clear();
        order[0] = Match.FirstPlacer; order[1] = 1 - Match.FirstPlacer;
        orderIndex = 0;
        if (shooter != null) shooter.ShootingEnabled = false;
        Refresh();
        NextPlacer();
    }

    // Siradaki oyuncuya gecer: hakki varsa teklif eder, yoksa dizdirir.
    private void NextPlacer()
    {
        while (orderIndex < 2)
        {
            int p = order[orderIndex];
            if (Match.HasPlaced(p)) { orderIndex++; continue; }

            // Ilk el herkes elle dizer, teklif yok.
            if (Match.Round == 1) { CurrentStep = Step.Place; Refresh(); return; }

            // Sonraki ellerde hakki varsa sorulur, yoksa otomatik dizilir.
            if (Match.PlacementRights(p) > 0) { CurrentStep = Step.Offer; Refresh(); return; }
            AutoPlace(p);
            orderIndex++;
        }
        StartShooting();
    }

    public void ChooseManual()
    {
        if (CurrentStep != Step.Offer) return;
        if (!Match.UsePlacementRight(ActivePlayer)) { ChooseAuto(); return; }
        CurrentStep = Step.Place; Refresh();
    }

    public void ChooseAuto()
    {
        if (CurrentStep != Step.Offer) return;
        AutoPlace(ActivePlayer);
        orderIndex++;
        NextPlacer();
    }

    private void AutoPlace(int player)
    {
        var hazir = DuelPlacement.DefaultLayout(player, Match.AnteFor(player), DuelSession.ArenaSize);
        var son = Avoid(hazir, player);
        Match.Place(player, ToTuples(son));
    }

    // Otomatik dizilis, ortada duran misketlerin ustune gelmesin.
    private List<Vector2> Avoid(List<Vector2> istenen, int player)
    {
        var dolu = new List<Vector2>();
        foreach (var m in Match.Marbles) if (!m.out_) dolu.Add(new Vector2(m.x, m.z));
        foreach (var q in spots[0]) dolu.Add(q);
        foreach (var q in spots[1]) dolu.Add(q);

        var sonuc = new List<Vector2>();
        foreach (var q in istenen)
        {
            Vector2 p = q;
            // Dolu noktadan kacana kadar kucuk adimlarla kaydir.
            for (int deneme = 0; deneme < 40; deneme++)
            {
                bool cakisma = false;
                foreach (var d in dolu) if (Vector2.Distance(d, p) < DuelPlacement.MinGap) { cakisma = true; break; }
                foreach (var d in sonuc) if (Vector2.Distance(d, p) < DuelPlacement.MinGap) { cakisma = true; break; }
                if (!cakisma) break;
                float a = deneme * 2.4f;
                p = DuelPlacement.Clamp(q.x + Mathf.Cos(a) * (.2f + deneme * .05f),
                                        q.y + Mathf.Sin(a) * (.2f + deneme * .05f), DuelSession.ArenaSize);
            }
            sonuc.Add(p);
        }
        return sonuc;
    }

    // ---------------- Elle dizme ----------------

    public bool AddSpot(Vector2 p)
    {
        if (CurrentStep != Step.Place || PlacedCount >= NeedCount) return false;
        p = DuelPlacement.Clamp(p.x, p.y, DuelSession.ArenaSize);
        if (Occupied(p)) return false;
        spots[ActivePlayer].Add(p);
        RefreshPreview();
        Refresh();
        return true;
    }

    private bool Occupied(Vector2 p)
    {
        foreach (var m in Match.Marbles) if (!m.out_ && Vector2.Distance(new Vector2(m.x, m.z), p) < DuelPlacement.MinGap) return true;
        foreach (var q in spots[0]) if (Vector2.Distance(q, p) < DuelPlacement.MinGap) return true;
        foreach (var q in spots[1]) if (Vector2.Distance(q, p) < DuelPlacement.MinGap) return true;
        return false;
    }

    public void UndoSpot()
    {
        if (CurrentStep != Step.Place || PlacedCount == 0) return;
        spots[ActivePlayer].RemoveAt(spots[ActivePlayer].Count - 1);
        RefreshPreview(); Refresh();
    }

    public void FillRemaining()
    {
        if (CurrentStep != Step.Place) return;
        int me = ActivePlayer;
        foreach (var q in Avoid(DuelPlacement.DefaultLayout(me, NeedCount, DuelSession.ArenaSize), me))
        {
            if (spots[me].Count >= NeedCount) break;
            if (!Occupied(q)) spots[me].Add(q);
        }
        RefreshPreview(); Refresh();
    }

    public void ConfirmPlacement()
    {
        if (CurrentStep != Step.Place || PlacedCount < NeedCount) return;
        int me = ActivePlayer;
        Match.Place(me, ToTuples(spots[me]));
        orderIndex++;

        // Sonraki oyuncu da elle dizecekse telefon devri ekrani gelir.
        if (orderIndex < 2 && !Match.HasPlaced(order[1]))
        {
            int sonraki = order[1];
            bool elle = Match.Round == 1 || Match.PlacementRights(sonraki) > 0;
            if (elle) { CurrentStep = Step.HandOver; Refresh(); return; }
        }
        NextPlacer();
    }

    public void HandOverDone()
    {
        if (CurrentStep != Step.HandOver) return;
        RefreshPreview();
        NextPlacer();
    }

    // ---------------- Atis ----------------

    private void StartShooting()
    {
        CurrentStep = Step.Shoot;
        BuildLevelData();
        level.ConfigureForDuel(data);
        AssignOwners();
        ApplyDuelPhysics();
        Hook();
        waiting = false; settleTimer = 0f; elapsed = 0f;
        PlaceShooterOnLine();
        Refresh();
    }

    private void BuildLevelData()
    {
        data.levelName = "DÜELLO";
        data.shape = DuelSession.Triangle ? ArenaShape.Triangle : ArenaShape.Circle;
        data.arenaSize = DuelSession.ArenaSize;
        data.shotCount = 999;
        data.oneStarTarget = data.twoStarTarget = data.threeStarTarget = 99;
        data.starsToPass = 1;
        data.obstacles = null; data.obstacleCount = 0;
        data.shooterHalfWidth = 0f; data.shooterOffsetX = 0f;
        data.shooterStartPosition = new Vector3(0f, .25f, DuelSession.ShooterZ);

        var list = new List<MarbleSpot>();
        foreach (var m in Match.Marbles) if (!m.out_) list.Add(new MarbleSpot(m.x, m.z));
        data.marbles = list.ToArray();
    }

    // Arena misketleri dizilis sirasiyla uretir; indeksler o siraya gore eslenir.
    private void AssignOwners()
    {
        indexOf.Clear();
        var spawned = arena.SpawnedMarbles;
        int k = 0;
        for (int i = 0; i < Match.Marbles.Count && k < spawned.Count; i++)
        {
            if (Match.Marbles[i].out_) continue;
            var m = spawned[k++];
            if (m == null) continue;
            indexOf[m] = i;
            Tint(m, Renk(Match.Marbles[i]));
        }
    }

    private static Color Renk(DuelMatch.Marble m)
    {
        // Cemberde kalmis atici ayri renkte: rakip icin acik hedef.
        if (m.stranded) return new Color(.94f, .90f, .78f);
        return DuelSession.PlayerColor(m.placedBy);
    }

    private void OnShotFired()
    {
        if (CurrentStep == Step.Toss)
        {
            tossShotTaken = true; settleTimer = 0f; elapsed = 0f;
            Refresh(); return;
        }
        if (Match == null || Match.State != DuelMatch.Phase.Shooting) return;
        knocked.Clear();
        waiting = true; settleTimer = 0f; elapsed = 0f;
        Refresh();
    }

    private void OnMarbleLeft(TargetMarble marble)
    {
        if (marble == null) return;
        if (indexOf.TryGetValue(marble, out int i) && !knocked.Contains(i)) knocked.Add(i);
        // Cemberi terk eden misket artik oyunda degil: dondurulur. Dondurulmazsa
        // zeminin kenarindan ucup bosluga dusuyor ve DUSEN CISIM ASLA DURMUYOR --
        // oyun "hepsi durdu mu" beklemesinde sonsuza kadar takiliyordu.
        Freeze(marble);
    }

    private static void Freeze(TargetMarble marble)
    {
        var body = marble.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }
        var col = marble.GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    private void Update()
    {
        if (CurrentStep == Step.Toss) { UpdateToss(); return; }
        if (CurrentStep == Step.Place) { UpdatePlacing(); return; }
        if (CurrentStep == Step.Shoot) UpdateShooting();
    }

    private void UpdateShooting()
    {
        if (!waiting || Match == null) return;

        elapsed += Time.deltaTime;
        if (elapsed > 8f)
        {
            // Emniyet: sekiz saniyede cozumle. Sadece hizlari sifirlamak
            // yetmiyordu, cunku dusen cismi yercekimi tekrar hizlandiriyor.
            foreach (var m in arena.SpawnedMarbles)
                if (m != null)
                {
                    var b = m.GetComponent<Rigidbody>();
                    if (b != null && !b.isKinematic) { b.linearVelocity = Vector3.zero; b.angularVelocity = Vector3.zero; }
                }
            waiting = false;
            Resolve(); return;
        }

        // Atici dunyadan dustuyse kurtar.
        if (shooter != null)
        {
            var sp = shooter.transform.position;
            if (sp.y < -1f || new Vector2(sp.x, sp.z).magnitude > DuelSession.ArenaSize * 3f)
                shooter.ResetTo(new Vector3(0f, .25f, DuelSession.ShooterZ));
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
        // Atici cemberin ORTASINDA durduysa kaybedilir ve orada hedef olur.
        // Kenarda durmak guvenli: olcum, "cemberin herhangi bir yeri" kuralinin
        // atislarin %60'inda tetiklendigini gosterdi, o da secim degil vergi olurdu.
        Vector3 sp = shooter != null ? shooter.transform.position : Vector3.zero;
        float sr = new Vector2(sp.x, sp.z).magnitude;
        // Ucgenin ic yaricapi kenar yaricapinin yarisi kadardir; tehlike
        // bolgesi de ona gore olceklenir ki iki modda ayni oranda tetiklensin.
        float tehlike = DuelSession.ArenaSize * DuelSession.StrandRadiusFactor * (DuelSession.Triangle ? .5f : 1f);
        bool stranded = shooter != null && sr <= tehlike;

        bool sameTurn = Match.ResolveShot(knocked, stranded, sp.x, sp.z);
        knocked.Clear();

        if (Match.State != DuelMatch.Phase.Shooting)
        {
            if (shooter != null) shooter.ShootingEnabled = false;
            CurrentStep = Match.State == DuelMatch.Phase.Finished ? Step.Done : Step.RoundOver;
            Refresh();
            return;
        }

        // Atici cemberde kaldiysa yenisi cizgiden gelir; kalmadiysa ve zincir
        // devam ediyorsa durdugu yerden atar.
        if (stranded) { RebuildRing(); PlaceShooterOnLine(); }
        else if (sameTurn) shooter.HoldPosition();
        else PlaceShooterOnLine();

        Refresh();
    }

    // Cemberde kalan atici yeni bir hedef olarak eklendigi icin arena yeniden kurulur.
    private void RebuildRing()
    {
        BuildLevelData();
        arena.Configure(data.shape, data.arenaSize, data.rings, data.triangleRows, data.marbles);
        arena.Rebuild();
        AssignOwners();
        ApplyDuelPhysics();
    }

    public void NextRound()
    {
        if (CurrentStep != Step.RoundOver) return;
        Match.NextRound();
        if (Match.State == DuelMatch.Phase.Finished) { CurrentStep = Step.Done; Refresh(); return; }
        DuelSession.FirstPlacer = Match.FirstPlacer;
        BeginRound();
    }

    public void Rematch()
    {
        DuelSession.FirstPlacer = 1 - DuelSession.FirstPlacer;
        DuelSession.MatchNumber++;
        Unhook();
        BeginMatch();
    }

    // ---------------- Yardimcilar ----------------

    private void RefreshPreview()
    {
        // Dizerken sadece ortadaki misketler ve SIRADAKI oyuncunun koydugu
        // gorunur; rakibin o el dizdigi gizli kalir.
        var list = new List<MarbleSpot>();
        var renk = new List<Color>();
        foreach (var m in Match.Marbles)
            if (!m.out_) { list.Add(new MarbleSpot(m.x, m.z)); renk.Add(Renk(m)); }
        foreach (var q in spots[ActivePlayer])
        { list.Add(new MarbleSpot(q.x, q.y)); renk.Add(DuelSession.PlayerColor(ActivePlayer)); }

        data.marbles = list.ToArray();
        arena.Configure(data.shape, data.arenaSize, data.rings, data.triangleRows, data.marbles);
        arena.Rebuild();
        var spawned = arena.SpawnedMarbles;
        for (int i = 0; i < spawned.Count && i < renk.Count; i++)
            if (spawned[i] != null) Tint(spawned[i], renk[i]);
    }

    private void ApplyDuelPhysics()
    {
        // Kaynak malzeme: ortadaki misketlerden biri. Sira belirleme atisinda
        // saha BOS oldugu icin orada atici kendi malzemesini verir, yoksa
        // toss atisi mac atislarindan farkli hissederdi.
        var source = arena != null && arena.SpawnedMarbles.Count > 0
            ? arena.SpawnedMarbles[0].GetComponent<Collider>()?.sharedMaterial : null;
        if (source == null && shooter != null)
        {
            var own = shooter.GetComponent<Collider>();
            if (own != null) source = own.sharedMaterial;
        }
        if (source == null) return;
        if (source == duelMaterial) source = null;   // kendi kendini kaynak almasin
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
        var sc = shooter != null ? shooter.GetComponent<Collider>() : null;
        if (sc != null) sc.sharedMaterial = duelMaterial;
        if (shooter != null) shooter.DuelImpulseScale = DuelSession.Impulse / .65f;
    }

    private void PlaceShooterOnLine()
    {
        if (shooter == null) return;
        shooter.ResetTo(new Vector3(0f, .25f, DuelSession.ShooterZ));
        shooter.ShootingEnabled = true;
        var visual = shooter.GetComponent<MarbleVisual>();
        if (visual != null) visual.SetSkin(DuelSession.Skin[Mathf.Clamp(Match.Turn, 0, 1)]);
    }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorId = Shader.PropertyToID("_Color");

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

    private static List<(float x, float z)> ToTuples(IList<Vector2> v)
    {
        var list = new List<(float, float)>();
        foreach (var s in v) list.Add((s.x, s.y));
        return list;
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

    private void Refresh() { Changed?.Invoke(); }

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

    private void UpdatePlacing()
    {
        var pointer = Pointer.current;
        if (pointer == null) return;
        if (!pointer.press.wasPressedThisFrame) return;

        Vector2 sp = pointer.position.ReadValue();
        // Alt serit buton alani; oradaki dokunus dizme sayilmaz ve uyari da
        // gerektirmez, oyuncu zaten butona basmaya calisiyor.
        if (sp.y < Screen.height * .22f) return;
        if (Camera.main == null) return;
        if (!PointerToGround(out Vector3 w)) return;

        if (!DuelPlacement.Inside(w.x, w.z, DuelSession.ArenaSize))
        { Hint = DuelSession.Triangle ? "Üçgenin içine koy" : "Çizginin içine koy"; return; }

        if (AddSpot(new Vector2(w.x, w.z))) Hint = "";
        else Hint = "Burası dolu, boş bir yer seç";
    }
}
