using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentItemUI : MonoBehaviour
{
    [Header("Refs")]
    public Button button;
    public Image icon;
    public Image frame;

    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;

    private Equipment equipment;
    private bool isEquipped;
    private int equippedSlotIndex;

    private Action<Equipment, bool, int> onClick;

    public void Init(Equipment eq, bool equipped, int slotIndex, Action<Equipment, bool, int> click)
    {
        equipment = eq;
        isEquipped = equipped;
        equippedSlotIndex = slotIndex;
        onClick = click;

        // ★ 빈 슬롯 판정: null 이거나, 저장/로드로 생긴 "빈 Equipment 객체"면 빈 슬롯
        bool isEmpty =
            (equipment == null) ||
            (string.IsNullOrEmpty(equipment.dataId) && string.IsNullOrEmpty(equipment.instanceId));

        // ★ 스펙: 비어있는 슬롯은 비활성화
        gameObject.SetActive(!isEmpty);

        if (isEmpty)
            return;

        // 데이터 바인딩이 안 되어 있을 수 있으니 안전 처리
        if (equipment.Data == null)
            equipment.BindData();

        ApplyVisual();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(equipment, isEquipped, equippedSlotIndex));
        }
    }


    private void ApplyVisual()
    {
        var d = equipment != null ? equipment.Data : null;

        if (icon != null)
        {
            icon.sprite = d != null ? d.sprite : null;
            icon.enabled = (d != null && d.sprite != null);
        }

        if (frame != null)
            frame.color = EquipmentUIUtil.GetFrameColor(d != null ? d.rarity : EquipmentRarity.Common);

        if (nameText != null)
            nameText.text = d != null ? d.equipmentName : "";

        if (levelText != null)
            levelText.text = ""; // 캐릭터 슬롯 표기에 레벨을 쓰지 않으면 비움(필요하면 연결)
    }
}
