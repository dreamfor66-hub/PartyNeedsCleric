using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OutgameCharacterItemUI : MonoBehaviour
{
    [Header("Refs")]
    public Button button;
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;

    public Character Data { get; private set; }

    private bool isEquipped;
    private int equippedSlotIndex;
    private Action<Character, bool, int> onClick;

    public void Init(Character ch, bool equipped, int slotIndex, Action<Character, bool, int> click)
    {
        Data = ch;
        isEquipped = equipped;
        equippedSlotIndex = slotIndex;
        onClick = click;

        ApplyVisual();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(Data, isEquipped, equippedSlotIndex));
        }
    }

    private void ApplyVisual()
    {
        var cd = Data != null ? Data.Data : null;

        if (icon != null)
        {
            icon.enabled = (cd != null && cd.icon != null);
            icon.sprite = cd != null ? cd.icon : null;
        }

        if (nameText != null)
            nameText.text = Data != null ? Data.nickname : "";

        if (levelText != null)
            levelText.text = Data != null ? $"Lv.{Data.level}" : "";
    }
}
