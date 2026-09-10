using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody))]
public class ShotController : MonoBehaviour
{
    [SerializeField] private float grabRadius = 1.15f;
    [SerializeField] private float minPullDistance = .15f;
    [SerializeField] private float maxPullDistance = 2.6f;
    [SerializeField] private float maxShotImpulse = .65f;
    [SerializeField] private float restSpeedThreshold = .2f;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private AimIndicator aimIndicator;
    [SerializeField] private ShooterLine shooterLine;
    private Rigidbody body;
    private SpecialMarblePhysics special;
    private int pendingWearSkin;
    private MarblePower pendingWearPower;
    public int ActiveSkin=>special!=null?special.ActiveSkin:MahalleProfile.EffectiveSkin;
    private Plane aimPlane;
    private bool isAiming, hasPendingImpulse, shootingEnabled = true;
    private Vector3 pullPoint, pendingImpulse, defaultScale;
    private float defaultMass;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    private bool keepsPosition, positionLocked, anchorUsedStock;
    public event Action ShotFired;
    public bool IsAiming => isAiming;
    // Bu atıştan sonra misket çizgiye dönmeyecek mi?
    public bool KeepsPosition => keepsPosition;
    public bool AnchorUsedStock => anchorUsedStock;
    // Misket çemberin içinde beklerken çizgiye dokunarak yer değiştirilemez.
    public bool PositionLocked => positionLocked;
    public float Power { get; private set; }
    public MarblePower SelectedPower { get; private set; } = MarblePower.None;
    public bool ShootingEnabled
    {
        get => shootingEnabled;
        set { shootingEnabled = value; if (!value) CancelAim(); }
    }
    public bool AtRest => body != null && (body.IsSleeping() || (body.linearVelocity.magnitude <= restSpeedThreshold && body.angularVelocity.magnitude <= restSpeedThreshold * 4f));
    private void Awake()
    {
        body = GetComponent<Rigidbody>(); defaultScale = transform.localScale; defaultMass = body.mass;
        if (gameCamera == null) gameCamera = Camera.main;
        special=GetComponent<SpecialMarblePhysics>();if(special==null)special=gameObject.AddComponent<SpecialMarblePhysics>();
        grabRadius = 1.15f; maxPullDistance = 2.6f;
    }
    public void ResetTo(Vector3 position)
    {
        CancelAim(); hasPendingImpulse = false; pendingImpulse = Vector3.zero;
        if (body == null) Awake();
        SelectedPower = MarblePower.None; special.Apply(SelectedPower);
        keepsPosition = false; positionLocked = false; anchorUsedStock = false;
        body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
        position.y=transform.localScale.y*.5f+.01f;
        transform.SetPositionAndRotation(position, Quaternion.identity); body.position = position; body.rotation = Quaternion.identity;
        var visual = GetComponent<MarbleVisual>(); if (visual != null) visual.SetSkin(MahalleProfile.EffectiveSkin);
    }

