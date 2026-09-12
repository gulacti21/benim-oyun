using System;
using System.Collections.Generic;

// ONLINE DUELLO OTURUMU.
//
// Bir tasiyici (IDuelTransport) alir, gelen paketleri KURAL MOTORUNA uygular,
// yerel oyuncunun yaptiklarini karsi tarafa yollar. Unity'ye bagli degildir:
// sahne, arayuz, fizik bilmez. Bu yuzden editorde bastan sona test edilebilir.
//
// TEMEL KURAL: bir hamleyi SADECE o hamlenin sahibi uretir.
// Atan taraf fizigi kendinde calistirir, sonucu gonderir; karsi taraf o
// sonucu uygular. Iki tarafta ayni fizigin ayni sonucu vermesini BEKLEMIYORUZ,
// cunku PhysX cihazdan cihaza birebir ayni degil. Kural motoru ayni kalir,
// gorunen animasyon birazcik farkli olabilir -- sira tabanli oyunda bu
// fark edilmez.
//
// Kendi hamlesini baskasi adina uretmeye calisan paket REDDEDILIR: karsi
// taraftan gelen paket, gonderenin oyuncusu disinda bir oyuncu adina
// konusamaz.
public class DuelNetSession
{
    public IDuelTransport Transport { get; private set; }
    public DuelMatch Match { get; private set; }
    public DuelToss Toss { get; private set; }

    public int LocalPlayer => Transport != null ? Transport.LocalPlayer : 0;
    public int RemotePlayer => 1 - LocalPlayer;
    public bool HandshakeDone { get; private set; }
    public bool PeerLeft { get; private set; }

    // Uyusmazlik sayaci: reddedilen paketler. Sifirdan buyukse bir taraf
    // ya eski surum ya da bozuk veri gonderiyor.
    public int Rejected { get; private set; }

    // Oyun akisini dinleyen taraf (arayuz) icin.
    public event Action<DuelNet.Packet> Applied;
    public event Action HandshakeChanged;

    private readonly int gameType;
    private bool helloSent, helloEchoed;

    public DuelNetSession(IDuelTransport transport, int gameType)
    {
        Transport = transport;
        this.gameType = gameType;
        Toss = new DuelToss();
        Toss.ResetTo(0);
        Transport.Received += OnBytes;
    }

    // El sikismayi baslatir. Kurucudan AYRI, cunku kurucuda gonderilen
    // paket karsi taraf henuz dinlemeye baslamadiysa kayboluyor -- ilk
    // surumde el sikisma tam bu yuzden hic tamamlanmadi.
    public void Begin()
    {
        if (helloSent) return;
        helloSent = true;
        Send(DuelNet.Hello(LocalPlayer, gameType));
    }

    public bool IsMyTurnToToss => !Toss.Finished && Toss.TurnOf == LocalPlayer;
    public bool IsMyTurnToShoot => Match != null && Match.State == DuelMatch.Phase.Shooting
                                                && Match.Turn == LocalPlayer;
    public bool IsMyTurnToPlace => Match != null && Match.State == DuelMatch.Phase.Placing
                                                 && !Match.HasPlaced(LocalPlayer);

    // ---------------- Yerel hamleler ----------------
    // Her biri once AGA yollar, sonra kendinde uygular. Sira onemli degil
    // (tasiyici eszamansiz), ama ikisi de yapilmali.

    public void LocalToss(float x, float z, bool fell)
    {
        if (!IsMyTurnToToss) return;
        var p = DuelNet.Toss(LocalPlayer, x, z, fell);
        Send(p); Apply(p, true);
    }

    public void LocalRight(bool used)
    {
        var p = DuelNet.Right(LocalPlayer, used);
        Send(p); Apply(p, true);
    }

    public void LocalPlace(IList<(float x, float z)> spots)
    {
        if (!IsMyTurnToPlace) return;
        var p = DuelNet.Place(LocalPlayer, spots);
        Send(p); Apply(p, true);
    }

