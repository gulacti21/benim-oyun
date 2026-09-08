using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Level_00", menuName = "MISKETR/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Identity")]
    public string levelName = "Seviye 1";

    [Header("Arena")]
    public ArenaShape shape = ArenaShape.Triangle;

    [FormerlySerializedAs("circleRadius")]
    public float arenaSize = 3f;

    [Min(1)] public int triangleRows = 4;
    public MarbleRing[] rings = new MarbleRing[]
    {
        new MarbleRing { count = 1, radiusFactor = 0f, angleOffset = 0f },
        new MarbleRing { count = 6, radiusFactor = 0.38f, angleOffset = 0f },
        new MarbleRing { count = 8, radiusFactor = 0.72f, angleOffset = 22.5f }
    };

    [Header("Rules")]
    [Min(1)] public int shotCount = 5;

    [Header("Stars (marbles knocked out of the circle)")]
    [Min(1)] public int oneStarTarget = 4;
    [Min(1)] public int twoStarTarget = 8;
    [Min(1)] public int threeStarTarget = 12;

    [Header("Shooter")]
    public Vector3 shooterStartPosition = new Vector3(0f, 0.25f, -4f);

    public int GetStars(int scoredMarbles)
    {
        if (scoredMarbles >= threeStarTarget)
        {
            return 3;
        }

        if (scoredMarbles >= twoStarTarget)
        {
            return 2;
        }

        if (scoredMarbles >= oneStarTarget)
        {
            return 1;
        }

        return 0;
    }
}
