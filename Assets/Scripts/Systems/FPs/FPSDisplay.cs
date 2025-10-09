using UnityEngine;
using TMPro;

public class FPSDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsText;
    private float timer;
    private const float updateInterval = 0.5f;

    void Start()
    {
        // Create Canvas
        fpsText.text = "FPS: 0";
    }

    void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer >= updateInterval)
        {
            int fps = Mathf.RoundToInt(1f / Time.unscaledDeltaTime);
            fpsText.text = $"FPS: {fps}";
            timer = 0f;
        }
    }
}