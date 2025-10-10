using System.Collections;
using UnityEngine;

public class OpenAndCloseDoor : MonoBehaviour
{
    public GameObject leftDoor;
    public GameObject rightDoor;

    [Tooltip("Time in seconds the open/close animation takes")]
    public float duration = 1f;

    [Tooltip("How far each door moves when opening")]
    public float doorOpenDistance = 1f;

    private Vector3 originPositionLeft;
    private Vector3 originPositionRight;

    private bool isOpen = false;
    private Coroutine currentCoroutine;

    void Start()
    {
        if (leftDoor == null || rightDoor == null)
        {
            Debug.LogError("Left or Right door not assigned");
            enabled = false;
            return;
        }

        originPositionLeft = leftDoor.transform.position;
        originPositionRight = rightDoor.transform.position;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            TryToggleDoor();
        }
    }

    void TryToggleDoor()
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        if (isOpen)
            currentCoroutine = StartCoroutine(AnimateDoors(originPositionLeft, originPositionRight, false));
        else
        {
            Vector3 targetLeft = originPositionLeft + new Vector3(0f, 0f, 1f) * doorOpenDistance;
            Vector3 targetRight = originPositionRight - new Vector3(0f, 0f, 1f) * doorOpenDistance;
            currentCoroutine = StartCoroutine(AnimateDoors(targetLeft, targetRight, true));
        }
    }

    IEnumerator AnimateDoors(Vector3 targetLeft, Vector3 targetRight, bool opening)
    {
        Vector3 startLeft = leftDoor.transform.position;
        Vector3 startRight = rightDoor.transform.position;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t); // smooth easing, optional

            leftDoor.transform.position = Vector3.Lerp(startLeft, targetLeft, t);
            rightDoor.transform.position = Vector3.Lerp(startRight, targetRight, t);

            yield return null;
        }

        leftDoor.transform.position = targetLeft;
        rightDoor.transform.position = targetRight;

        isOpen = opening;
        currentCoroutine = null;
    }
}