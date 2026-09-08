using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MarbleSfx : MonoBehaviour
{
    [SerializeField] private float minImpactSpeed = 0.5f;
    [SerializeField] private float maxImpactSpeed = 5f;
    [SerializeField] private float cooldown = 0.04f;

    private float nextPlayTime;

    private void OnCollisionEnter(Collision collision)
    {
        if (SfxPlayer.Instance == null || Time.time < nextPlayTime)
        {
            return;
        }

        float speed = collision.relativeVelocity.magnitude;

        if (speed < minImpactSpeed)
        {
            return;
        }

        nextPlayTime = Time.time + cooldown;
        SfxPlayer.Instance.PlayMarbleHit(Mathf.InverseLerp(minImpactSpeed, maxImpactSpeed, speed));
    }
}
