using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CircleArena : MonoBehaviour
{
    [Header("Arena")]
    [SerializeField] private float radius = 3f;
    [SerializeField] private int circleSegments = 72;
    [SerializeField] private float circleHeight = 0.02f;
    [SerializeField] private float circleWidth = 0.08f;
    [SerializeField] private Color circleColor = new Color(1f, 1f, 1f, 0.9f);

    [Header("Marbles")]
    [SerializeField] private GameObject targetMarblePrefab;
    [SerializeField] private float spawnHeight = 0.25f;
    [SerializeField] private bool spawnOnStart;
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
    private LineRenderer circleLine;
    private int score;
    private int totalMarbles;

    public event Action<int, int> ScoreChanged;

    public int Score => score;
    public int TotalMarbles => totalMarbles;
    public int RemainingMarbles => activeMarbles.Count;
    public float Radius => radius;

    private void Awake()
    {
        circleLine = GetComponent<LineRenderer>();
        DrawCircle();
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            Rebuild();
        }
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

    public void Configure(float newRadius, MarbleRing[] rings)
    {
        radius = Mathf.Max(0.5f, newRadius);

        if (rings != null && rings.Length > 0)
        {
            spawnRings = rings;
        }

        DrawCircle();
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
        Vector3 center = transform.position;
        float dx = worldPosition.x - center.x;
        float dz = worldPosition.z - center.z;
        float limit = radius + exitMargin;

        return (dx * dx) + (dz * dz) > limit * limit;
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

    private void DrawCircle()
    {
        if (circleLine == null)
        {
            circleLine = GetComponent<LineRenderer>();
        }

        if (circleLine == null)
        {
            return;
        }

        int segments = Mathf.Max(8, circleSegments);

        circleLine.useWorldSpace = false;
        circleLine.loop = true;
        circleLine.widthMultiplier = circleWidth;
        circleLine.startColor = circleColor;
        circleLine.endColor = circleColor;
        circleLine.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            circleLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, circleHeight, Mathf.Sin(angle) * radius));
        }
    }

    private void SpawnMarbles()
    {
        if (targetMarblePrefab == null)
        {
            Debug.LogError("[CircleArena] Target marble prefab is not assigned.");
            return;
        }

        Vector3 center = transform.position;

        for (int r = 0; r < spawnRings.Length; r++)
        {
            MarbleRing ring = spawnRings[r];

            if (ring.count <= 0)
            {
                continue;
            }

            float ringRadius = radius * Mathf.Clamp01(ring.radiusFactor);

            for (int i = 0; i < ring.count; i++)
            {
                Vector3 offset = Vector3.zero;

                if (ringRadius > 0.001f)
                {
                    float angle = ((i / (float)ring.count) * 360f + ring.angleOffset) * Mathf.Deg2Rad;
                    offset = new Vector3(Mathf.Cos(angle) * ringRadius, 0f, Mathf.Sin(angle) * ringRadius);
                }

                Vector3 position = new Vector3(center.x + offset.x, spawnHeight, center.z + offset.z);

                GameObject instance = Instantiate(targetMarblePrefab, position, Quaternion.identity, transform);
                instance.name = "TargetMarble_" + r + "_" + i;

                TargetMarble marble = instance.GetComponent<TargetMarble>();

                if (marble != null)
                {
                    spawnedMarbles.Add(marble);
                    activeMarbles.Add(marble);
                }
            }
        }

        totalMarbles = activeMarbles.Count;
        ScoreChanged?.Invoke(score, totalMarbles);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.8f);
        Vector3 center = transform.position;
        int segments = 48;
        Vector3 previous = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 current = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(previous, current);
            previous = current;
        }
    }
}
