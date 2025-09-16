using UnityEngine;

public class TrackSegment : MonoBehaviour
{
    public float scrollSpeedMultiplier = 1f;
    private Material trackMaterial;
    private Vector2 currentOffset = Vector2.zero;

    void Start()
    {
        // Get material for texture scrolling
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            trackMaterial = renderer.material;
        }
    }

    void Update()
    {
        // Scroll texture for additional movement effect
        if (trackMaterial != null)
        {
            currentOffset.y += Time.deltaTime * scrollSpeedMultiplier;
            trackMaterial.mainTextureOffset = currentOffset;
        }
    }
}