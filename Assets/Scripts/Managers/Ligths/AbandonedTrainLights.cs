using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


[Serializable]
public class LightNode
{
    public Light light;
    public Transform tether;
    [Range(0f,1f)] public float initialHealth = 1f;
    [Range(0f,1f)] public float failureThreshold = 0.1f; // health value at which the light fails
    [HideInInspector] public float health;
    [HideInInspector] public bool isDead;
    [HideInInspector] public float flickerTimer;
    [HideInInspector] public float blackoutTimer;
}

public class AbandonedTrainLights : MonoBehaviour
{
    [Header("Nodes")]
    public List<LightNode> nodes = new List<LightNode>();

    [Header("Global timing")]
    public float globalDecayRate = 0.005f;        // base health drain per second
    public Vector2 randomDecayRange = new Vector2(0f, 0.01f); // added per-node randomness
    public float propagationChance = 0.25f;      // chance that a dead node will cause neighbor decay
    public float propagationDistance = 3.5f;     // max distance to consider neighbor

    [Header("Flicker")]
    public Vector2 flickerInterval = new Vector2(0.05f, 0.25f); // how fast flicker pulses
    public Vector2 flickerIntensityRange = new Vector2(0.2f, 1.0f); // relative intensity during flicker
    [Range(0f, 1f)] public float flickerProbability = 0.6f; // per-event chance to flicker
    public float burstChance = 0.02f; // chance per second to start a longer burst flicker
    public float burstDuration = 1.2f;

    [Header("Blackouts")]
    public Vector2 blackoutInterval = new Vector2(6f, 20f); // average time between short blackouts per node
    public Vector2 blackoutDuration = new Vector2(0.15f, 0.8f);

    [Header("Visuals & Audio")]
    [Tooltip("If emmisiveMaterial not null, script will set float property to simulate glow (property name must exist).")]
    public Material emissiveMaterial = null;
    public string emissiveProperty = "_Emission"; // example property name (shader-dependent)
    public AudioClip sparkSfx = null;
    public float sparkSfxVolume = 0.6f;

    [Header("Debug")]
    public bool runOnStart = true;
    public bool debugLog = false;

    // internal
    System.Random rng = new System.Random();
    AudioSource audioSource;