    // "Yerinde Kal" kullanıldığında misket bulunduğu noktada bırakılır.
    // Sadece bir tur geçerlidir: sonraki atıştan sonra yine çizgiye döner.
    public void HoldPosition()
    {
        CancelAim(); hasPendingImpulse = false; pendingImpulse = Vector3.zero;
        if (body == null) Awake();
        SelectedPower = MarblePower.None; special.Apply(SelectedPower);
        keepsPosition = false; anchorUsedStock = false; positionLocked = true;
        body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
        Vector3 rest = body.position; rest.y = transform.localScale.y * .5f + .01f;
        body.position = rest; transform.position = rest; body.rotation = Quaternion.identity;
        var visual = GetComponent<MarbleVisual>(); if (visual != null) visual.SetSkin(MahalleProfile.EffectiveSkin);
    }
    public bool SelectPower(MarblePower power)
    {
        if (!ShootingEnabled || isAiming || !AtRest) return false;
        if (power != MarblePower.None && !MahalleProfile.CanUse(power)) return false;
        SelectedPower = SelectedPower == power ? MarblePower.None : power;
        special.Apply(SelectedPower);
        Vector3 p = body.position; p.y = transform.localScale.y * .5f + .01f; body.position = p; transform.position = p;
        var visual = GetComponent<MarbleVisual>();
        if (visual != null) visual.SetSkin(SelectedPower == MarblePower.Iron ? 5 : MahalleProfile.EffectiveSkin);
        return true;
    }
    public void CancelAim() { isAiming = false; Power = 0; if (aimIndicator != null) aimIndicator.Hide(); }
    private void OnApplicationFocus(bool focus) { if (!focus) CancelAim(); }
    private void Update()
    {
        var pointer = Pointer.current;
        if (pointer == null || gameCamera == null || !ShootingEnabled) { CancelAim(); return; }
        aimPlane = new Plane(Vector3.up, transform.position);
        if (pointer.press.wasPressedThisFrame) BeginAim(pointer.position.ReadValue());
        else if (isAiming && pointer.press.wasReleasedThisFrame) { UpdateAim(pointer.position.ReadValue()); ReleaseAim(); }
        else if (isAiming && pointer.press.isPressed) UpdateAim(pointer.position.ReadValue());
    }
    private void FixedUpdate()
    {
        if (!hasPendingImpulse) return;
        hasPendingImpulse = false;
        MahalleProfile.RecordMarbleShot(pendingWearSkin,pendingWearPower);
        body.AddForce(pendingImpulse, ForceMode.Impulse);
    }
    private bool OverUI(Vector2 point)
    {
        if (EventSystem.current == null) return false;
        uiHits.Clear(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, uiHits);
        return uiHits.Count > 0;
    }
    private void BeginAim(Vector2 screen)
    {
        if (!AtRest || OverUI(screen) || !TryGetPlanePoint(screen, out var world)) return;
        Vector3 flat = transform.position; flat.y = world.y;
        if (Vector3.Distance(world, flat) > grabRadius)
        {
            if (!positionLocked && shooterLine != null && shooterLine.IsNear(world))
            {
                var position = shooterLine.ClampToLine(world, transform.position.y);
                body.position = position; transform.position = position;
            }
            return;
        }
        isAiming = true; pullPoint = world;
    }
    private void UpdateAim(Vector2 screen)
    {
        if (!TryGetPlanePoint(screen, out var world)) return;
        pullPoint = world; GetShot(out var direction, out var power); Power = power;
        if (aimIndicator != null) aimIndicator.Show(transform.position, direction, power, SelectedPower == MarblePower.Guide, GetComponent<Collider>());
    }
    private void ReleaseAim()
    {
        GetShot(out var direction, out var power);
        CancelAim();
        bool usedStock;
        MarblePower fired = SelectedPower;
        if (power <= 0 || !MahalleProfile.Consume(fired, out usedStock)) return;
        keepsPosition = fired == MarblePower.Anchor;
        anchorUsedStock = usedStock;
        positionLocked = false;
        float multiplier = SelectedPower == MarblePower.Big ? 2.3f : SelectedPower == MarblePower.Iron ? 2.5f : 1f;
        pendingWearSkin=special.ActiveSkin;pendingWearPower=fired;
        pendingImpulse = direction * (maxShotImpulse * power * multiplier * special.ImpulseMultiplier); hasPendingImpulse = true;
        if (SfxPlayer.Instance != null) SfxPlayer.Instance.PlayShot(power);
        MahalleFeedback.Tap();
        shootingEnabled = false;
        MahalleProfile.Data.tutorialDone = true; MahalleProfile.Save();
        ShotFired?.Invoke();
    }
    private void GetShot(out Vector3 direction, out float power)
    {
        Vector3 pull = transform.position - pullPoint; pull.y = 0;
        float distance = pull.magnitude;
        direction = distance > .0001f ? pull / distance : Vector3.forward;
        power = Mathf.Clamp01((distance - minPullDistance) / (maxPullDistance - minPullDistance));
    }
    private bool TryGetPlanePoint(Vector2 screen, out Vector3 world)
    {
        Ray ray = gameCamera.ScreenPointToRay(screen);
        if (aimPlane.Raycast(ray, out float enter)) { world = ray.GetPoint(enter); return true; }
        world = Vector3.zero; return false;
    }
}
