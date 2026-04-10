using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StageMapNodeView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Image iconImage;
    [SerializeField] TMP_Text labelText;

    bool selectable;
    Action onClick;

    public void Bind(StageMapNode node, Color color, string label, bool selectable, Action onClick)
    {
        this.selectable = selectable;
        this.onClick = onClick;

        if (iconImage != null)
        {
            iconImage.color = color;
            iconImage.raycastTarget = true;
        }

        if (labelText != null)
            labelText.text = label;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!selectable)
            return;

        onClick?.Invoke();
    }
}
