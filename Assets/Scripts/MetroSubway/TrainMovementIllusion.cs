using UnityEngine;

public class TrainMovementIllusion : MonoBehaviour
{
    [Header("Configuración de Sacudidas Principales")]
    public float intensity = 1f; // Intensidad general de las vibraciones
    public float baseShakeFrequency = 0.5f; // Frecuencia base de las vibraciones
    public float accelerationShakeMultiplier = 2f; // Multiplicador de sacudidas durante "aceleración"
    
    [Header("Balanceo Lateral (Wobble)")]
    public float maxLateralTilt = 3f; // Máxima inclinación lateral en grados
    public float lateralWobbleSpeed = 1f; // Velocidad del balanceo lateral
    
    [Header("Vibración Vertical")]
    public float verticalVibrationAmount = 0.05f; // Cantidad de vibración vertical
    public float verticalVibrationSpeed = 2f; // Velocidad de vibración vertical
    
    [Header("Sacudidas de Aceleración")]
    public float accelerationShakeIntensity = 0.2f; // Intensidad de sacudidas de aceleración
    public float accelerationShakeDuration = 1.5f; // Duración de cada sacudida de aceleración
    public Vector3 accelerationShakeDirection = new Vector3(0, 0, -1); // Dirección de las sacudidas (hacia atrás)
    
    [Header("Efectos de Sonido")]
    public AudioSource trainAudioSource;
    public AudioClip engineRumbleClip;
    public AudioClip trackRattleClip;
    public float minRumbleVolume = 0.1f;
    public float maxRumbleVolume = 0.3f;
    public float minRattlePitch = 0.8f;
    public float maxRattlePitch = 1.2f;

    // Variables de estado interno
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float currentSpeed = 0f;
    private float targetSpeed = 0f;
    private float shakeTime = 0f;
    private float accelerationShakeTimer = 0f;
    private bool isAccelerating = false;
    
    // Para el efecto de retorno suave (spring)
    private Vector3 currentVelocity;
    private float angularVelocity;
    private float springForce = 15f;
    private float springDamping = 0.8f;

    void Start()
    {
        // Guardar la posición y rotación originales
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        
        // Configurar audio
        if (trainAudioSource != null)
        {
            trainAudioSource.loop = true;
            trainAudioSource.clip = engineRumbleClip;
            trainAudioSource.Play();
        }
    }

    void Update()
    {
        // Actualizar el estado de movimiento
        UpdateMovementState();
        
        // Calcular las vibraciones y sacudidas
        Vector3 shakeOffset = CalculateShakeOffset();
        Quaternion shakeRotation = CalculateShakeRotation();
        
        // Aplicar el movimiento con efecto de resorte para retorno suave
        ApplySpringMovement(shakeOffset, shakeRotation);
        
        // Actualizar efectos de audio
        UpdateAudioEffects();
    }

    void UpdateMovementState()
    {
        // Simular cambios de velocidad (puedes controlar esto desde otros scripts)
        // Por ejemplo, cuando el jugador "acelera" o "frena"
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime);
        
        // Detectar si está acelerando
        isAccelerating = targetSpeed > currentSpeed;
        
        // Temporizador para sacudidas de aceleración
        if (isAccelerating)
        {
            accelerationShakeTimer += Time.deltaTime;
            if (accelerationShakeTimer > accelerationShakeDuration)
            {
                accelerationShakeTimer = 0f;
            }
        }
        else
        {
            accelerationShakeTimer = 0f;
        }
        
