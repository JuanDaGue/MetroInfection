using UnityEngine;
using System.Linq;

[DisallowMultipleComponent]
public class FirstPerstonStepsAudios : MonoBehaviour
{
    [Header("References (assign)")]
    public MobileFPSController playerController;    // your existing controller
    public CharacterController characterController; // the CharacterController used by playerController

    [Header("Footsteps")]
    public AudioSource footstepSource;   // one-shot source for footsteps
    public AudioClip[] walkSteps;
    public AudioClip[] runSteps;
    public AudioClip[] crouchSteps;
    [Tooltip("Meters between footsteps when walking.")]
    public float stepDistance = 0.8f;
    [Tooltip("Multiplier on stepDistance when running (smaller -> faster steps).")]
    public float runningStepMultiplier = 0.75f;

    [Header("Landing / Jump")]
    public AudioSource landingSource;
    public AudioClip[] landingSFX;
    public AudioSource jumpSource;
    public AudioClip[] jumpSFX;

    [Header("Crouch")]
    public AudioSource crouchStartSource;
    public AudioClip[] crouchStartSFX;
    public AudioSource crouchEndSource;
    public AudioClip[] crouchEndSFX;

    [Header("Tuning")]
    [Range(0f, 0.2f)] public float pitchVariance = 0.05f;
    [Range(0f, 0.2f)] public float volumeVariance = 0.08f;
    [Tooltip("Min movement delta (meters/frame) to be considered moving")]
    public float velocityThreshold = 0.001f;

    // internals
    Vector2 lastPlayerPosXZ;
    float distanceAccumulator = 0f;
    bool prevGrounded = true;
    bool prevCrouched = false;

    void Reset()
    {
        // try to auto-assign sensible defaults for quick setup
        playerController = GetComponentInParent<MobileFPSController>();
        characterController = GetComponentInParent<CharacterController>();

        footstepSource = GetOrCreateAudioSource("Footstep Source");
        landingSource = GetOrCreateAudioSource("Landing Source");
        jumpSource = GetOrCreateAudioSource("Jump Source");
        crouchStartSource = GetOrCreateAudioSource("Crouch Start Source");
        crouchEndSource = GetOrCreateAudioSource("Crouch End Source");
    }

    void Start()
    {
        if (playerController == null)
            Debug.LogWarning("FirstPersonAudio: playerController not assigned.", this);

        if (characterController == null && playerController != null)
            characterController = playerController.controller;

        lastPlayerPosXZ = GetPlayerXZ();
        prevGrounded = characterController ? characterController.isGrounded : true;
        prevCrouched = playerController ? playerController.IsCrouched() : false;
    }

