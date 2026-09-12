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
    // DIZI modu: atis oncesi misketlerin durdugu yerler. Kazanma kosulu
    // "kipirdattigin misket senin" oldugu icin karsilastirma buna gore.
    private readonly Dictionary<TargetMarble, Vector3> rowHome = new Dictionary<TargetMarble, Vector3>();
    // KUYU modunun cukuru. Sadece bu modda yaratilir; kampanya bilmez.
    private DuelHole hole;
    // KUYU modunun kural motoru. Diger modlarda null; DuelMatch ile hicbir
    // ortak yeri yok, iki oyun ayri ayri yasiyor.
    public WellMatch Well { get; private set; }
    // Rakibin sahadaki misketi (kuyu modunda tek hedef odur).
    private TargetMarble wellRival;
    private float settleTimer, elapsed;
    private const float SettleDelay = .4f;

    // AG MODU. Net null ise tek cihazda oynanan mac: asagidaki her sey
    // eskisi gibi calisir. Net doluysa MACIN SAHIBI oturumdur; controller
    // kendi kopyasini tutmaz -- iki kopya tutmak kacinilmaz olarak
    // uyusmazlik uretir.
    public DuelNetSession Net { get; private set; }
    private DuelMatch localMatch;
    public DuelMatch Match => Net != null ? Net.Match : localMatch;

    // Ag modunda sadece SIRASI GELEN oyuncu hamle uretir; digeri izler.
    public bool Online => Net != null;

    // Her modun kendi atis gucu var; hepsi ayri ayri olculdu.
    private static float ModImpulse => DuelSession.Row ? DuelSession.RowImpulse
                                     : DuelSession.Well ? DuelSession.WellImpulse
                                     : DuelSession.Impulse;
    public int MePlayer => Net != null ? Net.LocalPlayer : ActivePlayer;
    public bool Watching
    {
        get
        {
            if (Net == null) return false;
            if (CurrentStep == Step.Toss) return !Net.IsMyTurnToToss;
            if (DuelSession.Well) return Well != null && Well.Turn != Net.LocalPlayer;
            if (Match == null) return false;
            return (CurrentStep == Step.Shoot && Match.Turn != Net.LocalPlayer)
                || (CurrentStep == Step.Place && Match.HasPlaced(Net.LocalPlayer));
        }
    }

    public void AttachNet(DuelNetSession session)
    {
        Net = session;
        if (Net != null) Net.Applied += OnNetApplied;
    }

    // Uzaktan gelen hamle oturumda zaten islendi; burada sadece sahneyi
    // ve ekrani ona gore guncelliyoruz.
    private void OnNetApplied(DuelNet.Packet p)
    {
        if (p.player == Net.LocalPlayer) return;        // kendi hamlemiz zaten islendi
        switch (p.kind)
        {
            case DuelNet.Kind.Toss:
                tossShotTaken = false;
                if (Net.Toss.Finished) { if (shooter != null) shooter.ShootingEnabled = false; }
                else PlaceTossShooter();
                break;
            case DuelNet.Kind.Place:
                orderIndex++;
                if (Match.State == DuelMatch.Phase.Shooting) StartShooting();
                else { RefreshPreview(); }
                break;
            case DuelNet.Kind.Shot:
                SyncAfterShot();
                break;
            case DuelNet.Kind.NextRound:
                DuelSession.FirstPlacer = Match.FirstPlacer;
                BeginRound();
                break;
        }
        Refresh();
    }
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
        if (hole != null) { Destroy(hole.gameObject); hole = null; }
        if (Net != null) Net.Applied -= OnNetApplied;
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
        if (Net == null) DuelToss.Reset(0);
        tossShotTaken = false;
        BuildTossData();
        level.ConfigureForDuel(data);
        EnsureHole();
        ApplyDuelPhysics();
        Hook();
        waiting = false; settleTimer = 0f; elapsed = 0f;
        PlaceTossShooter();
        Refresh();
    }

    private void BuildTossData()
    {
        data.levelName = "SIRA ATIŞI";
        data.shape = DuelSession.Shape;
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
        shooter.ShootingEnabled = !Watching;
        // Sira atisi mac atisindan DAHA HAFIF: bkz. DuelSession.TossImpulseFactor.
        shooter.DuelImpulseScale = DuelSession.TossImpulse / .65f;
        var visual = shooter.GetComponent<MarbleVisual>();
        int tossAtan = Net != null ? Net.Toss.TurnOf : DuelToss.Shooter;
        if (visual != null) visual.SetSkin(DuelSession.Skin[Mathf.Clamp(tossAtan, 0, 1)]);
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
        if (Net != null)
        {
            Net.LocalToss(sp.x, sp.z, dustu);
            if (Net.Toss.Finished) { if (shooter != null) shooter.ShootingEnabled = false; }
            else PlaceTossShooter();
            Refresh();
            return;
        }

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
        if (CurrentStep != Step.Toss) return;
        if (Net != null)
        {
            if (!Net.Toss.Finished) return;
            DuelSession.FirstPlacer = Net.Match != null ? Net.Match.FirstPlacer : Net.Toss.FirstPlacerOf;
            StartMatchProper();
            return;
        }
        if (!DuelToss.Done) return;
        DuelSession.FirstPlacer = DuelToss.FirstPlacer;
        StartMatchProper();
    }

    private void StartMatchProper()
    {
        Unhook();
        if (DuelSession.Well)
        {
            Well = new WellMatch(DuelSession.FirstPlacer);
            BeginWell();
            return;
        }

        // Ag modunda maci oturum kurar (sira atisi bitince); burada degil.
        if (Net == null) localMatch = new DuelMatch(DuelSession.FirstPlacer);
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

            // DIZI modunda dizme asamasi YOK: misketler siraya otomatik
            // dizilir. Modun butun meselesi herkesin ayni siraya bakmasi;
            // dizilis taktigi olsaydi cember modundan farki kalmazdi.
            if (DuelSession.Row) { AutoPlace(p); orderIndex++; continue; }

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
        if (Net != null) Net.LocalRight(true);
        else if (!Match.UsePlacementRight(ActivePlayer)) { ChooseAuto(); return; }
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
        if (Net != null) Net.LocalPlace(ToTuples(son));
        else Match.Place(player, ToTuples(son));
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
        if (Net != null) Net.LocalPlace(ToTuples(spots[me]));
        else Match.Place(me, ToTuples(spots[me]));
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

    // Cukur sadece kuyu modunda ve arena kuruldugunda var olur.
    private void EnsureHole()
    {
        if (!DuelSession.Well)
        {
            if (hole != null) { Destroy(hole.gameObject); hole = null; }
            return;
        }
        if (hole != null || arena == null) return;
        var line = arena.GetComponent<LineRenderer>();
        hole = DuelHole.Create(arena.transform, DuelSession.HoleRadius,
                               line != null ? line.sharedMaterial : null);
    }

    // ---------------- KUYU ----------------

    private void BeginWell()
    {
        CurrentStep = Step.Shoot;
        if (Well == null) Well = new WellMatch(DuelSession.FirstPlacer);
        BuildWellData();
        level.ConfigureForDuel(data);
        EnsureHole();
        ApplyDuelPhysics();
        Hook();
        waiting = false; settleTimer = 0f; elapsed = 0f;
        PlaceWellPieces();
        Refresh();
    }

    // Sahada tek hedef var: RAKIBIN misketi. Atan oyuncununki ShotController'in
    // aticisi; sirasi gelince roller degisiyor.
    private void BuildWellData()
    {
        data.levelName = "KUYU";
        data.shape = ArenaShape.Circle;
        data.arenaSize = DuelSession.WellSize;
        data.shotCount = 999;
        data.oneStarTarget = data.twoStarTarget = data.threeStarTarget = 99;
        data.starsToPass = 1;
        data.obstacles = null; data.obstacleCount = 0;
        data.shooterHalfWidth = 0f; data.shooterOffsetX = 0f;
        data.shooterStartPosition = new Vector3(0f, .25f, DuelSession.ShooterZ);

        int rakip = 1 - Well.Turn;
        if (Well.OnLine(rakip))
        {
            // Rakip henuz ilk atisini yapmadi: sahada degil, cizgide bekliyor.
            data.marbles = new MarbleSpot[0];
        }
        else data.marbles = new[] { new MarbleSpot(Well.X(rakip), Well.Z(rakip)) };
    }

    private void PlaceWellPieces()
    {
        int me = Well.Turn, rakip = 1 - me;

        wellRival = null;
        var spawned = arena.SpawnedMarbles;
        if (spawned.Count > 0 && spawned[0] != null)
        {
            wellRival = spawned[0];
            Tint(wellRival, DuelSession.PlayerColor(rakip));
        }

        if (shooter == null) return;
        Vector3 yer = Well.OnLine(me)
            ? new Vector3(0f, .25f, DuelSession.ShooterZ)
            : new Vector3(Well.X(me), .25f, Well.Z(me));
        shooter.ResetTo(yer);
        shooter.ShootingEnabled = !Watching;
        shooter.DuelImpulseScale = DuelSession.WellImpulse / .65f;
        var visual = shooter.GetComponent<MarbleVisual>();
        if (visual != null) visual.SetSkin(DuelSession.Skin[Mathf.Clamp(me, 0, 1)]);
    }

    private void ResolveWell(Vector3 sp)
    {
        int me = Well.Turn;
        bool cukurda = hole != null && hole.Contains(sp);
        bool disarida = arena != null && arena.IsOutside(sp);

        // Rakibin misketi kipirdadiysa ona VURULMUS demektir. Temas geri
        // cagrisi yerine yer degistirmeye bakiyoruz: zincirle itilmeyi de
        // yakaliyor ve dizi modunda zaten olculmus bir esik var.
        bool vurdu = false;
        Vector3 rakipYer = wellRival != null ? wellRival.transform.position : Vector3.zero;
        if (wellRival != null && rowHome.TryGetValue(wellRival, out Vector3 eski))
            vurdu = Vector3.Distance(rakipYer, eski) > DuelSession.RowNudge;

        var sonuc = disarida ? WellMatch.ShotResult.OutOfField
                  : cukurda ? WellMatch.ShotResult.InHole
                  : vurdu ? WellMatch.ShotResult.Hit
                  : WellMatch.ShotResult.Miss;

        // Cukura giren misket agzin kenarina cikarilir: merkezde birakilsa
        // bir sonraki atista kendiliginden yine "iceride" sayilirdi.
        Vector3 benim = sp;
        if (sonuc == WellMatch.ShotResult.InHole)
        {
            float d = WellMatch.HoleExitDistance(DuelSession.HoleRadius);
            Vector2 yon = new Vector2(sp.x, sp.z);
            yon = yon.sqrMagnitude < .0001f ? new Vector2(0f, -1f) : yon.normalized;
            benim = new Vector3(yon.x * d, .25f, yon.y * d);
        }

        int rakip = 1 - me;
        Vector3 rakipSon = Well.OnLine(rakip) ? new Vector3(Well.X(rakip), .25f, Well.Z(rakip)) : rakipYer;
        // Rakibin misketi sahadan cikmissa o da cizgiye doner.
        if (wellRival != null && arena != null && arena.IsOutside(rakipYer)) rakipSon = Vector3.zero;

        Well.Resolve(sonuc, benim.x, benim.z, rakipSon.x, rakipSon.z);

        if (Well.State == WellMatch.Phase.Finished)
        {
            if (shooter != null) shooter.ShootingEnabled = false;
            CurrentStep = Step.Done;
            Refresh();
            return;
        }

        BuildWellData();
        arena.Configure(data.shape, data.arenaSize, data.rings, data.triangleRows, data.marbles);
        arena.Rebuild();
        ApplyDuelPhysics();
        PlaceWellPieces();
        Refresh();
    }

    private void StartShooting()
    {
        CurrentStep = Step.Shoot;
        BuildLevelData();
        level.ConfigureForDuel(data);
        EnsureHole();
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
        data.shape = DuelSession.Shape;
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

    // DIZI: atistan HEMEN ONCE misketlerin yerini not et.
    private void RememberRow()
    {
        rowHome.Clear();
        if (!(DuelSession.Row || DuelSession.Well) || arena == null) return;
        foreach (var m in arena.SpawnedMarbles)
            if (m != null) rowHome[m] = m.transform.position;
    }

    // Baslangic yerinden RowNudge'dan fazla ayrilan misket KIPIRDAMIS
    // sayilir ve atanin olur.
    private List<int> MovedMarbles()
    {
        var list = new List<int>();
        foreach (var pair in rowHome)
        {
            var m = pair.Key;
            if (m == null) continue;
            if (Vector3.Distance(m.transform.position, pair.Value) <= DuelSession.RowNudge) continue;
            if (indexOf.TryGetValue(m, out int i) && !list.Contains(i)) list.Add(i);
        }
        list.Sort();
        return list;
    }

    private void OnShotFired()
    {
        if (CurrentStep == Step.Toss)
        {
            tossShotTaken = true; settleTimer = 0f; elapsed = 0f;
            Refresh(); return;
        }
        if (!DuelSession.Well && (Match == null || Match.State != DuelMatch.Phase.Shooting)) return;
        if (DuelSession.Well && (Well == null || Well.State != WellMatch.Phase.Shooting)) return;
        knocked.Clear();
        RememberRow();
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
        if (!waiting) return;
        if (!DuelSession.Well && Match == null) return;

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

        // KUYU kendi kural motorunu kullanir (WellMatch): kese yok, SAYI var.
        if (DuelSession.Well)
        {
            ResolveWell(sp);
            return;
        }

        // DIZI: cikarma yok, KIPIRDATMA var. Atici cemberde kalma kurali da
        // yok, cunku cember yok.
        if (DuelSession.Row)
        {
            knocked.Clear();
            knocked.AddRange(MovedMarbles());
            if (Net != null) { Net.LocalShot(knocked, false, sp.x, sp.z); knocked.Clear(); SyncAfterShot(); return; }
            bool devam = Match.ResolveShot(knocked, false, sp.x, sp.z);
            knocked.Clear();
            if (Match.State != DuelMatch.Phase.Shooting)
            {
                if (shooter != null) shooter.ShootingEnabled = false;
                CurrentStep = Match.State == DuelMatch.Phase.Finished ? Step.Done : Step.RoundOver;
                Refresh(); return;
            }
            // Kipirdayan misket kazanildi, kipirdamayan YERINE DONER.
            // Boylece sira her atista ayni kalir: hem kural net, hem de
            // agda iki cihaz ayni diziliste bulusur.
            RebuildRing();
            PlaceShooterOnLine();
            Refresh();
            return;
        }

        float sr = new Vector2(sp.x, sp.z).magnitude;
        // Ucgenin ic yaricapi kenar yaricapinin yarisi kadardir; tehlike
        // bolgesi de ona gore olceklenir ki iki modda ayni oranda tetiklensin.
        float tehlike = DuelSession.ArenaSize * DuelSession.StrandRadiusFactor * (DuelSession.Triangle ? .5f : 1f);
        bool stranded = shooter != null && sr <= tehlike;

        if (Net != null)
        {
            // Ag modunda atisin sonucunu OTURUM isler ve rakibe yollar;
            // sahneyi guncelleme isi ortak SyncAfterShot'ta.
            Net.LocalShot(knocked, stranded, sp.x, sp.z);
            knocked.Clear();
            SyncAfterShot();
            return;
        }

        bool sameTurn = Match.ResolveShot(knocked, stranded, sp.x, sp.z);
        knocked.Clear();
        AfterShot(sameTurn, stranded);
    }

    // Atistan sonra sahne ve adim. Tek cihazda zincir bilgisi elimizde;
    // ag modunda sira degisip degismedigine bakilir.
    private void AfterShot(bool sameTurn, bool stranded)
    {
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

    // Ag modunda atistan sonra: zinciri, sirayi ve cemberi mac durumundan
    // tureterek sahneyi yeniden esler. Kimin attigi onemli degil, iki
    // tarafta ayni kod calisir.
    private void SyncAfterShot()
    {
        if (Match == null) return;
        if (Match.State != DuelMatch.Phase.Shooting)
        {
            if (shooter != null) shooter.ShootingEnabled = false;
            CurrentStep = Match.State == DuelMatch.Phase.Finished ? Step.Done : Step.RoundOver;
            Refresh();
            return;
        }
        RebuildRing();
        PlaceShooterOnLine();
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
        if (Net != null) { Net.LocalNextRound(); if (Match.State == DuelMatch.Phase.Placing) { DuelSession.FirstPlacer = Match.FirstPlacer; BeginRound(); } return; }
        Match.NextRound();
        if (Match.State == DuelMatch.Phase.Finished) { CurrentStep = Step.Done; Refresh(); return; }
        DuelSession.FirstPlacer = Match.FirstPlacer;
        BeginRound();
    }

    public void Rematch()
    {
        if (DuelSession.Well)
        {
            DuelSession.MatchNumber++;
            Well = Well != null ? Well.Rematch() : new WellMatch(0);
            DuelSession.FirstPlacer = Well.Starter;
            Unhook();
            BeginWell();
            return;
        }

        if (Net != null)
        {
            Net.LocalRematch();
            Unhook();
            DuelSession.FirstPlacer = Match != null ? Match.FirstPlacer : 0;
            BeginRound();
            return;
        }
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
        if (shooter != null)
            shooter.DuelImpulseScale = ModImpulse / .65f;
    }

    private void PlaceShooterOnLine()
    {
        if (shooter == null) return;
        shooter.ResetTo(new Vector3(0f, .25f, DuelSession.ShooterZ));
        // Ag modunda sadece sirasi gelen oyuncu atar; digerinde atis kapali,
        // yoksa iki taraf ayni anda atip mac catallanir.
        shooter.ShootingEnabled = !Watching;
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
