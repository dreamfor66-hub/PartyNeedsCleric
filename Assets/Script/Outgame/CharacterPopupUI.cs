using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterPopupUI : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("Equipped Equipments (6)")]
    public EquipmentItemUI weaponSlot;   // 0
    public EquipmentItemUI headSlot;     // 1
    public EquipmentItemUI bodySlot;     // 2
    public EquipmentItemUI glovesSlot;   // 3
    public EquipmentItemUI ring1Slot;    // 4
    public EquipmentItemUI ring2Slot;    // 5

    [Header("Equipped Skills (3)")]
    public OutgameSkillSlotUI[] skillSlots = new OutgameSkillSlotUI[3];
    public SkillPopupUI skillPopup;
    [Header("Equipped Skills Root")]
    public GameObject skillsRoot;

    [Header("Stats (UI)")]
    public Transform statParent;
    public StatUI statPrefab;

    private SkillData lastPopupSkill;
    private bool popupVisible;


    [Header("Info")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public Image icon;

    [Header("Buttons")]
    public Button equipButton;
    public Button unequipButton;
    public Button closeButton;

    private EquipmentPage page;
    private Character character;
    private bool isEquipped;
    private int equippedSlotIndex;

    public void Open(EquipmentPage owner, Character ch, bool equipped, int slotIndex)
    {
        page = owner;
        character = ch;
        isEquipped = equipped;
        equippedSlotIndex = slotIndex;

        if (root != null) root.SetActive(true);

        if (nameText != null) nameText.text = ch != null ? ch.nickname : "";
        if (levelText != null) levelText.text = ch != null ? $"Lv.{ch.level} {ch.Data.characterName}" : "";
        if (icon != null) icon.sprite = ch.Data.sprites.anim_idleFront;

        HideSkillPopup();

        RefreshEquippedEquipments();
        RefreshEquippedSkills();
        RefreshStats();

        if (equipButton != null)
        {
            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(OnClick_Equip);
            equipButton.gameObject.SetActive(!isEquipped);
        }

        if (unequipButton != null)
        {
            unequipButton.onClick.RemoveAllListeners();
            unequipButton.onClick.AddListener(OnClick_Unequip);
            unequipButton.gameObject.SetActive(isEquipped);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
    }

    public void Close()
    {
        HideSkillPopup();

        if (root != null) root.SetActive(false);
        page = null;
        character = null;
        isEquipped = false;
        equippedSlotIndex = -1;
    }

    private void OnClick_Equip()
    {
        if (OutgameAccountManager.Instance == null) return;
        if (character == null) return;

        var r = OutgameAccountManager.Instance.EquipCharacter(character, -1);

        if (r == OutgameAccountManager.CharacterEquipResult.NeedPartySlotChoice)
        {
            page?.BeginPartyReplaceSelect(character);
            Close();
            return;
        }

        OutgameAccountManager.Instance.Save();
        page?.RefreshAll();
        Close();
    }

    private void OnClick_Unequip()
    {
        if (OutgameAccountManager.Instance == null) return;
        if (equippedSlotIndex < 0) return;

        OutgameAccountManager.Instance.UnequipCharacter(equippedSlotIndex);
        page?.RefreshAll();
        Close();
    }
    private void RefreshEquippedEquipments()
    {
        var arr = character != null ? character.equippedEquipments : null;

        weaponSlot?.Init(arr != null ? arr[0] : null, true, 0, OnClick_CharacterEquipment);
        headSlot?.Init(arr != null ? arr[1] : null, true, 1, OnClick_CharacterEquipment);
        bodySlot?.Init(arr != null ? arr[2] : null, true, 2, OnClick_CharacterEquipment);
        glovesSlot?.Init(arr != null ? arr[3] : null, true, 3, OnClick_CharacterEquipment);
        ring1Slot?.Init(arr != null ? arr[4] : null, true, 4, OnClick_CharacterEquipment);
        ring2Slot?.Init(arr != null ? arr[5] : null, true, 5, OnClick_CharacterEquipment);
    }

    private void OnClick_CharacterEquipment(Equipment eq, bool equipped, int slotIndex)
    {
        if (eq == null) return;

        if (eq.Data == null)
            eq.BindData();

        // EquipmentPage의 팝업을 재사용(읽기전용)
        if (page == null || page.popup == null) return;

        page.popup.OpenReadOnly(eq);
    }


    private void RefreshEquippedSkills()
    {
        if (skillSlots == null || skillSlots.Length != 3)
        {
            if (skillsRoot != null) skillsRoot.SetActive(false);
            HideSkillPopup();
            return;
        }

        bool hasAnySkill = false;

        for (int i = 0; i < 3; i++)
        {
            var slot = skillSlots[i];
            if (slot == null) continue;

            // 캐릭터팝업에선 부가 UI 전부 OFF
            if (slot.fixedSlotPanel != null) slot.fixedSlotPanel.SetActive(false);
            if (slot.equippedMarkPanel != null) slot.equippedMarkPanel.SetActive(false);
            if (slot.unusableBlackPanel != null) slot.unusableBlackPanel.SetActive(false);

            SkillData sd = ResolveSkillData(i);

            if (sd != null) hasAnySkill = true;

            slot.SetEquippedSkill(sd);

            int idx = i;
            if (slot.button != null)
            {
                slot.button.onClick.RemoveAllListeners();
                slot.button.onClick.AddListener(() => OnClick_SkillSlot(idx));
            }
        }

        if (skillsRoot != null)
            skillsRoot.SetActive(hasAnySkill);
        else
        {
            // 루트가 없으면 슬롯 자체를 끔
            for (int i = 0; i < skillSlots.Length; i++)
                if (skillSlots[i] != null) skillSlots[i].gameObject.SetActive(hasAnySkill);
        }

        if (!hasAnySkill)
            HideSkillPopup();
    }


    private SkillData ResolveSkillData(int index)
    {
        if (character == null || character.skillIds == null) return null;
        if (index < 0 || index >= character.skillIds.Length) return null;

        string id = character.skillIds[index];
        if (string.IsNullOrEmpty(id)) return null;

        var acc = OutgameAccountManager.Instance;
        if (acc == null) return null;

        return acc.ResolveSkillById(id);
    }

    private void OnClick_SkillSlot(int index)
    {
        var sd = ResolveSkillData(index);
        if (sd == null) return;
        ToggleSkillPopup(sd);
    }

    private void ToggleSkillPopup(SkillData skill)
    {
        if (skillPopup == null) return;

        if (!popupVisible)
        {
            skillPopup.gameObject.SetActive(true);
            skillPopup.Init(skill);
            lastPopupSkill = skill;
            popupVisible = true;
            return;
        }

        if (lastPopupSkill == skill)
        {
            HideSkillPopup();
            return;
        }

        skillPopup.Init(skill);
        lastPopupSkill = skill;
        popupVisible = true;
    }

    private void HideSkillPopup()
    {
        if (skillPopup == null) return;

        skillPopup.gameObject.SetActive(false);
        lastPopupSkill = null;
        popupVisible = false;
    }

    private void RefreshStats()
    {
        if (statParent == null || statPrefab == null) return;

        ClearStats();

        if (character == null || character.Data == null) return;

        var cd = character.Data;

        // Melee/Ranged 공통: 공격력, 체력, 이동속도
        {
            var ui = Instantiate(statPrefab, statParent);
            ui.Apply(EquipmentStatType.Attack, character.atk);
        }
        {
            var ui = Instantiate(statPrefab, statParent);
            ui.Apply(EquipmentStatType.Health, character.hp);
        }
        {
            var ui = Instantiate(statPrefab, statParent);
            ui.Apply(EquipmentStatType.BaseMoveSpeed, cd.moveSpeed);
        }

        // Ranged 추가: 공격 쿨타임, 사거리
        if (cd.characterType == CharacterType.Ranged)
        {
            {
                var ui = Instantiate(statPrefab, statParent);
                ui.Apply(EquipmentStatType.BulletCooldown, cd.bulletCooldown);
            }
            {
                var ui = Instantiate(statPrefab, statParent);
                ui.Apply(EquipmentStatType.RangeRadius, cd.rangeRadius);
            }
        }
    }

    private void ClearStats()
    {
        for (int i = statParent.childCount - 1; i >= 0; i--)
            Destroy(statParent.GetChild(i).gameObject);
    }

}