    public void LocalShot(IList<int> knocked, bool stranded, float sx, float sz)
    {
        if (!IsMyTurnToShoot) return;
        var p = DuelNet.Shot(LocalPlayer, knocked, stranded, sx, sz);
        Send(p); Apply(p, true);
    }

    public void LocalNextRound()
    {
        var p = DuelNet.Simple(DuelNet.Kind.NextRound, LocalPlayer);
        Send(p); Apply(p, true);
    }

    public void LocalRematch()
    {
        var p = DuelNet.Simple(DuelNet.Kind.Rematch, LocalPlayer);
        Send(p); Apply(p, true);
    }

    public void LocalLeave()
    {
        Send(DuelNet.Simple(DuelNet.Kind.Leave, LocalPlayer));
        Transport?.Close();
    }

    private void Send(DuelNet.Packet p) => Transport?.Send(DuelNet.Encode(p));

    // ---------------- Gelen paket ----------------

    private void OnBytes(byte[] data)
    {
        if (!DuelNet.Decode(data, out var p)) { Rejected++; return; }
        Apply(p, false);
    }

    // local: paketi biz urettik. Uzaktan gelen paket BASKASI adina konusamaz.
    private void Apply(DuelNet.Packet p, bool local)
    {
        if (!local && p.player == LocalPlayer) { Rejected++; return; }

        switch (p.kind)
        {
            case DuelNet.Kind.Hello:
                if (local) return;
                // Surum uyusmazliginda oyun BASLAMAZ. Sessizce yanlis
                // oynamaktansa acikca baglanmamak iyidir.
                if (p.number != DuelNet.Protocol) { Rejected++; return; }
                HandshakeDone = true;
                // Sonradan katilan taraf bizim ilk selamimizi kacirmis
                // olabilir; bir kez daha yolla. Bir kez, yoksa iki taraf
                // birbirine sonsuza kadar selam verir.
                if (!helloEchoed) { helloEchoed = true; Begin(); Send(DuelNet.Hello(LocalPlayer, gameType)); }
                HandshakeChanged?.Invoke();
                break;

            case DuelNet.Kind.Toss:
                if (Toss.Finished || Toss.TurnOf != p.player) { Rejected++; return; }
                Toss.RecordFor(p.player, DuelToss.Measure(p.x, p.z, p.number == 1));
                if (Toss.Finished && Match == null) Match = new DuelMatch(Toss.FirstPlacerOf);
                break;

            case DuelNet.Kind.Right:
                if (Match == null) { Rejected++; return; }
                if (p.number == 1 && !Match.UsePlacementRight(p.player)) { Rejected++; return; }
                break;

            case DuelNet.Kind.Place:
                if (Match == null) { Rejected++; return; }
                if (!Match.Place(p.player, DuelNet.ReadSpots(p))) { Rejected++; return; }
                break;

            case DuelNet.Kind.Shot:
                if (Match == null || Match.State != DuelMatch.Phase.Shooting) { Rejected++; return; }
                // Sirasi gelmeyen oyuncunun atisi kabul edilmez.
                if (Match.Turn != p.player) { Rejected++; return; }
                // Kuyuda kaybedilen atici ortada hedef OLMAZ: cukurun dibinde.
                Match.ResolveShot(p.knocked, p.flag, p.x, p.z, !DuelSession.Well);
                break;

            // "Sonraki el" ve "rovans" IKI oyuncu da basar. Ikincisi geldiginde
            // durum zaten ilerlemis olur; bu bir hata degil, TEKRARdir --
            // sessizce yutulur, uyusmazlik sayaci artmaz.
            case DuelNet.Kind.NextRound:
                if (Match == null || Match.State != DuelMatch.Phase.RoundOver) return;
                Match.NextRound();
                break;

            case DuelNet.Kind.Rematch:
                if (Match == null || Match.State != DuelMatch.Phase.Finished) return;
                Match = Match.Rematch();
                break;

            case DuelNet.Kind.Leave:
                if (!local) PeerLeft = true;
                break;
        }

        Applied?.Invoke(p);
    }

    public void Dispose()
    {
        if (Transport != null) { Transport.Received -= OnBytes; Transport.Close(); }
        Transport = null;
    }
}
