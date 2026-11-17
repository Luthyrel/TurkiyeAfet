using UnityEngine;

public sealed class PrototypeAudio : MonoBehaviour
{
    public static PrototypeAudio Instance { get; private set; }

    private AudioSource effectsSource;
    private AudioSource movementSource;
    private AudioSource stormRainSource;
    private AudioSource tornadoWindSource;
    private AudioClip stormRainLoop;
    private AudioClip tornadoWindLoop;
    private float stormRainTargetVolume;
    private float tornadoWindTargetVolume;

    private AudioClip footsteps;
    private AudioClip softSteps;
    private AudioClip doorOpen;
    private AudioClip bagOpen;
    private AudioClip bagClose;
    private AudioClip itemPickup;
    private AudioClip woodImpact;
    private AudioClip metalImpact;
    private AudioClip gasValve;
    private AudioClip windowSlide;
    private AudioClip collapse;
    private AudioClip debrisFall;
    private AudioClip uiSelect;
    private AudioClip answerCorrect;
    private AudioClip answerWrong;
    private AudioClip taskComplete;
    private AudioClip floodCallRing;
    private AudioClip floodCallConnect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        effectsSource = gameObject.AddComponent<AudioSource>();
        effectsSource.playOnAwake = false;
        effectsSource.spatialBlend = 0f;
        effectsSource.volume = 0.85f;

        movementSource = gameObject.AddComponent<AudioSource>();
        movementSource.playOnAwake = false;
        movementSource.spatialBlend = 0f;
        movementSource.loop = true;
        movementSource.volume = 0.36f;

        stormRainSource = gameObject.AddComponent<AudioSource>();
        stormRainSource.playOnAwake = false;
        stormRainSource.spatialBlend = 0f;
        stormRainSource.loop = true;
        stormRainSource.volume = 0f;
        stormRainSource.dopplerLevel = 0f;

        tornadoWindSource = gameObject.AddComponent<AudioSource>();
        tornadoWindSource.playOnAwake = false;
        tornadoWindSource.spatialBlend = 0f;
        tornadoWindSource.loop = true;
        tornadoWindSource.volume = 0f;
        tornadoWindSource.dopplerLevel = 0f;
        tornadoWindSource.priority = 80;

        footsteps = Load("Footsteps");
        // Crouching reuses the same indoor recording at a slower, quieter pace.
        softSteps = Load("Footsteps");
        doorOpen = Load("DoorOpen");
        bagOpen = Load("BagOpen");
        bagClose = Load("BagClose");
        itemPickup = Load("ItemPickup");
        woodImpact = Load("WoodImpact");
        metalImpact = Load("MetalImpact");
        gasValve = Load("GasValve");
        windowSlide = Load("WindowSlide");
        collapse = Load("Collapse");
        debrisFall = Load("DebrisFall");
        uiSelect = Load("UISelect");
        answerCorrect = Load("AnswerCorrect");
        answerWrong = Load("AnswerWrong");
        taskComplete = Load("TaskComplete");
        floodCallRing = CreatePhoneRingClip();
        floodCallConnect = CreatePhoneConnectClip();
        stormRainLoop = Resources.Load<AudioClip>("Audio/StormRain");
        stormRainSource.clip = stormRainLoop;
        tornadoWindLoop = CreateTornadoWindLoop();
        tornadoWindSource.clip = tornadoWindLoop;

    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (stormRainSource != null)
        {
            float transitionSpeed = stormRainTargetVolume < stormRainSource.volume ? 1.8f : 0.8f;
            stormRainSource.volume = Mathf.MoveTowards(stormRainSource.volume, stormRainTargetVolume,
                Time.unscaledDeltaTime * transitionSpeed);
            if (stormRainTargetVolume <= 0f && stormRainSource.volume <= 0.001f && stormRainSource.isPlaying)
                stormRainSource.Stop();
        }

