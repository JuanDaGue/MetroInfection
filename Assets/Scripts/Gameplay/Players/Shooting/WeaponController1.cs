using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstPersonMobileTools.DynamicFirstPerson
{
    [RequireComponent(typeof(AudioSource))]
    public class WeaponController1 : MonoBehaviour
    {
        [Header("References")]
        public Camera playerCamera;           // asigna en inspector o deja null para Camera.main
        public Transform muzzleTransform;     // punto de spawn del muzzle flash
        public Transform shellEjectPoint;     // punto de eyección de casquillos (opcional)

        private CameraLook lookScript;        // referencia al script de camera recoil
        private AudioSource audioSource;

        [Header("Weapon Stats")]
        public int magazineSize = 12;
        public float fireRate = 0.2f;
        public bool isAutomatic = false;
        public float damage = 40f;
        public float range = 120f;
        public float reloadTime = 1.2f;

        [Header("Accuracy & Spread")]
        public float baseSpread = 1.8f;
        public float movingSpreadMultiplier = 1.6f;
        public float adsSpreadMultiplier = 0.4f;
        public float recoilPerShot = 4f;                    // grados hacia arriba
        public Vector2 recoilSideRange = new Vector2(-0.4f, 0.4f);

        [Header("Effects & Pooling")]
        public GameObject muzzlePrefab;
        public GameObject impactPrefab;
        public int poolSize = 12;

        [Header("Layers & Mask")]
        public LayerMask hitMask = ~0;
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

        // Pools
        SimplePool muzzlePool;
        SimplePool impactPool;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();

            // 1) Cámara por defecto si no asignaste en Inspector
            if (playerCamera == null)
                playerCamera = Camera.main;

            // 2) Obtén el CameraLook desde la cámara
            if (playerCamera != null)
                lookScript = playerCamera.GetComponent<CameraLook>();

            // 3) Backup por si no lo encuentra
            if (lookScript == null)
                lookScript = GetComponent<CameraLook>();

            currentAmmo = magazineSize;

            // Inicializa pools
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
            if (isAutomatic && firing && !isReloading)
                TryShoot();
        }

        #region Input Callbacks
        public void OnFire(InputAction.CallbackContext ctx)
        {
            if (ctx.started)
            {
                firing = isAutomatic;
                TryShoot();
            }
            else if (ctx.canceled)
            {
                firing = false;
            }
        }

        public void OnReload(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
                StartCoroutine(Reload());
        }

        public void OnAim(InputAction.CallbackContext ctx)
        {
            isAiming = ctx.ReadValueAsButton();
        }

        public void OnFireButtonDown()
        {
            firing = isAutomatic;
            TryShoot();
        }

        public void OnFireButtonUp()
        {
            firing = false;
        }

        public void OnReloadButton()
        {
            if (!isReloading)
                StartCoroutine(Reload());
        }
        #endregion

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

            // 1) Audio y efectos visuales
            if (fireClip) audioSource.PlayOneShot(fireClip, volume);
            if (muzzlePrefab && muzzlePool != null)
            {
                var m = muzzlePool.Get(
                    muzzleTransform ? muzzleTransform.position : transform.position,
                    muzzleTransform ? muzzleTransform.rotation : transform.rotation
                );
                StartCoroutine(ReturnAfterDelay(m, 0.6f, muzzlePool));
            }

            // 2) Vibración (móvil)
#if UNITY_ANDROID || UNITY_IOS
            try { Handheld.Vibrate(); } catch { }
#endif

            // 3) **Recoil**: llama a CameraLook.AddRecoil
            Vector2 recoil = new Vector2(
                Random.Range(recoilSideRange.x, recoilSideRange.y),
                recoilPerShot
            );
            lookScript?.AddRecoil(recoil);

            // 4) Cálculo de spread y raycast
            float spread = baseSpread;
            var movement = GetComponentInParent<MobileFPSController>();
            if (movement != null)
                spread *= movement.IsCrouched() ? 0.6f :
                          (movement.GetCurrentSpeed() > 0.1f ? movingSpreadMultiplier : 1f);
            if (isAiming) spread *= adsSpreadMultiplier;

            Vector3 origin = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)).origin;
            Vector3 dir    = RandomConeDirection(playerCamera.transform.forward, spread);

            if (Physics.Raycast(origin, dir, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
                HandleHit(hit);

            UpdateAmmoUI();
        }

        Vector3 RandomConeDirection(Vector3 forward, float maxAngleDeg)
        {
            if (maxAngleDeg <= 0f) return forward;
            float yaw   = Random.Range(-maxAngleDeg, maxAngleDeg);
            float pitch = Random.Range(-maxAngleDeg, maxAngleDeg);
            return Quaternion.Euler(pitch, yaw, 0) * forward;
        }

        void HandleHit(RaycastHit hit)
        {
            if (impactPrefab && impactPool != null)
            {
                var go = impactPool.Get(hit.point + hit.normal * 0.01f, Quaternion.LookRotation(hit.normal));
                StartCoroutine(ReturnAfterDelay(go, 4f, impactPool));
            }

            var health = hit.collider.GetComponent<LifeSystem>();
            if (health != null)
                health.TakeDamage(damage);
            else if (hit.collider.CompareTag(damageableTag))
            {
                var parentHealth = hit.collider.GetComponentInParent<LifeSystem>();
                parentHealth?.TakeDamage(damage);
            }

            if (hit.rigidbody)
                hit.rigidbody.AddForceAtPosition(-hit.normal * 80f, hit.point, ForceMode.Impulse);
        }

        IEnumerator Reload()
        {
            if (isReloading || currentAmmo == magazineSize) yield break;
            isReloading = true;
            if (reloadClip) audioSource.PlayOneShot(reloadClip, volume);
            yield return new WaitForSeconds(reloadTime);
            currentAmmo   = magazineSize;
            isReloading   = false;
            UpdateAmmoUI();
        }

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

        void UpdateAmmoUI()
        {
            // Actualiza tu UI aquí
            Debug.Log($"Ammo: {currentAmmo}/{magazineSize}");
        }

        public int  GetCurrentAmmo() => currentAmmo;
        public bool IsReloading()   => isReloading;

        void OnDisable()
        {
            firing = false;
        }
    }
}