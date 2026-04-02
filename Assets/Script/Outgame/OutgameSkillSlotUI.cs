using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OutgameSkillSlotUI : MonoBehaviour
{
    [Header("Common")]
    public Button button;
    public Image icon;

    [Header("Equipped Slot (Fixed 5)")]
    public GameObject fixedSlotPanel; // 1~5 고정 표기(장착슬롯에서만)
    public TextMeshProUGUI fixedSlotNumberText; // 1~5 고정 표기(장착슬롯에서만)

    [Header("Available List Item")]
    public GameObject equippedMarkPanel;        // 장착된 스킬이면 ON
    public TextMeshProUGUI equippedMarkNumberText; // 1~5

    [Header("Available List Item - Unusable")]
    public GameObject unusableBlackPanel; // 사용 불가면 ON

    public SkillData Data { get; private set; }

    private int equippedSlotIndex = -1;
    private Action<int> onClickEquippedSlot;
    private Action<SkillData> onClickAvailableItem;

    public void InitEquippedSlot(int slotIndex, Action<int> onClick)
    {
        equippedSlotIndex = slotIndex;
        onClickEquippedSlot = onClick;
        onClickAvailableItem = null;
        equippedMarkPanel.SetActive(false);

        if (fixedSlotNumberText != null)
            fixedSlotNumberText.text = (slotIndex + 1).ToString();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClickEquippedSlot?.Invoke(equippedSlotIndex));
        }

        SetEquippedSkill(null);
    }

    public void InitAvailableItem(SkillData skill, Action<SkillData> onClick, bool usable = true)
    {
        Data = skill;
        onClickAvailableItem = onClick;
        onClickEquippedSlot = null;
        fixedSlotPanel.SetActive(false);

        ApplySkillVisual(skill);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClickAvailableItem?.Invoke(Data));
        }

        SetEquippedMark(-1);
        SetUsable(usable);
    }

    public void SetEquippedSkill(SkillData skill)
    {
        Data = skill;

        ApplySkillVisual(skill);
    }
    public void SetUsable(bool usable)
    {
        if (unusableBlackPanel != null)
            unusableBlackPanel.SetActive(!usable);
    }

    public void SetEquippedMark(int slotIndex)
    {
        bool equipped = slotIndex >= 0;

        if (equippedMarkPanel != null)
            equippedMarkPanel.SetActive(equipped);

        if (equippedMarkNumberText != null)
            equippedMarkNumberText.text = equipped ? (slotIndex + 1).ToString() : "";
    }

    private void ApplySkillVisual(SkillData skill)
    {
        bool has = skill != null;

        if (icon != null)
        {
            icon.enabled = has;
            icon.sprite = has ? skill.icon : null;
        }
    }
}
