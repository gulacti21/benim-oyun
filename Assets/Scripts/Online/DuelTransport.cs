using System;
using System.Collections.Generic;

// Paketleri karsi tarafa TASIYAN katman. Oyun burayi bilmez, sadece
// "gonder" der ve "geldi" olayini dinler. Boylece tasiyici degistiginde
// (bugun loopback, yarin Unity Relay, obur gun baska bir sey) oyun kodu
// hic degismez.
public interface IDuelTransport
{
    bool Connected { get; }
    int LocalPlayer { get; }          // 0 = odayi kuran, 1 = katilan
    void Send(byte[] data);
    event Action<byte[]> Received;
    void Close();
}

// Tek surecte iki ucu birbirine baglar. Ag YOK.
//
// Ne ise yarar: online kurallarinin tamami bulut hesabi olmadan burada
// test edilebiliyor. DuelNetVerify tam bir maci bu tasiyici uzerinden
// oynatip iki tarafin ayni durumda bittigini dogruluyor. Gercek tasiyici
// geldiginde degisen tek sey bu sinifin yerine gecmesi olacak.
public class LoopbackTransport : IDuelTransport
{
    private LoopbackTransport peer;
    private readonly List<byte[]> inbox = new List<byte[]>();

    public bool Connected => peer != null;
    public int LocalPlayer { get; private set; }
    public event Action<byte[]> Received;

    // Anlik teslim yerine kuyruga koyma secenegi: gecikmeli agi taklit eder.
    // Testte ikisi de denenir; kural sonucu ikisinde de ayni olmali.
    public bool Buffered { get; set; }

    public static (LoopbackTransport host, LoopbackTransport guest) Pair()
    {
        var a = new LoopbackTransport { LocalPlayer = 0 };
        var b = new LoopbackTransport { LocalPlayer = 1 };
        a.peer = b; b.peer = a;
        return (a, b);
    }

    public void Send(byte[] data)
    {
        if (peer == null || data == null) return;
        // Kopyalayarak gonder: gonderen paketi sonradan degistirirse
        // alanin eline gecen sey degismesin.
        var copy = new byte[data.Length];
        Buffer.BlockCopy(data, 0, copy, 0, data.Length);
        if (peer.Buffered) peer.inbox.Add(copy);
        else peer.Deliver(copy);
    }

    // Kuyruktakileri sirayla teslim eder. Sira KORUNUR: sira tabanli
    // oyunda mesajlarin sirasi kurali degistirir.
    public int Flush()
    {
        int n = inbox.Count;
        for (int i = 0; i < inbox.Count; i++) Deliver(inbox[i]);
        inbox.Clear();
        return n;
    }

    private void Deliver(byte[] data) => Received?.Invoke(data);

    public void Close() { peer = null; inbox.Clear(); Received = null; }
}
