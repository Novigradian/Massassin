using UnityEngine;
using UnityEngine.EventSystems; // Required for UI events

public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    // Detects when the mouse hovers over the button
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (AudioManager.Instance != null && hoverSound != null)
        {
            AudioManager.Instance.PlaySFX(hoverSound);
        }
    }

    // Detects when the mouse is clicked down
    public void OnPointerDown(PointerEventData eventData)
    {
        if (AudioManager.Instance != null && clickSound != null)
        {
            AudioManager.Instance.PlaySFX(clickSound);
        }
    }
}