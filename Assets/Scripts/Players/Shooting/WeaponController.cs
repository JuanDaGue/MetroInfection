using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class WeaponController : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;           // main camera (set in inspector or null -> Camera.main)
    public Transform muzzleTransform;     // muzzle transform on your arm prefab
    public Transform shellEjectPoint;     // optional
    public FirstPersonLook_m lookScript;    // for recoil application

    [Header("Weapon Stats")]
    public int magazineSize = 12;
    public float fireRate = 0.2f;         // seconds between shots
    public bool isAutomatic = false;
    public float damage = 40f;
    public float range = 120f;
    public float reloadTime = 1.2f;

    [Header("Accuracy & Spread")]
    public float baseSpread = 1.8f;       // degrees at hip
    public float movingSpreadMultiplier = 1.6f;
    public float adsSpreadMultiplier = 0.4f; // while aiming
    public float recoilPerShot = 4f;      // degrees applied upwards
    public Vector2 recoilSideRange = new Vector2(-0.4f, 0.4f); // randomized lateral recoil

    [Header("Effects & Pooling")]
    public GameObject muzzlePrefab;       // short-lived muzzle flash prefab
    public GameObject impactPrefab;       // impact decal/particles prefab
    public int poolSize = 12;

    [Header("Layers & Mask")]
    public LayerMask hitMask = ~0;        // default: everything
    public string damageableTag = "Damageable";

    [Header("Audio")]
    public AudioClip fireClip;
    public AudioClip dryClip;
    public AudioClip reloadClip;
    public float volume = 1f;

    // runtime
    int currentAmmo;
    float lastFireTime = -99f;
    bool isReloading = false;
    bool firing = false;
    bool isAiming = false;
    AudioSource audioSource;

    // Pools
    SimplePool muzzlePool;
    SimplePool impactPool;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (playerCamera == null) playerCamera = Camera.main;
        currentAmmo = magazineSize;

        // create runtime pools under this object
        var muzzlePoolGO = new GameObject("MuzzlePool");
        muzzlePoolGO.transform.SetParent(transform);
        muzzlePool = muzzlePoolGO.AddComponent<SimplePool>();
        if (muzzlePrefab) muzzlePool.Initialize(muzzlePrefab, poolSize);

        var impactPoolGO = new GameObject("ImpactPool");
        impactPoolGO.transform.SetParent(transform);
        impactPool = impactPoolGO.AddComponent<SimplePool>();
        if (impactPrefab) impactPool.Initialize(impactPrefab, poolSize);
    }

    void Update()
    {
        // For automatic weapons, maintain firing while hold
        if (isAutomatic && firing && !isReloading)
        {
            TryShoot();
        }
    }

    // ---------------------------
    // Input System callbacks (PlayerInput Send Messages)
    // ---------------------------
    public void OnFire(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            if (isAutomatic)
            {
                firing = true;
                TryShoot();
            }
            else
            {
                // semi-auto: shoot on started
                TryShoot();
            }
        }
        else if (ctx.canceled)
        {
            firing = false;
        }
    }

    public void OnReload(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) StartCoroutine(Reload());
    }

    public void OnAim(InputAction.CallbackContext ctx)
    {
        // for hold-to-aim control
        isAiming = ctx.ReadValueAsButton();
        // You can modify FoV or sensitivity here when aiming
    }

    // ---------------------------
    // Public mobile methods (hook UI buttons)
    // ---------------------------
    public void OnFireButtonDown()    // pointer down on fire button -> start fire/shot
    {
        if (isAutomatic)
        {
            firing = true;
            TryShoot();
        }
        else
        {
            TryShoot();
        }
    }

    public void OnFireButtonUp()      // stop automatic fire
    {
        firing = false;
    }

    public void OnReloadButton()    // button press
    {
        if (!isReloading) StartCoroutine(Reload());
    }

    public void SetAim(bool aim)
    {
        isAiming = aim;
    }

    // ---------------------------
    // Core shooting logic
    // ---------------------------
    void TryShoot()
    {
        if (isReloading) return;
        if (Time.time - lastFireTime < fireRate) return;

        if (currentAmmo <= 0)
        {
            PlayDry();
            lastFireTime = Time.time;
            return;
        }

        Shoot();
    }

    void Shoot()
    {
        lastFireTime = Time.time;
        currentAmmo--;

        // audio
        if (fireClip) audioSource.PlayOneShot(fireClip, volume);

        // muzzle flash (pooled)
        if (muzzlePrefab && muzzlePool != null)
        {
            var m = muzzlePool.Get(muzzleTransform ? muzzleTransform.position : transform.position, muzzleTransform ? muzzleTransform.rotation : transform.rotation);
            // If the prefab has a ParticleSystem / timed life, it should deactivate itself and call ReturnToPool
            StartCoroutine(ReturnAfterDelay(m, 0.6f, muzzlePool));
        }

        // small vibration for mobile (basic)
#if UNITY_ANDROID || UNITY_IOS
        try { Handheld.Vibrate(); } catch { }
#endif

        // recoil
        Vector2 recoil = new Vector2(Random.Range(recoilSideRange.x, recoilSideRange.y), recoilPerShot);
        if (lookScript != null) lookScript.AddRecoil(recoil);

        // calculate spread in degrees
        float spread = baseSpread;
        // increase spread if moving (check player controller speed if available)
        var movement = GetComponentInParent<MobileFPSController>();
        if (movement != null)
            spread *= movement.IsCrouched() ? 0.6f : (movement.GetCurrentSpeed() > 0.1f ? movingSpreadMultiplier : 1f);

        if (isAiming) spread *= adsSpreadMultiplier;

        // compute final direction with random cone
        Vector3 origin = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)).origin;
        Vector3 forward = playerCamera.transform.forward;

        // randomize by rotating forward by random angles within spread
        Vector3 dir = RandomConeDirection(forward, spread);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            // handle hit
            HandleHit(hit);
        }

        // UI update for ammo can be invoked through an event or direct reference
        UpdateAmmoUI();
    }

    // helper: returns direction perturbed by degrees angle
    Vector3 RandomConeDirection(Vector3 forward, float maxAngleDeg)
    {
        if (maxAngleDeg <= 0f) return forward;
        // pick random point on cone
        float angle = Random.Range(0f, maxAngleDeg);
        float az = Random.Range(0f, 360f);
        // small-angle approximation: convert to vector
        Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up) * Quaternion.AngleAxis(az, Vector3.forward);
        // Instead of complex spherical sampling, we use a simpler method:
        Vector3 random = Quaternion.Euler(Random.Range(-maxAngleDeg, maxAngleDeg), Random.Range(-maxAngleDeg, maxAngleDeg), 0) * forward;
        return random.normalized;
    }

    void HandleHit(RaycastHit hit)
    {
        // spawn impact effect
        if (impactPrefab && impactPool != null)
        {
            var go = impactPool.Get(hit.point + hit.normal * 0.01f, Quaternion.LookRotation(hit.normal));
            go.transform.SetParent(hit.collider.transform);
            StartCoroutine(ReturnAfterDelay(go, 4f, impactPool));
        }

        // apply damage if target has health script
        var health = hit.collider.GetComponent<LifeSystem>();
        if (health != null)
        {
            health.TakeDamage(damage);
            Debug.Log("Hit " + hit.collider.name + " for " + damage + " damage.");
            health.OnDeath += delegate { Debug.Log("Player died."); };
        }
        else
        {
            // attempt to find damageable by tag or parent
            if (hit.collider.CompareTag(damageableTag))
            {
                var h = hit.collider.GetComponentInParent<LifeSystem>();
                if (h != null) h.TakeDamage(damage);
                Debug.Log("Hit " + hit.collider.name + " for " + damage + " damage.");
                h.OnDeath += delegate { Debug.Log("Player died."); };

            }
        }

        // apply physics impulse
        if (hit.rigidbody)
        {
            hit.rigidbody.AddForceAtPosition(-hit.normal * 80f, hit.point, ForceMode.Impulse);
        }
    }

    // ---------------------------
    // Reload logic
    // ---------------------------
    IEnumerator Reload()
    {
        if (isReloading) yield break;
        if (currentAmmo == magazineSize) yield break;

        isReloading = true;
        if (reloadClip) audioSource.PlayOneShot(reloadClip, volume);

        // optionally trigger arm/animator reload here
        yield return new WaitForSeconds(reloadTime);

        currentAmmo = magazineSize;
        isReloading = false;

        UpdateAmmoUI();
    }

    // ---------------------------
    // Pool return helper
    // ---------------------------
    IEnumerator ReturnAfterDelay(GameObject go, float delay, SimplePool pool)
    {
        if (go == null || pool == null) yield break;
        yield return new WaitForSeconds(delay);
        pool.Return(go);
    }

    void PlayDry()
    {
        if (dryClip) audioSource.PlayOneShot(dryClip, volume);
    }

    // ---------------------------
    // UI / query helpers
    // ---------------------------
    void UpdateAmmoUI()
    {
        // // Example: try to find a UI script in parents/scene and call it.
        // var ui = FindObjectOfType<AmmoUI>();
        // if (ui != null) ui.SetAmmo(currentAmmo, magazineSize);
        Debug.Log("currentAmmo: " + currentAmmo);
    }

    public int GetCurrentAmmo() => currentAmmo;
    public bool IsReloading() => isReloading;

    // cleanup
    void OnDisable()
    {
        firing = false;
    }
}
