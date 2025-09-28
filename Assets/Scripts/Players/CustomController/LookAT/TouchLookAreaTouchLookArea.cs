using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UnityEngine.RectTransform))]
public class TouchLookArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Tooltip("Reference to the FirstPersonLook to send deltas to")]
    public FirstPersonLook_m lookReceiver;

    Vector2 lastPointerPos;
    bool dragging = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        dragging = true;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, eventData.position, eventData.pressEventCamera, out lastPointerPos);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging) return;

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, eventData.position, eventData.pressEventCamera, out local);
        Vector2 delta = local - lastPointerPos;
        lastPointerPos = local;

        // send delta in pixels to lookReceiver
        if (lookReceiver != null)
            lookReceiver.SetTouchLookDelta(delta);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        dragging = false;
        if (lookReceiver != null)
            lookReceiver.ClearTouchLook();
    }
}
