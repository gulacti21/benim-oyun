using UnityEngine;

// Kırılan buzun uçuşan parçası. Kendi kendini yok eder.
public class IceShard : MonoBehaviour
{
    public Vector3 velocity;
    private float age;
    private void Update()
    {
        age += Time.deltaTime;
        velocity += Vector3.down * 9f * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        transform.Rotate(360f * Time.deltaTime, 200f * Time.deltaTime, 0f);
        transform.localScale *= Mathf.Max(0f, 1f - Time.deltaTime * 1.6f);
        if (age > .6f) Destroy(gameObject);
    }
}
