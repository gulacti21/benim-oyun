using UnityEngine;

// Elle tasarlanmış bir bölümün parçaları. Konumlar çemberin merkezine göre,
// oyun dünyası biriminde verilir. x sağa, z ileri (çemberin uzak tarafına) doğrudur.
[System.Serializable]
public struct MarbleSpot
{
    public float x, z;
    public MarbleSpot(float x, float z) { this.x = x; this.z = z; }
}

// Gerçek çarpışan engel. Dekor değildir: misket ona çarpar ve seker.
[System.Serializable]
public struct ObstacleSpot
{
    public float x, z, width, depth, angle;
    public ObstacleSpot(float x, float z, float width, float depth, float angle = 0f)
    { this.x = x; this.z = z; this.width = width; this.depth = depth; this.angle = angle; }
}
