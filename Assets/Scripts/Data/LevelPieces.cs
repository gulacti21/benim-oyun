using UnityEngine;

// Elle tasarlanmış bir bölümün parçaları. Konumlar çemberin merkezine göre,
// oyun dünyası biriminde verilir. x sağa, z ileri (çemberin uzak tarafına) doğrudur.
[System.Serializable]
public struct MarbleSpot
{
    public float x, z;
    // Harita 2: buzlu / bölünen misket. Varsayılan Normal, Harita 1'in bütün bölümleri Normal.
    public MarbleKind kind;
    public MarbleSpot(float x, float z) { this.x = x; this.z = z; kind = MarbleKind.Normal; }
    public MarbleSpot(float x, float z, MarbleKind kind) { this.x = x; this.z = z; this.kind = kind; }
}

public enum MarbleKind { Normal = 0, Ice = 1, Split = 2, Catapat = 3, Magnet = 4 }   // Catapat: Harita 3 · Magnet: Harita 4

// Harita 4 (Lunapark): Bumper = yuvarlak tampon (çarpan misketi daha hızlı geri fırlatır, Lunapark.cs).
public enum ObstacleKind { Stone = 0, Bumper = 1 }

// Gerçek çarpışan engel. Dekor değildir: misket ona çarpar ve seker.
// Tamponda width = çap (depth kullanılmaz).
[System.Serializable]
public struct ObstacleSpot
{
    public float x, z, width, depth, angle;
    public ObstacleKind kind;
    public ObstacleSpot(float x, float z, float width, float depth, float angle = 0f)
    { this.x = x; this.z = z; this.width = width; this.depth = depth; this.angle = angle; kind = ObstacleKind.Stone; }
    public static ObstacleSpot Bumper(float x, float z, float diameter)
    { var o = new ObstacleSpot(x, z, diameter, diameter); o.kind = ObstacleKind.Bumper; return o; }
}

// HARİTA 2 zemin bölgesi: çemberin merkezine göre, dünya biriminde bir daire.
//   Sand = kum (içinden geçen misket çabuk yavaşlar), Mud = çamur (giren misket
//   saplanıp durur), Pit = çukur (yavaş yuvarlanan hedef misket içine düşer, kaybolur).
// Kurallar ve ölçülmüş sabitler GroundRules'ta.
// Harita 3: Slick = rögar kapağı (kaygan metal: üstünden geçen misket neredeyse yavaşlamaz).
// Harita 4: Belt = yürüyen bant (içindeki misketi angle yönüne taşır; derece, 0 = +x, 90 = +z/ileri).
public enum ZoneKind { Sand = 0, Mud = 1, Pit = 2, Slick = 3, Belt = 4 }

[System.Serializable]
public struct ZoneSpot
{
    public ZoneKind kind;
    public float x, z, radius;
    public float angle;   // sadece Belt
    public ZoneSpot(ZoneKind kind, float x, float z, float radius)
    { this.kind = kind; this.x = x; this.z = z; this.radius = radius; angle = 0f; }
    public ZoneSpot(ZoneKind kind, float x, float z, float radius, float angle)
    { this.kind = kind; this.x = x; this.z = z; this.radius = radius; this.angle = angle; }
}
