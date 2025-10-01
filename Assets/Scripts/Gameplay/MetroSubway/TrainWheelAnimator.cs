using UnityEngine;

public class TrainWheelAnimator : MonoBehaviour
{
    public float wheelRadius = 0.5f;
    public float currentSpeed = 0f;
    
    void Update()
    {
        if (currentSpeed > 0)
        {
            float rotationAmount = (currentSpeed * Time.deltaTime) / (2 * Mathf.PI * wheelRadius) * 360f;
            transform.Rotate(rotationAmount, 0, 0);
        }
    }
    
    public void SetSpeed(float speed)
    {
        currentSpeed = speed;
    }
}