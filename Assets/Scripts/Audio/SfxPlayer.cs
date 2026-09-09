using UnityEngine;

public class SfxPlayer : MonoBehaviour
{
    public static SfxPlayer Instance { get; private set; }

    [Header("Clips")]
    [SerializeField] private AudioClip shotClip;
    [SerializeField] private AudioClip marbleHitClip;
    [SerializeField] private AudioClip marbleOutClip;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip loseClip;
    [SerializeField] private AudioClip uiTapClip;

    [Header("Settings")]
    [SerializeField] private int voiceCount = 8;
    [SerializeField] private float masterVolume = 0.75f;

    private AudioSource[] voices;
    private int nextVoice;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        voices = new AudioSource[Mathf.Max(1, voiceCount)];

        for (int i = 0; i < voices.Length; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            voices[i] = source;
        }
    }

    public void Play(AudioClip clip, float volume, float pitch)
    {
        if (!MahalleProfile.Data.sound || clip == null || voices == null)
        {
            return;
        }

        AudioSource source = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Length;

        source.clip = clip;
        source.volume = Mathf.Clamp01(volume) * masterVolume;
        source.pitch = pitch;
        source.Play();
    }

    public void PlayShot(float power)
    {
        Play(shotClip, 0.45f + (0.5f * Mathf.Clamp01(power)), 0.92f + (0.2f * Mathf.Clamp01(power)));
    }

    public void PlayMarbleHit(float strength)
    {
        Play(marbleHitClip, 0.25f + (0.7f * Mathf.Clamp01(strength)), Random.Range(0.9f, 1.15f));
    }

    public void PlayMarbleOut()
    {
        Play(marbleOutClip, 0.7f, Random.Range(0.98f, 1.05f));
    }

    public void PlayWin()
    {
        Play(winClip, 0.85f, 1f);
    }

    public void PlayLose()
    {
        Play(loseClip, 0.7f, 1f);
    }

    public void PlayUiTap()
    {
        Play(uiTapClip, 0.5f, 1f);
    }
}
