using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MarbleArena : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField] private ArenaShape shape = ArenaShape.Triangle;
    [SerializeField] private float size = 3f;
    [SerializeField] private int circleSegments = 72;
    [SerializeField] private float outlineHeight = 0.02f;
    [SerializeField] private float outlineWidth = 0.08f;
    [SerializeField] private Color outlineColor = new Color(1f, 1f, 1f, 0.9f);

    [Header("Marbles")]
    [SerializeField] private GameObject targetMarblePrefab;
    [SerializeField] private float spawnHeight = 0.25f;
    [SerializeField] private float marbleSpacing = 0.62f;
    [SerializeField] private int triangleRows = 4;
    [SerializeField] private MarbleRing[] spawnRings = new MarbleRing[]
    {
        new MarbleRing { count = 1, radiusFactor = 0f, angleOffset = 0f },
        new MarbleRing { count = 6, radiusFactor = 0.38f, angleOffset = 0f },
        new MarbleRing { count = 8, radiusFactor = 0.72f, angleOffset = 22.5f }
    };

    [Header("Scoring")]
    [SerializeField] private float exitMargin = 0.25f;
    [SerializeField] private float restSpeedThreshold = 0.15f;

    private readonly List<TargetMarble> activeMarbles = new List<TargetMarble>();
    private readonly List<TargetMarble> spawnedMarbles = new List<TargetMarble>();
    private readonly Vector3[] triangleCorners = new Vector3[3];
    private LineRenderer outline;
    private int score;
    private int totalMarbles;

    public event Action<int, int> ScoreChanged;

    public int Score => score;
    public int TotalMarbles => totalMarbles;
    public int RemainingMarbles => activeMarbles.Count;
    public ArenaShape Shape => shape;
    public float Size => size;

    private void Awake()
    {
        outline = GetComponent<LineRenderer>();
        RefreshOutline();
    }

    private void FixedUpdate()
    {
        for (int i = activeMarbles.Count - 1; i >= 0; i--)
        {
            TargetMarble marble = activeMarbles[i];

            if (marble == null)
            {
                activeMarbles.RemoveAt(i);
                continue;
            }

            if (!IsOutside(marble.transform.position))
            {
                continue;
            }

            marble.MarkScored();
            activeMarbles.RemoveAt(i);
            score++;
            ScoreChanged?.Invoke(score, totalMarbles);
        }
    }

    public void Configure(ArenaShape newShape, float newSize, MarbleRing[] rings, int rows)
    {
        shape = newShape;
        size = Mathf.Max(0.5f, newSize);

        if (rings != null && rings.Length > 0)
        {
            spawnRings = rings;
        }

        if (rows > 0)
        {
            triangleRows = rows;
        }

        RefreshOutline();
    }

    public void Rebuild()
    {
        ClearMarbles();
        SpawnMarbles();
    }

    public bool AllMarblesAtRest()
    {
        for (int i = 0; i < spawnedMarbles.Count; i++)
        {
            TargetMarble marble = spawnedMarbles[i];

            if (marble == null || marble.Body == null)
            {
                continue;
            }

            if (marble.Body.linearVelocity.magnitude > restSpeedThreshold)
            {
                return false;
            }
        }

        return true;
    }

    public bool IsOutside(Vector3 worldPosition)
    {
        return shape == ArenaShape.Triangle
            ? IsOutsideTriangle(worldPosition)
            : IsOutsideCircle(worldPosition);
    }

    private bool IsOutsideCircle(Vector3 worldPosition)
    {
        Vector3 center = transform.position;
        float dx = worldPosition.x - center.x;
        float dz = worldPosition.z - center.z;
        float limit = size + exitMargin;

        return (dx * dx) + (dz * dz) > limit * limit;
    }

    private bool IsOutsideTriangle(Vector3 worldPosition)
    {
        UpdateTriangleCorners();

        Vector2 point = new Vector2(worldPosition.x, worldPosition.z);
        Vector2 a = new Vector2(triangleCorners[0].x, triangleCorners[0].z);
        Vector2 b = new Vector2(triangleCorners[1].x, triangleCorners[1].z);
        Vector2 c = new Vector2(triangleCorners[2].x, triangleCorners[2].z);

        float d1 = EdgeDistance(point, a, b);
        float d2 = EdgeDistance(point, b, c);
        float d3 = EdgeDistance(point, c, a);

        float maxOutside = Mathf.Max(d1, Mathf.Max(d2, d3));

        return maxOutside > exitMargin;
    }

    private static float EdgeDistance(Vector2 point, Vector2 from, Vector2 to)
    {
        Vector2 edge = to - from;
        Vector2 normal = new Vector2(edge.y, -edge.x).normalized;

        return Vector2.Dot(point - from, normal);
    }

    private void UpdateTriangleCorners()
    {
        Vector3 center = transform.position;

        for (int i = 0; i < 3; i++)
        {
            float angle = (270f + (i * 120f)) * Mathf.Deg2Rad;
            triangleCorners[i] = new Vector3(
                center.x + Mathf.Cos(angle) * size,
                center.y,
                center.z + Mathf.Sin(angle) * size);
        }
    }

    private void RefreshOutline()
    {
        if (outline == null)
        {
            outline = GetComponent<LineRenderer>();
        }

        if (outline == null)
        {
            return;
        }

        outline.useWorldSpace = false;
        outline.loop = true;
        outline.widthMultiplier = outlineWidth;
        outline.startColor = outlineColor;
        outline.endColor = outlineColor;

        if (shape == ArenaShape.Triangle)
        {
            outline.positionCount = 3;

            for (int i = 0; i < 3; i++)
            {
                float angle = (270f + (i * 120f)) * Mathf.Deg2Rad;
                outline.SetPosition(i, new Vector3(Mathf.Cos(angle) * size, outlineHeight, Mathf.Sin(angle) * size));
            }

            return;
        }

        int segments = Mathf.Max(8, circleSegments);
        outline.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            outline.SetPosition(i, new Vector3(Mathf.Cos(angle) * size, outlineHeight, Mathf.Sin(angle) * size));
        }
    }

    private void ClearMarbles()
    {
        for (int i = 0; i < spawnedMarbles.Count; i++)
        {
            if (spawnedMarbles[i] != null)
            {
                Destroy(spawnedMarbles[i].gameObject);
            }
        }

        spawnedMarbles.Clear();
        activeMarbles.Clear();
        score = 0;
        totalMarbles = 0;
    }

    private void SpawnMarbles()
    {
        if (targetMarblePrefab == null)
        {
            Debug.LogError("[MarbleArena] Target marble prefab is not assigned.");
            return;
        }

        if (shape == ArenaShape.Triangle)
        {
            SpawnTriangleRack();
        }
        else
        {
            SpawnCircleRings();
        }

        totalMarbles = activeMarbles.Count;
        ScoreChanged?.Invoke(score, totalMarbles);
    }

    private void SpawnTriangleRack()
    {
        Vector3 center = transform.position;
        int rows = Mathf.Max(1, triangleRows);

        float rowStep = marbleSpacing * 0.87f;
        float startZ = center.z - ((rows - 1) * rowStep * 0.5f);

        for (int row = 0; row < rows; row++)
        {
            int countInRow = row + 1;
            float rowZ = startZ + (row * rowStep);
            float rowStartX = center.x - ((countInRow - 1) * marbleSpacing * 0.5f);

            for (int i = 0; i < countInRow; i++)
            {
                Vector3 position = new Vector3(rowStartX + (i * marbleSpacing), spawnHeight, rowZ);
                SpawnMarbleAt(position, "TargetMarble_r" + row + "_" + i);
            }
        }
    }

    private void SpawnCircleRings()
    {
        Vector3 center = transform.position;

        for (int r = 0; r < spawnRings.Length; r++)
        {
            MarbleRing ring = spawnRings[r];

            if (ring.count <= 0)
            {
                continue;
            }

            float ringRadius = size * Mathf.Clamp01(ring.radiusFactor);

            for (int i = 0; i < ring.count; i++)
            {
                Vector3 offset = Vector3.zero;

                if (ringRadius > 0.001f)
                {
                    float angle = ((i / (float)ring.count) * 360f + ring.angleOffset) * Mathf.Deg2Rad;
                    offset = new Vector3(Mathf.Cos(angle) * ringRadius, 0f, Mathf.Sin(angle) * ringRadius);
                }

                Vector3 position = new Vector3(center.x + offset.x, spawnHeight, center.z + offset.z);
                SpawnMarbleAt(position, "TargetMarble_" + r + "_" + i);
            }
        }
    }

    private void SpawnMarbleAt(Vector3 position, string instanceName)
    {
        GameObject instance = Instantiate(targetMarblePrefab, position, Quaternion.identity, transform);
        instance.name = instanceName;

        TargetMarble marble = instance.GetComponent<TargetMarble>();

        if (marble != null)
        {
            spawnedMarbles.Add(marble);
            activeMarbles.Add(marble);
        }
    }
}