    void FixedUpdate()
    {
        if (playerController == null || characterController == null) return;
        //Debug.Log("Is Grounded: " + characterController.isGrounded+ "Prev Grounded: " + prevGrounded);
        // ---------- Grounded / Jump / Landing detection ----------
        bool isGrounded = characterController.isGrounded;

        // Jump start: previously grounded and now not grounded -> player left ground (jump or fall)
        if (prevGrounded && !isGrounded)
        {
            // treat as jump start (covers player-initiated jump)
            PlayRandomClip(jumpSource, jumpSFX);
            Debug.Log("Jump SFX");
            // optional mobile haptic
#if UNITY_ANDROID || UNITY_IOS
            try { Handheld.Vibrate(); } catch { }
#endif
        }

        // Landing: previously not grounded and now grounded -> landed
        if (!prevGrounded && isGrounded)
        {
            PlayRandomClip(landingSource, landingSFX);
#if UNITY_ANDROID || UNITY_IOS
            try { Handheld.Vibrate(); } catch { }
#endif
        }

        prevGrounded = isGrounded;

        // ---------- Crouch start/end detection ----------
        bool isCrouched = playerController.IsCrouched();
        if (!prevCrouched && isCrouched) // crouch started
        {
            PlayRandomClip(crouchStartSource, crouchStartSFX);
        }
        else if (prevCrouched && !isCrouched) // crouch ended
        {
            PlayRandomClip(crouchEndSource, crouchEndSFX);
        }
        prevCrouched = isCrouched;

        // ---------- Footsteps (distance-based) ----------
        // accumulate horizontal distance travelled (XZ plane)
        Vector2 curXZ = GetPlayerXZ();
        float delta = Vector2.Distance(curXZ, lastPlayerPosXZ);
        lastPlayerPosXZ = curXZ;
        //Debug.Log("Delta: " + delta + " Velocity Threshold: " + velocityThreshold + " Grounded: " + isGrounded);
        // ignore extremely small jitter
        if (delta < velocityThreshold || !isGrounded)
        {
            // not moving or in air: don't accumulate
            distanceAccumulator = 0f;
            return;
        }
       //Debug.Log("Distance Accumulator: " + distanceAccumulator);
        float effectiveStepDistance = stepDistance;
        if (playerController.IsCrouched()) effectiveStepDistance *= 1.15f; // tweak for crouch
        if (playerController.GetCurrentSpeed() > playerController.walkSpeed + 0.01f) // running
            effectiveStepDistance *= runningStepMultiplier;

        distanceAccumulator += delta;
        //Debug.Log("Distance Accumulator: " + distanceAccumulator+ " Effective Step Distance: " + effectiveStepDistance);
        if (distanceAccumulator >= effectiveStepDistance)
        {
            //Debug.Log("Play Footstep");
            PlayFootstep();
            distanceAccumulator = 0f;
        }
    }

    Vector2 GetPlayerXZ()
    {
        var t = playerController.transform;
        return new Vector2(t.position.x, t.position.z);
    }

    void PlayFootstep()
    {
        //Debug.Log("Play Footstep: " + footstepSource + " : " + walkSteps + " : " + runSteps + " : " + crouchSteps);
        if (footstepSource == null) return;

        AudioClip[] pool = walkSteps;

        if (playerController.IsCrouched() && crouchSteps != null && crouchSteps.Length > 0)
            pool = crouchSteps;
        else if (playerController.GetCurrentSpeed() > playerController.walkSpeed + 0.01f && runSteps != null && runSteps.Length > 0)
            pool = runSteps;

        if (pool == null || pool.Length == 0)
        {
            if (footstepSource.clip != null) PlayWithVariation(footstepSource, footstepSource.clip);
            return;
        }

        AudioClip clip = pool[Random.Range(0, pool.Length)];
        // avoid immediate repeat
        if (pool.Length > 1)
        {
            int attempts = 0;
            while (clip == footstepSource.clip && attempts++ < 6)
                clip = pool[Random.Range(0, pool.Length)];
        }

        PlayWithVariation(footstepSource, clip);
    }

    void PlayWithVariation(AudioSource src, AudioClip clip)
    {
        if (src == null || clip == null) return;
        float pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        float vol = 1f + Random.Range(-volumeVariance, volumeVariance);
        src.pitch = pitch;
        src.PlayOneShot(clip, vol);
    }

    // Utility: play a random clip from an array on the given source (non-one-shot playback)
    void PlayRandomClip(AudioSource source, AudioClip[] clips)
    {
       // Debug.Log("Play Random Clip:"+ source+" : "+ clips);
        if (source == null || clips == null || clips.Length == 0) return;
       // Debug.Log("Play Random Clip");
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clips.Length > 1)
        {
            int attempts = 0;
            while (clip == source.clip && attempts++ < 6)
                clip = clips[Random.Range(0, clips.Length)];
        }
        source.clip = clip;
        source.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        source.Play();
    }

    AudioSource GetOrCreateAudioSource(string name)
    {
        AudioSource found = System.Array.Find(GetComponentsInChildren<AudioSource>(), a => a.name == name);
        if (found != null) return found;

        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.spatialBlend = 1f; // 3D
        src.playOnAwake = false;
        return src;
    }
}