        // Actualizar tiempo base de vibraciones
        shakeTime += Time.deltaTime * baseShakeFrequency * (1f + currentSpeed);
    }

    Vector3 CalculateShakeOffset()
    {
        Vector3 offset = Vector3.zero;
        
        // 1. Vibración base constante (ruido Perlin para movimiento natural)
        float perlinX = Mathf.PerlinNoise(shakeTime, 0f) * 2f - 1f;
        float perlinY = Mathf.PerlinNoise(shakeTime + 0.5f, 0f) * 2f - 1f;
        float perlinZ = Mathf.PerlinNoise(shakeTime + 1f, 0f) * 2f - 1f;
        
        offset.x = perlinX * intensity * 0.05f;
        offset.y = perlinY * intensity * verticalVibrationAmount;
        offset.z = perlinZ * intensity * 0.03f;
        
        // 2. Vibración vertical adicional
        offset.y += Mathf.Sin(shakeTime * verticalVibrationSpeed) * verticalVibrationAmount * 0.5f;
        
        // 3. Sacudidas de aceleración (impulsos hacia atrás)
        if (isAccelerating && accelerationShakeTimer < accelerationShakeDuration * 0.5f)
        {
            float accelerationFactor = accelerationShakeTimer / (accelerationShakeDuration * 0.5f);
            Vector3 accelerationShake = accelerationShakeDirection * accelerationFactor * accelerationShakeIntensity;
            offset += accelerationShake;
        }
        
        return offset;
    }

    Quaternion CalculateShakeRotation()
    {
        Quaternion rotation = originalRotation;
        
        // 1. Balanceo lateral basado en ruido Perlin
        float lateralTilt = Mathf.PerlinNoise(shakeTime * 0.7f, 0f) * 2f - 1f;
        lateralTilt *= maxLateralTilt * (1f + currentSpeed * 0.5f);
        
        // 2. Balanceo adicional con patrón sinusoidal
        float wobble = Mathf.Sin(shakeTime * lateralWobbleSpeed) * maxLateralTilt * 0.3f;
        
        // 3. Inclinación hacia atrás durante aceleración
        float accelerationTilt = 0f;
        if (isAccelerating)
        {
            accelerationTilt = Mathf.Clamp01(accelerationShakeTimer / accelerationShakeDuration) * -2f;
        }
        
        // Combinar todas las rotaciones
        rotation *= Quaternion.Euler(
            accelerationTilt, // Inclinación hacia atrás/aceleración
            lateralTilt * 0.3f, // Pequeño balanceo en Y
            lateralTilt + wobble // Balanceo principal en Z
        );
        
        return rotation;
    }

    void ApplySpringMovement(Vector3 targetOffset, Quaternion targetRotation)
    {
        // Aplicar movimiento con efecto de resorte para retorno suave a posición original
        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition, 
            originalPosition + targetOffset, 
            ref currentVelocity, 
            1f / springForce, 
            Mathf.Infinity, 
            Time.deltaTime
        );
        
        // Aplicar rotación con efecto de resorte
        float angle;
        Vector3 axis;
        Quaternion deltaRotation = targetRotation * Quaternion.Inverse(transform.localRotation);
        deltaRotation.ToAngleAxis(out angle, out axis);
        
        if (angle > 180f) angle -= 360f;
        
        Vector3 angularVelocityVector = axis * (angle * Mathf.Deg2Rad * springForce);
        angularVelocity = Mathf.Lerp(angularVelocity, angularVelocityVector.magnitude, Time.deltaTime * springDamping);
        
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation, 
            targetRotation, 
            angularVelocity * Time.deltaTime
        );
    }

    void UpdateAudioEffects()
    {
        if (trainAudioSource != null)
        {
            // Ajustar volumen del rumor del motor basado en la "velocidad"
            trainAudioSource.volume = Mathf.Lerp(minRumbleVolume, maxRumbleVolume, currentSpeed);
            
            // Ajustar pitch para simular cambios en el motor
            trainAudioSource.pitch = Mathf.Lerp(0.9f, 1.1f, Mathf.PerlinNoise(Time.time * 2f, 0f));
            
            // Reproducir sonido de traqueteo de vías si está disponible
            if (trackRattleClip != null && !trainAudioSource.isPlaying)
            {
                trainAudioSource.PlayOneShot(trackRattleClip, Mathf.Clamp01(currentSpeed * 0.5f));
            }
        }
    }

    // Métodos públicos para controlar el movimiento desde otros scripts
    public void SetTargetSpeed(float speed)
    {
        targetSpeed = Mathf.Clamp01(speed);
    }
    
    public void JerkForward(float intensityMultiplier = 1f)
    {
        // Sacudida fuerte hacia adelante (para simulaciones de arranque)
        accelerationShakeTimer = 0f;
        accelerationShakeIntensity *= intensityMultiplier;
        isAccelerating = true;
    }
    
    public void EmergencyBrake()
    {
        // Sacudida fuerte por frenado de emergencia
        targetSpeed = 0f;
        accelerationShakeDirection = new Vector3(0, 0, 1f); // Dirección invertida
        accelerationShakeIntensity = 0.3f;
        accelerationShakeTimer = 0f;
    }
}