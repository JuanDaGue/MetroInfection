using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainMovementSimulator : MonoBehaviour
{
    [Header("Train Movement Settings")]
    public float baseSpeed = 5f;
    public float accelerationRate = 0.5f;
    public float decelerationRate = 1f;
    public float maxSpeed = 15f;
    public float currentSpeed = 0f;
    public bool isMoving = false;

    [Header("Train Wobble & Sway Settings")]
    public float lateralWobbleAmount = 0.05f;
    public float verticalBounceAmount = 0.03f;
    public float wobbleFrequency = 0.8f;
    private float wobbleTime = 0f;

    [Header("Visual Effects References")]
    public MovingEnvironmentController environmentController;
    public Camera playerCamera;
    public Transform[] trainWheels;
    public ParticleSystem[] wheelSmokeEffects;

    [Header("Audio Settings")]
    public AudioSource engineAudioSource;
    public AudioSource tracksAudioSource;
    public AudioSource interiorAmbienceSource;
    public float minEnginePitch = 0.7f;
    public float maxEnginePitch = 1.3f;
    public float minTrackVolume = 0.1f;
    public float maxTrackVolume = 0.4f;
    public AudioClip engineStartClip;
    public AudioClip engineLoopClip;
    public AudioClip engineStopClip;
    public AudioClip trackRumbleClip;
    public AudioClip interiorAmbienceClip;

    [Header("Passenger Effects")]
    public float cameraSwayAmount = 0.5f;
    public float cameraBounceFrequency = 1f;
    private Vector3 cameraOriginalPosition;
    private float cameraSwayTime = 0f;

    private bool isInitialized = false;

    void Start()
    {
        InitializeComponents();
        cameraOriginalPosition = playerCamera.transform.localPosition;
        PlayInteriorAmbience();
    }

    void InitializeComponents()
    {
        // Ensure all necessary components are assigned
        if (environmentController == null)
            environmentController = FindFirstObjectByType<MovingEnvironmentController>();
        
        if (playerCamera == null)
            playerCamera = Camera.main;
        
        if (engineAudioSource == null || tracksAudioSource == null || interiorAmbienceSource == null)
        {
            AudioSource[] audioSources = GetComponents<AudioSource>();
            if (audioSources.Length >= 3)
            {
                engineAudioSource = audioSources[0];
                tracksAudioSource = audioSources[1];
                interiorAmbienceSource = audioSources[2];
            }
        }

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;

        HandleInput();
        UpdateMovement();
        UpdateWobbleEffects();
        UpdateCameraMovement();
        UpdateAudio();
        UpdateWheelRotation();
        UpdateParticleEffects();
    }

    void HandleInput()
    {
        // Simple input handling for train movement
        if (Input.GetKeyDown(KeyCode.W) && !isMoving)
        {
            StartTrain();
        }
        else if (Input.GetKeyDown(KeyCode.S) && isMoving)
        {
            StopTrain();
        }
        else if (Input.GetKeyDown(KeyCode.Space) && isMoving)
        {
            EmergencyBrake();
        }
    }

    public void StartTrain()
    {
        if (!isMoving)
        {
            isMoving = true;
            StartCoroutine(AccelerateTrain());
            PlayEngineStartSound();
        }
    }

    public void StopTrain()
    {
        if (isMoving)
        {
            isMoving = false;
            PlayEngineStopSound();
        }
    }

    public void EmergencyBrake()
    {
        if (isMoving)
        {
            StartCoroutine(DecelerateQuickly());
            // Play brake sound effect could be added here
        }
    }

    IEnumerator AccelerateTrain()
    {
        while (isMoving && currentSpeed < maxSpeed)
        {
            currentSpeed += accelerationRate * Time.deltaTime;
            currentSpeed = Mathf.Clamp(currentSpeed, 0, maxSpeed);
            if (environmentController != null)
            {
                environmentController.SetSpeed(currentSpeed);
            }
            yield return null;
        }
    }

    IEnumerator DecelerateQuickly()
    {
        float brakeSpeed = currentSpeed;
        while (brakeSpeed > 0)
        {
            brakeSpeed -= decelerationRate * 3f * Time.deltaTime;
            currentSpeed = Mathf.Clamp(brakeSpeed, 0, maxSpeed);
            if (environmentController != null)
            {
                environmentController.SetSpeed(currentSpeed);
            }
            yield return null;
        }
        isMoving = false;
    }

    void UpdateMovement()
    {
        if (!isMoving && currentSpeed > 0)
        {
            currentSpeed -= decelerationRate * Time.deltaTime;
            currentSpeed = Mathf.Max(0, currentSpeed);
            if (environmentController != null)
            {
                environmentController.SetSpeed(currentSpeed);
            }
        }
    }

    void UpdateWobbleEffects()
    {
        if (currentSpeed > 0)
        {
            // Calculate train wobble based on speed and time
            wobbleTime += Time.deltaTime * wobbleFrequency * (currentSpeed / maxSpeed);
            
            // Apply lateral wobble (side-to-side movement)
            float lateralWobble = Mathf.PerlinNoise(wobbleTime, 0) * 2f - 1f;
            lateralWobble *= lateralWobbleAmount * (currentSpeed / maxSpeed);
            
            // Apply vertical bounce
            float verticalBounce = Mathf.Sin(wobbleTime * 2f) * verticalBounceAmount * (currentSpeed / maxSpeed);
            
            // Apply to train transform
            transform.localPosition = new Vector3(lateralWobble, verticalBounce, 0);
        }
        else
        {
            // Gradually return to original position
            transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero, Time.deltaTime * 2f);
        }
    }

    void UpdateCameraMovement()
    {
        if (currentSpeed > 0 && playerCamera != null)
        {
            // Subtle camera sway to simulate train movement
            cameraSwayTime += Time.deltaTime * cameraBounceFrequency;
            
            float swayX = Mathf.Sin(cameraSwayTime * 0.7f) * cameraSwayAmount * (currentSpeed / maxSpeed) * 0.1f;
            float swayY = Mathf.Sin(cameraSwayTime * 1.2f) * cameraSwayAmount * (currentSpeed / maxSpeed) * 0.05f;
            float swayZ = Mathf.Sin(cameraSwayTime * 0.5f) * cameraSwayAmount * (currentSpeed / maxSpeed) * 0.1f;
            
            playerCamera.transform.localPosition = cameraOriginalPosition + new Vector3(swayX, swayY, swayZ);
        }
        else if (playerCamera != null)
        {
            // Return camera to original position
            playerCamera.transform.localPosition = Vector3.Lerp(
                playerCamera.transform.localPosition, 
                cameraOriginalPosition, 
                Time.deltaTime * 2f
            );
        }
    }

    void UpdateAudio()
    {
        // Update engine sound based on speed
        if (engineAudioSource != null)
        {
            if (isMoving && currentSpeed > 0)
            {
                if (!engineAudioSource.isPlaying)
                {
                    engineAudioSource.clip = engineLoopClip;
                    engineAudioSource.loop = true;
                    engineAudioSource.Play();
                }
                
                float pitchFactor = Mathf.Lerp(minEnginePitch, maxEnginePitch, currentSpeed / maxSpeed);
                engineAudioSource.pitch = pitchFactor;
                engineAudioSource.volume = Mathf.Lerp(0.3f, 1f, currentSpeed / maxSpeed);
            }
            else if (engineAudioSource.isPlaying && currentSpeed <= 0)
            {
                engineAudioSource.Stop();
            }
        }

        // Update track sounds based on speed
        if (tracksAudioSource != null)
        {
            if (currentSpeed > 0)
            {
                if (!tracksAudioSource.isPlaying)
                {
                    tracksAudioSource.clip = trackRumbleClip;
                    tracksAudioSource.loop = true;
                    tracksAudioSource.Play();
                }
                
                tracksAudioSource.volume = Mathf.Lerp(minTrackVolume, maxTrackVolume, currentSpeed / maxSpeed);
                tracksAudioSource.pitch = Mathf.Lerp(0.8f, 1.2f, currentSpeed / maxSpeed);
            }
            else if (tracksAudioSource.isPlaying && currentSpeed <= 0)
            {
                tracksAudioSource.Stop();
            }
        }
    }

    void UpdateWheelRotation()
    {
        if (trainWheels != null && trainWheels.Length > 0)
        {
            float rotationSpeed = currentSpeed * 100f; // Adjust multiplier for visual appropriateness
            
            foreach (Transform wheel in trainWheels)
            {
                wheel.Rotate(rotationSpeed * Time.deltaTime, 0, 0);
            }
        }
    }

    void UpdateParticleEffects()
    {
        if (wheelSmokeEffects != null && wheelSmokeEffects.Length > 0)
        {
            foreach (ParticleSystem smoke in wheelSmokeEffects)
            {
                var emission = smoke.emission;
                emission.rateOverTime = Mathf.Lerp(0, 30, currentSpeed / maxSpeed);
                
                var main = smoke.main;
                main.startSpeed = Mathf.Lerp(0.5f, 3f, currentSpeed / maxSpeed);
            }
        }
    }

    void PlayEngineStartSound()
    {
        if (engineAudioSource != null && engineStartClip != null)
        {
            engineAudioSource.PlayOneShot(engineStartClip);
            // Start looped sound after a delay
            Invoke("StartEngineLoop", engineStartClip.length - 0.5f);
        }
    }

    void StartEngineLoop()
    {
        if (engineAudioSource != null && engineLoopClip != null && isMoving)
        {
            engineAudioSource.clip = engineLoopClip;
            engineAudioSource.loop = true;
            engineAudioSource.Play();
        }
    }

    void PlayEngineStopSound()
    {
        if (engineAudioSource != null && engineStopClip != null)
        {
            engineAudioSource.loop = false;
            engineAudioSource.Stop();
            engineAudioSource.PlayOneShot(engineStopClip);
        }
    }

    void PlayInteriorAmbience()
    {
        if (interiorAmbienceSource != null && interiorAmbienceClip != null)
        {
            interiorAmbienceSource.clip = interiorAmbienceClip;
            interiorAmbienceSource.loop = true;
            interiorAmbienceSource.Play();
        }
    }

    // Public methods for UI integration
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    public bool IsTrainMoving()
    {
        return isMoving;
    }

    public void SetSpeed(float newSpeed)
    {
        currentSpeed = Mathf.Clamp(newSpeed, 0, maxSpeed);
        if (environmentController != null)
        {
            environmentController.SetSpeed(currentSpeed);
        }
        
        isMoving = currentSpeed > 0;
    }
}