    void Awake()
    {
        // create audio source if needed
        if (sparkSfx != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.volume = sparkSfxVolume;
        }

        // initialize nodes
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            n.health = n.initialHealth;
            n.isDead = n.health <= n.failureThreshold;
            n.flickerTimer = Random.Range(0f, flickerInterval.y);
            n.blackoutTimer = Random.Range(blackoutInterval.x, blackoutInterval.y);
            nodes[i] = n;
        }
    }

    void Start()
    {
        if (runOnStart)
            StartSimulation();
    }

    public void StartSimulation()
    {
        StopAllCoroutines();
        StartCoroutine(SimulationLoop());
    }

    public void StopSimulation()
    {
        StopAllCoroutines();
        // restore full brightness if desired
        foreach (var n in nodes) if (n.light != null)
        {
            n.light.enabled = true;
            n.light.intensity = n.light.intensity; // no-op, but you can set default if you saved it
        }
    }

    IEnumerator SimulationLoop()
    {
        float lastTime = Time.time;
        while (true)
        {
            float dt = Time.time - lastTime;
            lastTime = Time.time;
            UpdateNodes(dt);
            yield return null;
        }
    }

    void UpdateNodes(float dt)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            if (n.light == null) continue;

            // passive health decay
            float decay = globalDecayRate * dt + Random.Range(randomDecayRange.x, randomDecayRange.y) * dt;
            n.health = Mathf.Max(0f, n.health - decay);

            // propagation: if any other node nearby is dead, increase local decay
            if (!n.isDead)
            {
                foreach (var other in nodes)
                {
                    if (other == n || other.isDead == false || other.tether == null || n.tether == null) continue;
                    float dist = Vector3.Distance(n.tether.position, other.tether.position);
                    if (dist <= propagationDistance && UnityEngine.Random.value < propagationChance * dt)
                    {
                        float extra = UnityEngine.Random.Range(globalDecayRate * 0.5f, globalDecayRate * 2f) * dt;
                        n.health = Mathf.Max(0f, n.health - extra);
                        if (debugLog) Debug.Log($"Propagation: {i} got extra decay {extra:F4} from dead neighbor.");
                    }
                }
            }

            // check death
            if (!n.isDead && n.health <= n.failureThreshold)
            {
                n.isDead = true;
                HandleDeath(n);
            }

            // flicker logic (only if not dead)
            if (!n.isDead)
            {
                // occasional burst event
                if (UnityEngine.Random.value < burstChance * dt)
                {
                    StartCoroutine(BurstFlicker(n, burstDuration));
                }

                // short blackout timer
                n.blackoutTimer -= dt;
                if (n.blackoutTimer <= 0f)
                {
                    StartCoroutine(ShortBlackout(n, Random.Range(blackoutDuration.x, blackoutDuration.y)));
                    n.blackoutTimer = Random.Range(blackoutInterval.x, blackoutInterval.y);
                }

                // regular flicker pulses
                n.flickerTimer -= dt;
                if (n.flickerTimer <= 0f)
                {
                    n.flickerTimer = Random.Range(flickerInterval.x, flickerInterval.y);
                    if (UnityEngine.Random.value <= flickerProbability)
                    {
                        float t = UnityEngine.Random.Range(0f, 1f);
                        float rel = Mathf.Lerp(flickerIntensityRange.x, flickerIntensityRange.y, t);
                        float baseIntensity = GetBaseIntensity(n.light);
                        n.light.intensity = baseIntensity * rel;
                        SetEmissionIntensity(baseIntensity * rel);
                        // small restore over short time
                        StartCoroutine(RestoreIntensity(n, baseIntensity, UnityEngine.Random.Range(0.04f, 0.25f)));
                        if (sparkSfx != null && UnityEngine.Random.value < 0.12f)
                        {
                            PlaySparks(n);
                        }
                    }
                }
            }
            else
            {
                // dead: ensure off or minimal flicker ghost
                float ghost = Mathf.PingPong(Time.time * 0.5f + i, 0.02f);
                n.light.intensity = Mathf.Lerp(0f, 0.03f, ghost);
                SetEmissionIntensity(n.light.intensity);
            }

            nodes[i] = n;
        }
    }

    IEnumerator RestoreIntensity(LightNode n, float target, float duration)
    {
        if (n.light == null) yield break;
        float start = n.light.intensity;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float v = Mathf.Lerp(start, target, t / duration);
            n.light.intensity = v;
            SetEmissionIntensity(v);
            yield return null;
        }
        if (n.light != null) n.light.intensity = target;
    }

    IEnumerator ShortBlackout(LightNode n, float dur)
    {
        if (n.light == null || n.isDead) yield break;
        float backup = n.light.intensity;
        n.light.intensity = 0f;
        SetEmissionIntensity(0f);
        if (sparkSfx != null && UnityEngine.Random.value < 0.15f) PlaySparks(n);
        yield return new WaitForSeconds(dur);
        if (!n.isDead)
        {
            n.light.intensity = backup;
            SetEmissionIntensity(backup);
        }
    }

    IEnumerator BurstFlicker(LightNode n, float totalDuration)
    {
        if (n.light == null || n.isDead) yield break;
        float elapsed = 0f;
        float baseIntensity = GetBaseIntensity(n.light);
        while (elapsed < totalDuration)
        {
            float rel = UnityEngine.Random.Range(flickerIntensityRange.x, flickerIntensityRange.y);
            n.light.intensity = baseIntensity * rel;
            SetEmissionIntensity(baseIntensity * rel);
            if (UnityEngine.Random.value < 0.1f) PlaySparks(n);
            elapsed += UnityEngine.Random.Range(0.03f, 0.25f);
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.02f, 0.12f));
        }
        if (!n.isDead) n.light.intensity = baseIntensity;
    }

    void HandleDeath(LightNode n)
    {
        if (n.light == null) return;
        n.light.intensity = 0f;
        n.light.enabled = false;
        SetEmissionIntensity(0f);
        if (debugLog) Debug.Log($"Light died: {n.light.name}");
        // chance to create a spark or particle effect hook
        if (sparkSfx != null && UnityEngine.Random.value < 0.35f)
            PlaySparks(n);

        // optional: spawn a particle or call event here
    }

    void PlaySparks(LightNode n)
    {
        if (audioSource == null || sparkSfx == null) return;
        audioSource.transform.position = (n.tether != null) ? n.tether.position : n.light.transform.position;
        audioSource.PlayOneShot(sparkSfx, sparkSfxVolume * (0.7f + (float)rng.NextDouble() * 0.6f));
    }

    float GetBaseIntensity(Light l)
    {
        // store default intensity in light's range or use current if not stored
        return Mathf.Max(0.01f, l.intensity);
    }

    void SetEmissionIntensity(float intensity)
    {
        if (emissiveMaterial == null) return;
        // Some shaders expect a color or float; adapt as needed. Here we set a float property name.
        if (emissiveMaterial.HasProperty(emissiveProperty))
            emissiveMaterial.SetFloat(emissiveProperty, intensity);
    }

    // Utility: allow external systems to force-fail a node by index or reference
    public void ForceFailNode(int index)
    {
        if (index < 0 || index >= nodes.Count) return;
        nodes[index].health = 0f;
        nodes[index].isDead = true;
        HandleDeath(nodes[index]);
    }

    public void ForceFailNode(Light lightRef)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].light == lightRef) { ForceFailNode(i); return; }
        }
    }
}