        if (tornadoWindSource != null)
        {
            float transitionSpeed = tornadoWindTargetVolume < tornadoWindSource.volume
                ? Mathf.Max(0.025f, tornadoWindTargetVolume * 0.25f)
                : 0.13f;
            tornadoWindSource.volume = Mathf.MoveTowards(tornadoWindSource.volume, tornadoWindTargetVolume,
                Time.unscaledDeltaTime * transitionSpeed);
            if (tornadoWindTargetVolume <= 0f && tornadoWindSource.volume <= 0.001f && tornadoWindSource.isPlaying)
                tornadoWindSource.Stop();
        }
    }

    public static void SetStormRain(bool playing, bool doorOpen)
    {
        if (Instance == null || Instance.stormRainSource == null)
            return;

        if (playing)
        {
            if (!Instance.stormRainSource.isPlaying)
            {
                Instance.stormRainSource.volume = 0f;
                Instance.stormRainSource.Play();
            }
            Instance.stormRainTargetVolume = doorOpen ? 0.48f : 0.055f;
        }
        else
        {
            Instance.stormRainTargetVolume = 0f;
        }
    }

    public static void SetTornadoWind(float intensity)
    {
        if (Instance == null || Instance.tornadoWindSource == null)
            return;

        if (intensity > 0.001f)
        {
            if (!Instance.tornadoWindSource.isPlaying)
            {
                Instance.tornadoWindSource.volume = 0f;
                Instance.tornadoWindSource.Play();
            }
            Instance.tornadoWindTargetVolume = Mathf.Clamp01(intensity) * 0.42f;
        }
        else
        {
            Instance.tornadoWindTargetVolume = 0f;
        }
    }

    private AudioClip Load(string clipName)
    {
        return Resources.Load<AudioClip>("Audio/Mixkit/" + clipName);
    }

    private AudioClip CreatePhoneRingClip()
    {
        const int sampleRate = 44100;
        const float duration = 1.25f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float pulse = time % 0.58f;
            if (pulse < 0.22f)
            {
                float edge = Mathf.Min(pulse / 0.018f, Mathf.Min((0.22f - pulse) / 0.025f, 1f));
                float tone = Mathf.Sin(2f * Mathf.PI * 440f * time) * 0.55f
                    + Mathf.Sin(2f * Mathf.PI * 480f * time) * 0.45f;
                samples[i] = tone * Mathf.Clamp01(edge) * 0.24f;
            }
        }
        AudioClip clip = AudioClip.Create("Simüle Telefon Zili", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreatePhoneConnectClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.24f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float edge = Mathf.Min(time / 0.018f, Mathf.Min((duration - time) / 0.04f, 1f));
            samples[i] = Mathf.Sin(2f * Mathf.PI * 425f * time) * Mathf.Clamp01(edge) * 0.18f;
        }
        AudioClip clip = AudioClip.Create("Simüle Telefon Bağlantı Sesi", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateTornadoWindLoop()
    {
        const int sampleRate = 22050;
        const int durationSeconds = 4;
        const int noiseNodeCount = 176;
        int sampleCount = sampleRate * durationSeconds;
        float[] noiseNodes = new float[noiseNodeCount + 1];
        System.Random random = new System.Random(20261117);
        for (int i = 0; i < noiseNodeCount; i++)
            noiseNodes[i] = (float)(random.NextDouble() * 2.0 - 1.0);
        noiseNodes[noiseNodeCount] = noiseNodes[0];

        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float nodePosition = t / durationSeconds * noiseNodeCount;
            int node = Mathf.FloorToInt(nodePosition);
            float blend = nodePosition - node;
            float noise = Mathf.Lerp(noiseNodes[node], noiseNodes[node + 1], blend);
            float gust = 0.72f + 0.20f * Mathf.Sin(2f * Mathf.PI * 0.5f * t)
                + 0.08f * Mathf.Sin(2f * Mathf.PI * 0.75f * t + 0.8f);
            float lowRumble = Mathf.Sin(2f * Mathf.PI * 48f * t) * 0.31f
                + Mathf.Sin(2f * Mathf.PI * 73f * t) * 0.16f
                + Mathf.Sin(2f * Mathf.PI * 111f * t) * 0.08f;
            samples[i] = Mathf.Clamp((noise * 0.42f + lowRumble) * gust, -0.9f, 0.9f);
        }

        AudioClip clip = AudioClip.Create("Hortumun Alçak Frekanslı Rüzgâr Döngüsü", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void PlayOneShot(AudioClip clip, float volume = 1f, float pitchMin = 0.96f, float pitchMax = 1.04f)
    {
        if (clip == null || effectsSource == null)
            return;

        effectsSource.pitch = Random.Range(pitchMin, pitchMax);
        effectsSource.PlayOneShot(clip, volume);
        effectsSource.pitch = 1f;
    }

    public static void PlayUiSelect() => Instance?.PlayOneShot(Instance.uiSelect, 0.42f);
    public static void PlayItemPickup() => Instance?.PlayOneShot(Instance.itemPickup, 0.22f, 0.94f, 1.02f);
    public static void PlayBagOpen() => Instance?.PlayOneShot(Instance.bagOpen, 0.38f, 0.96f, 1.02f);
    public static void PlayBagStore() => Instance?.PlayOneShot(Instance.itemPickup, 0.22f, 0.98f, 1.04f);
    public static void PlayBagClose() => Instance?.PlayOneShot(Instance.bagClose, 0.30f, 0.96f, 1.02f);
    public static void PlayDoorOpen() => Instance?.PlayOneShot(Instance.doorOpen, 0.65f);
    public static void PlayGasValve() => Instance?.PlayOneShot(Instance.gasValve, 0.62f);
    public static void PlayWindowSlide() => Instance?.PlayOneShot(Instance.windowSlide, 0.5f);
    public static void PlayTaskComplete() => Instance?.PlayOneShot(Instance.taskComplete, 0.30f);
    public static void PlayAnswerCorrect() => Instance?.PlayOneShot(Instance.answerCorrect, 0.24f);
    public static void PlayAnswerWrong() => Instance?.PlayOneShot(Instance.answerWrong, 0.55f);
    public static void PlayPhoneRing() => Instance?.PlayOneShot(Instance.floodCallRing, 0.65f, 1f, 1f);
    public static void PlayPhoneConnect() => Instance?.PlayOneShot(Instance.floodCallConnect, 0.45f, 1f, 1f);

    public static void PlayFurnitureSound(PrototypeAction action)
    {
        if (Instance == null)
            return;

        AudioClip clip = action == PrototypeAction.SecureFurniture ? Instance.metalImpact : Instance.woodImpact;
        Instance.PlayOneShot(clip, 0.58f);
    }

    public static void PlayHoldStart()
    {
        if (Instance != null)
            Instance.PlayOneShot(Instance.itemPickup, 0.10f, 0.88f, 0.94f);
    }

    public static void PlayImpact(float volume)
    {
        if (Instance != null)
            Instance.PlayOneShot(Instance.woodImpact, volume, 0.88f, 1.08f);
    }

    public static void PlayCollapse()
    {
        if (Instance == null)
            return;

        Instance.PlayOneShot(Instance.collapse, 0.72f, 0.92f, 1.03f);
        Instance.PlayOneShot(Instance.debrisFall, 0.5f, 0.94f, 1.06f);
    }

    public static void PlayLandslide()
    {
        if (Instance != null)
            Instance.PlayOneShot(Instance.debrisFall, 0.30f, 0.86f, 0.96f);
    }

    public static void SetMovement(bool moving, bool running, bool crouched)
    {
        if (Instance == null || Instance.movementSource == null)
            return;

        if (!moving || Instance.footsteps == null)
        {
            if (Instance.movementSource.isPlaying)
                Instance.movementSource.Stop();
            return;
        }

        AudioClip movementClip = crouched && Instance.softSteps != null
            ? Instance.softSteps : Instance.footsteps;
        if (Instance.movementSource.clip != movementClip)
        {
            Instance.movementSource.Stop();
            Instance.movementSource.clip = movementClip;
        }
        Instance.movementSource.pitch = crouched ? 0.82f : running ? 1.10f : 0.96f;
        Instance.movementSource.volume = crouched ? 0.14f : running ? 0.34f : 0.24f;
        if (!Instance.movementSource.isPlaying)
            Instance.movementSource.Play();
    }

    public static void PlayCrouch()
    {
        if (Instance != null)
            Instance.PlayOneShot(Instance.itemPickup, 0.10f, 0.88f, 0.94f);
    }
}
