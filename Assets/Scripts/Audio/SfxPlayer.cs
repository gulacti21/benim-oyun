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

    // AÇILIŞ: tebeşir çizme sesleri (Resources/Mahalle/Sfx) ve misket tıkları.
    private AudioClip chalkRing, chalkTriangle;
    public void PlayChalk(bool triangle)
    {
        if (triangle) { if (chalkTriangle == null) chalkTriangle = Resources.Load<AudioClip>("Mahalle/Sfx/ChalkTriangle"); Play(chalkTriangle, .75f, 1f); }
        else { if (chalkRing == null) chalkRing = Resources.Load<AudioClip>("Mahalle/Sfx/ChalkRing"); Play(chalkRing, .8f, 1f); }
    }
    public void PlayMarbleTick(int index)
    {
        Play(marbleHitClip, .35f, 1.05f + index * .06f);
    }

    public void PlayShot(float power)
    {
        Play(shotClip, 0.45f + (0.5f * Mathf.Clamp01(power)), 0.92f + (0.2f * Mathf.Clamp01(power)));
    }

    public void PlayMarbleHit(float strength)
    {
        Play(marbleHitClip, 0.25f + (0.7f * Mathf.Clamp01(strength)), Random.Range(0.9f, 1.15f));
    }

    // BUZLU MİSKET: kırılma = yüksek perdeli sert tık üst üste, zayıf darbe = ince kısa tık.
    // Ayrı ses dosyası yok; misket çarpma sesinin perdesi yükseltilerek üretilir.
    public void PlayIceBreak(float strength)
    {
        Play(marbleHitClip, .7f + .3f * Mathf.Clamp01(strength), 1.75f);
        Play(marbleHitClip, .45f, 2.3f);
    }

    public void PlayIceTick(float strength)
    {
        Play(marbleHitClip, .18f + .25f * Mathf.Clamp01(strength), 2.6f);
    }

    // BÖLÜNEN MİSKET: kısa "çıt" — alçak ve kuru.
    public void PlaySplit(float strength)
    {
        Play(marbleHitClip, .6f + .35f * Mathf.Clamp01(strength), .72f);
    }

    // ÇUKUR: misket düştü — boğuk, alçak tık.
    public void PlayPit()
    {
        Play(marbleHitClip, .55f, .55f);
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
