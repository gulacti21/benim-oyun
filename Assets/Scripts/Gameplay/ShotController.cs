using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class ShotController : MonoBehaviour
{
    [Header("Aim")]
    [SerializeField] private float grabRadius = 2f;
    [SerializeField] private float minPullDistance = 0.2f;
    [SerializeField] private float maxPullDistance = 3.5f;

    [Header("Shot")]
    [SerializeField] private float maxShotImpulse = 0.5f;

    [Header("Rest Detection")]
    [SerializeField] private float restSpeedThreshold = 0.2f;

    [Header("References")]
    [SerializeField] private Camera gameCamera;
    [SerializeField] private AimIndicator aimIndicator;

    private Rigidbody body;
    private Plane aimPlane;
    private bool isAiming;
    private Vector3 pullPoint;
    private Vector3 pendingImpulse;
    private bool hasPendingImpulse;

    public event Action ShotFired;

    public bool IsAiming => isAiming;
    public bool ShootingEnabled { get; set; } = true;
    public bool AtRest => IsAtRest();

    public void ResetTo(Vector3 position)
    {
        isAiming = false;
        hasPendingImpulse = false;
        pendingImpulse = Vector3.zero;

        if (aimIndicator != null)
        {
            aimIndicator.Hide();
        }

        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(position, Quaternion.identity);
        body.position = position;
        body.rotation = Quaternion.identity;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        if (gameCamera == null)
        {
            gameCamera = Camera.main;
        }
    }

    private void Update()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null || gameCamera == null)
        {
            return;
        }

        aimPlane = new Plane(Vector3.up, transform.position);

        if (pointer.press.wasPressedThisFrame)
        {
            BeginAim(pointer.position.ReadValue());
        }
        else if (isAiming && pointer.press.wasReleasedThisFrame)
        {
            ReleaseAim();
        }
        else if (isAiming && pointer.press.isPressed)
        {
            UpdateAim(pointer.position.ReadValue());
        }
    }

    private void FixedUpdate()
    {
        if (!hasPendingImpulse)
        {
            return;
        }

        hasPendingImpulse = false;
        body.AddForce(pendingImpulse, ForceMode.Impulse);
    }

    private bool IsAtRest()
    {
        return body.linearVelocity.magnitude <= restSpeedThreshold
            && body.angularVelocity.magnitude <= restSpeedThreshold * 4f;
    }

    private void BeginAim(Vector2 screenPosition)
    {
        if (!ShootingEnabled)
        {
            return;
        }

        if (!IsAtRest())
        {
            return;
        }

        if (!TryGetPlanePoint(screenPosition, out Vector3 world))
        {
            return;
        }

        Vector3 marbleFlat = transform.position;
        marbleFlat.y = world.y;

        if (Vector3.Distance(world, marbleFlat) > grabRadius)
        {
            return;
        }

        isAiming = true;
        pullPoint = world;
    }

    private void UpdateAim(Vector2 screenPosition)
    {
        if (!TryGetPlanePoint(screenPosition, out Vector3 world))
        {
            return;
        }

        pullPoint = world;
        GetShot(out Vector3 direction, out float power);

        if (aimIndicator != null)
        {
            aimIndicator.Show(transform.position, direction, power);
        }
    }

    private void ReleaseAim()
    {
        isAiming = false;

        if (aimIndicator != null)
        {
            aimIndicator.Hide();
        }

        GetShot(out Vector3 direction, out float power);

        if (power <= 0f)
        {
            return;
        }

        pendingImpulse = direction * (maxShotImpulse * power);
        hasPendingImpulse = true;
        ShotFired?.Invoke();
    }

    private void GetShot(out Vector3 direction, out float normalizedPower)
    {
        Vector3 marbleFlat = transform.position;
        marbleFlat.y = pullPoint.y;

        Vector3 pull = marbleFlat - pullPoint;
        pull.y = 0f;

        float distance = pull.magnitude;

        if (distance < minPullDistance)
        {
            direction = distance > 0.0001f ? pull / distance : transform.forward;
            normalizedPower = 0f;
            return;
        }

        float clamped = Mathf.Min(distance, maxPullDistance);
        direction = pull / distance;
        normalizedPower = Mathf.Clamp01((clamped - minPullDistance) / (maxPullDistance - minPullDistance));
    }

    private bool TryGetPlanePoint(Vector2 screenPosition, out Vector3 world)
    {
        Ray ray = gameCamera.ScreenPointToRay(screenPosition);

        if (aimPlane.Raycast(ray, out float enter))
        {
            world = ray.GetPoint(enter);
            return true;
        }

        world = Vector3.zero;
        return false;
    }
}
