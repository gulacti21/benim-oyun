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

public enum MarbleKind { Normal = 0, Ice = 1, Split = 2 }

// Gerçek çarpışan engel. Dekor değildir: misket ona çarpar ve seker.
[System.Serializable]
public struct ObstacleSpot
{
    public float x, z, width, depth, angle;
    public ObstacleSpot(float x, float z, float width, float depth, float angle = 0f)
    { this.x = x; this.z = z; this.width = width; this.depth = depth; this.angle = angle; }
}

// HARİTA 2 zemin bölgesi: çemberin merkezine göre, dünya biriminde bir daire.
//   Sand = kum (içinden geçen misket çabuk yavaşlar), Mud = çamur (giren misket
//   saplanıp durur), Pit = çukur (yavaş yuvarlanan hedef misket içine düşer, kaybolur).
// Kurallar ve ölçülmüş sabitler GroundRules'ta.
public enum ZoneKind { Sand = 0, Mud = 1, Pit = 2 }

[System.Serializable]
public struct ZoneSpot
{
    public ZoneKind kind;
    public float x, z, radius;
    public ZoneSpot(ZoneKind kind, float x, float z, float radius)
    { this.kind = kind; this.x = x; this.z = z; this.radius = radius; }
}
