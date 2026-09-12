using System.Collections.Generic;
using System.IO;

// DUELLONUN AG PROTOKOLU.
//
// Tasarim karari: FIZIGI AGA TASIMIYORUZ.
// Iki telefonda PhysX'in birebir ayni sonucu vermesi garanti degil (islemci,
// kare suresi, derleyici farklari). Onun yerine ATAN taraf fizigi kendinde
// calistirir, sonucu -- "su misketler cikti, atici surada durdu" -- rakibe
// gonderir. Karsi taraf bu sonucu KURAL MOTORUNA uygular, kendi fizigini
// hakem yapmaz. Boylece iki tarafin kurali her zaman ayni kalir.
//
// Kural motoru (DuelMatch) zaten saf ve deterministik: ayni mesaj dizisi
// iki cihazda ayni duruma goturur. DuelNetVerify tam olarak bunu olcuyor.
//
// Mesajlar kisa: bir atis en fazla birkac on bayt. Sira tabanli oyun oldugu
// icin saniyede bir mesaj bile yeterli, gecikme oyunu bozmaz.
public static class DuelNet
{
    // Protokol surumu. Iki taraf ayni surumde degilse oyun baslamaz --
    // yoksa eski surum yeni mesaji yanlis okuyup sessizce sapitir.
    public const int Protocol = 1;

    public enum Kind : byte
    {
        Hello = 1,       // el sikisma: surum + hangi oyun turu
        Toss = 2,        // sira belirleme atisinin sonucu
        Right = 3,       // ekstra dizme hakki kullanildi mi
        Place = 4,       // dizilis
        Shot = 5,        // atisin sonucu
        NextRound = 6,   // sonraki ele gec
        Rematch = 7,     // rovans
        Leave = 8,       // rakip cikti
    }

    public struct Packet
    {
        public Kind kind;
        public int player;      // mesaji ureten oyuncu (0/1)
        public int number;      // Hello: protokol · Right: 1 kullandi 0 kullanmadi · Toss: 1 dustu
        public float x, z;      // atici/atis konumu
        public bool flag;       // Shot: atici sahada kaldi mi
        public float[] spots;   // Place: x,z ciftleri
        public int[] knocked;   // Shot: cikan misketlerin indeksleri

        public static Packet Of(Kind kind, int player) =>
            new Packet { kind = kind, player = player, spots = Empty, knocked = NoInts };
    }

    private static readonly float[] Empty = new float[0];
    private static readonly int[] NoInts = new int[0];

    public static byte[] Encode(Packet p)
    {
        using (var ms = new MemoryStream())
        using (var w = new BinaryWriter(ms))
        {
            w.Write((byte)p.kind);
            w.Write((byte)(p.player & 1));
            w.Write(p.number);
            w.Write(p.x); w.Write(p.z);
            w.Write(p.flag);

            var spots = p.spots ?? Empty;
            w.Write((ushort)spots.Length);
            foreach (float f in spots) w.Write(f);

            var knocked = p.knocked ?? NoInts;
            w.Write((ushort)knocked.Length);
            foreach (int i in knocked) w.Write((ushort)i);

            w.Flush();
            return ms.ToArray();
        }
    }

    // Bozuk paket oyunu dusurmemeli: false doner, cagiran yok sayar.
    // Ag tarafindan gelen her sey supheli kabul edilir.
    public static bool Decode(byte[] data, out Packet p)
    {
        p = default;
        if (data == null || data.Length < 12) return false;
        try
        {
            using (var ms = new MemoryStream(data))
            using (var r = new BinaryReader(ms))
            {
                byte kind = r.ReadByte();
                if (kind < (byte)Kind.Hello || kind > (byte)Kind.Leave) return false;
                p.kind = (Kind)kind;
                p.player = r.ReadByte() & 1;
                p.number = r.ReadInt32();
                p.x = r.ReadSingle(); p.z = r.ReadSingle();
                p.flag = r.ReadBoolean();

                int n = r.ReadUInt16();
                if (n > 64 || n % 2 != 0) return false;       // x,z ciftleri
                p.spots = new float[n];
                for (int i = 0; i < n; i++) p.spots[i] = r.ReadSingle();

                int k = r.ReadUInt16();
                if (k > 64) return false;
                p.knocked = new int[k];
                for (int i = 0; i < k; i++) p.knocked[i] = r.ReadUInt16();
                return true;
            }
        }
        catch { return false; }
    }

    // --- Kolaylik ureticileri ---

    public static Packet Hello(int player, int gameType)
    {
        var p = Packet.Of(Kind.Hello, player);
        p.number = Protocol; p.x = gameType;
        return p;
    }

    public static Packet Toss(int player, float x, float z, bool fell)
    {
        var p = Packet.Of(Kind.Toss, player);
        p.x = x; p.z = z; p.number = fell ? 1 : 0;
        return p;
    }

    public static Packet Right(int player, bool used)
    {
        var p = Packet.Of(Kind.Right, player);
        p.number = used ? 1 : 0;
        return p;
    }

    public static Packet Place(int player, IList<(float x, float z)> spots)
    {
        var p = Packet.Of(Kind.Place, player);
        var flat = new float[spots.Count * 2];
        for (int i = 0; i < spots.Count; i++) { flat[i * 2] = spots[i].x; flat[i * 2 + 1] = spots[i].z; }
        p.spots = flat;
        return p;
    }

    public static List<(float x, float z)> ReadSpots(Packet p)
    {
        var list = new List<(float, float)>();
        var s = p.spots ?? Empty;
        for (int i = 0; i + 1 < s.Length; i += 2) list.Add((s[i], s[i + 1]));
        return list;
    }

    public static Packet Shot(int player, IList<int> knocked, bool stranded, float sx, float sz)
    {
        var p = Packet.Of(Kind.Shot, player);
        var k = new int[knocked == null ? 0 : knocked.Count];
        for (int i = 0; i < k.Length; i++) k[i] = knocked[i];
        p.knocked = k; p.flag = stranded; p.x = sx; p.z = sz;
        return p;
    }

    public static Packet Simple(Kind kind, int player) => Packet.Of(kind, player);
}
