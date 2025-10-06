using UnityEngine;
using System.Collections;

public class Turret : MonoBehaviour
{
    [Header("Configuración de Apuntado")]
    public float range = 15f;
    public float rotationSpeed = 5f;
    public string enemyTag = "Enemy";
    
    [Header("Configuración de Disparo")]
    public float fireRate = 2f;
    public int damage = 10;
    public LayerMask enemyLayerMask;
    
    [Header("Referencias de Efectos")]
    public Transform rotationBase; // Base que rota horizontalmente
    public Transform barrel; // Cañón que puede elevarse (opcional)
    public Transform firePoint; // Punto de origen del rayo
    public LineRenderer laserLineRenderer;
    public ParticleSystem muzzleFlash;
    public Light muzzleLight;
    public AudioSource fireAudioSource;
    
    [Header("Configuración de Efectos")]
    public Color laserColor = Color.red;
    public float laserDuration = 0.1f;
    public float laserWidth = 0.1f;
    
    private Transform target;
    private float fireCountdown = 0f;
    private bool isFiring = false;

    void Start()
    {
        // Configurar el LineRenderer para el láser
        if (laserLineRenderer != null)
        {
            laserLineRenderer.startColor = laserColor;
            laserLineRenderer.endColor = laserColor;
            laserLineRenderer.startWidth = laserWidth;
            laserLineRenderer.endWidth = laserWidth;
            laserLineRenderer.enabled = false;
        }
        
        // Invocar repetidamente la búsqueda de objetivos para optimizar
        InvokeRepeating("UpdateTarget", 0f, 0.5f);
    }

    void Update()
    {
        if (target == null)
        {
            // Opcional: comportamiento cuando no hay objetivo
            if (laserLineRenderer != null && laserLineRenderer.enabled)
                laserLineRenderer.enabled = false;
            return;
        }

        // Rotar hacia el objetivo
        LockOnTarget();

        // Control de disparo
        if (fireCountdown <= 0f)
        {
            Shoot();
            fireCountdown = 1f / fireRate;
        }
        fireCountdown -= Time.deltaTime;
    }

    void UpdateTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        float shortestDistance = Mathf.Infinity;
        GameObject nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy < shortestDistance && distanceToEnemy <= range)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemy;
            }
        }

        if (nearestEnemy != null)
        {
            target = nearestEnemy.transform;
        }
        else
        {
            target = null;
        }
    }

    void LockOnTarget()
    {
        // Rotación horizontal de la base
        if (rotationBase != null)
        {
            Vector3 dir = target.position - rotationBase.position;
            Quaternion lookRotation = Quaternion.LookRotation(dir);
            Vector3 rotation = Quaternion.Lerp(rotationBase.rotation, lookRotation, Time.deltaTime * rotationSpeed).eulerAngles;
            rotationBase.rotation = Quaternion.Euler(0f, rotation.y, 0f);
        }

        // Rotación vertical del cañón (opcional)
        if (barrel != null)
        {
            Vector3 barrelDir = target.position - barrel.position;
            Quaternion barrelLookRotation = Quaternion.LookRotation(barrelDir);
            Vector3 barrelRotation = Quaternion.Lerp(barrel.rotation, barrelLookRotation, Time.deltaTime * rotationSpeed).eulerAngles;
            barrel.rotation = Quaternion.Euler(barrelRotation.x, rotationBase.rotation.eulerAngles.y, 0f);
        }
    }

    void Shoot()
    {
        // Iniciar corrutina para el efecto completo del disparo
        StartCoroutine(FireLaser());
        
        // Aplicar daño mediante raycast
        RaycastHit hit;
        bool hitSuccess = Physics.Raycast(firePoint.position, firePoint.forward, out hit, range, enemyLayerMask);
        //Debug.Log("Hit " + hitSuccess);
        //Debug.Log("Hit " + hit.collider.name + " for " + damage + " damage.");
        if (hitSuccess)
        {
            // Aplicar daño al enemigo
            LifeSystem enemyHealth = hit.collider.GetComponent<LifeSystem>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
                //Debug.Log("Hit " + hit.collider.name + " for " + damage + " damage.");
                enemyHealth.OnDeath += delegate { Debug.Log("Enemy died."); };
            }

            // Efecto de impacto opcional
            CreateImpactEffect(hit.point);
        }
    }

    IEnumerator FireLaser()
    {
        isFiring = true;

        // 1. Efecto de fogonazo (muzzle flash)
        if (muzzleFlash != null)
            muzzleFlash.Play();

        // 2. Luz momentánea
        if (muzzleLight != null)
        {
            muzzleLight.enabled = true;
            StartCoroutine(FadeLight(muzzleLight, 0.1f));
        }

        // 3. Sonido de disparo
        if (fireAudioSource != null)
            fireAudioSource.Play();

        // 4. Rayo láser visible
        if (laserLineRenderer != null)
        {
            laserLineRenderer.enabled = true;
            
            RaycastHit hit;
            if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, range, enemyLayerMask))
            {
                laserLineRenderer.SetPosition(0, firePoint.position);
                laserLineRenderer.SetPosition(1, hit.point);
            }
            else
            {
                laserLineRenderer.SetPosition(0, firePoint.position);
                laserLineRenderer.SetPosition(1, firePoint.position + firePoint.forward * range);
            }
        }

        // Mantener el láser visible por un breve momento
        yield return new WaitForSeconds(laserDuration);

        // Desactivar el láser
        if (laserLineRenderer != null)
            laserLineRenderer.enabled = false;

        isFiring = false;
    }

    IEnumerator FadeLight(Light light, float duration)
    {
        float startIntensity = light.intensity;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            light.intensity = Mathf.Lerp(startIntensity, 0f, timer / duration);
            yield return null;
        }

        light.enabled = false;
        light.intensity = startIntensity; // Restaurar intensidad original
    }

    void CreateImpactEffect(Vector3 impactPoint)
    {
        // Aquí puedes añadir efectos de impacto como chispas, humo, etc.
        // Puedes instanciar un sistema de partículas en el punto de impacto
    }

    void OnDrawGizmosSelected()
    {
        // Dibujar el rango de la torreta en el editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}