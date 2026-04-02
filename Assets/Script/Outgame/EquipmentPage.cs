using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentPage : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("Data")]
    public EquipmentDataTable equipmentTable;

    [Header("Tabs")]
    public Button equipmentTabButton;
    public Button skillTabButton;
    public Button partyTabButton;

    [Header("Tab Layout Roots")]
    public GameObject equipmentLayoutRoot;
    public GameObject skillLayoutRoot;
    public GameObject partyLayoutRoot;

    [Header("Skill Tab - Equipped Skill Slots (Fixed 5)")]
    public OutgameSkillSlotUI[] skillEquippedSlots = new OutgameSkillSlotUI[6];

    [Header("Skill Tab - Skill List (Grid Spawn)")]
    public Transform skillListParent;
    public OutgameSkillSlotUI skillItemPrefab;

    [Header("Skill Tab - Buttons")]
    private bool tabsBuilt = false;

    [Header("Skill Tab - Skill Popup")]
    public SkillPopupUI skillPopupUI;

    private bool skillTabActive;

    private PlayableDataTable table;
    private CharacterData tempCommander;
    private SkillData[] tempSkills = new SkillData[6];

    private SkillData lastPopupSkill;
    private bool popupVisible;

    private readonly List<OutgameSkillSlotUI> skillItems = new();

    [Header("Equipment Tab - Equipment List (Spawn)")]
    public Transform gridParent;
    public EquipmentItemUI itemPrefab;

    [Header("Equipment Tab - Equipped Slot Placeholders (pre-placed EquipmentItemUI)")]
    public EquipmentItemUI weaponSlot;   // slotIndex 0
    public EquipmentItemUI headSlot;    // slotIndex 1
    public EquipmentItemUI bodySlot;     // slotIndex 2
    public EquipmentItemUI glovesSlot;   // slotIndex 3
    public EquipmentItemUI ring1Slot;    // slotIndex 4
    public EquipmentItemUI ring2Slot;    // slotIndex 5

    [Header("Equipment Tab -Popup")]
    public EquipmentPopup popup;

    [Header("Equipment Tab - Accessory Replace Select")]
    public GameObject selectionBlocker;

    [Header("Equipment Tab - Center Info")]
    public Image centerPortraitImage;
    public TextMeshProUGUI centerText;

    private bool isAccessorySelectMode = false;
    private Equipment pendingAccessory = null;

    private readonly List<EquipmentItemUI> spawnedListItems = new();

    private bool equippedAnchorsPrepared = false;
    private SlotAnchor[] anchors = new SlotAnchor[6];
    private EquipmentItemUI[] equippedSpawned = new EquipmentItemUI[6];

    [Serializable]
    private class SlotAnchor
    {
        public Transform parent;
        public int siblingIndex;

        public bool isRect;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;

        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 sizeDelta;
        public Vector3 anchoredPosition3D;
    }


    [Header("Party Tab - Equipped Slots (Fixed 3)")]
    public OutgamePartySlotUI[] partyEquippedSlots = new OutgamePartySlotUI[3];

    [Header("Party Tab - Character List (Spawn)")]
    public Transform partyListParent;          
    public OutgameCharacterItemUI partyItemPrefab;

    [Header("Party Tab - Popup")]
    public CharacterPopupUI characterPopupUI;

    [Header("Party Tab - Characrter Replace Select")]
    public GameObject selectionBlockerParty;

    private int tabIndex = 0;

    private bool isPartySelectMode = false;
    private Character pendingPartyCharacter = null;
    private readonly List<OutgameCharacterItemUI> spawnedPartyItems = new();


    public void OpenPage()
    {
        if (root != null) root.SetActive(true);

        PrepareEquippedSlotAnchors();

        OutgameAccountManager.Instance.Load();

        if (!tabsBuilt)
        {
            if (equipmentTabButton != null)
            {
                equipmentTabButton.onClick.RemoveAllListeners();
                equipmentTabButton.onClick.AddListener(() => SetTab(0));
            }

            if (skillTabButton != null)
            {
                skillTabButton.onClick.RemoveAllListeners();
                skillTabButton.onClick.AddListener(() => SetTab(1));
            }

            if (partyTabButton != null)
            {
                partyTabButton.onClick.RemoveAllListeners();
                partyTabButton.onClick.AddListener(() => SetTab(2));
            }

            for (int i = 0; i < 6 && i < skillEquippedSlots.Length; i++)
            {
                int idx = i;
                if (skillEquippedSlots[idx] == null) continue;
                skillEquippedSlots[idx].InitEquippedSlot(idx, OnClick_EquippedSkillSlot);
            }

            for (int i = 0; i < 3 && i < partyEquippedSlots.Length; i++)
            {
                int idx = i;
                if (partyEquippedSlots[idx] == null) continue;
                partyEquippedSlots[idx].Init(idx, OnClick_PartyEquippedSlot);
            }

            tabsBuilt = true;
        }

        SetTab(0);
    }



    public void ClosePage()
    {
        if (popup != null) popup.Close();
        CancelAccessoryReplaceSelect();

        if (root != null) root.SetActive(false);
    }

    public void RefreshAll()
    {
        if (tabIndex == 1)
        {
            RefreshSkillTab();
            return;
        }

        if (tabIndex == 2)
        {
            RefreshPartyTab();
            return;
        }

        RefreshEquippedSlots();
        RefreshList();
        var acc = OutgameAccountManager.Instance;

        var cd = acc.CurrentCommander;

        Sprite sp = null;
        sp = cd.portrait2;

        centerPortraitImage.sprite = sp;
        centerPortraitImage.enabled = (sp != null);
        
        var name = cd != null ? cd.characterName : "";
        centerText.text = $"Lv.{acc.AccountLevel} {name}";
        
    }


    private void SetTab(int idx)
    {
        tabIndex = idx;

        if (equipmentLayoutRoot != null) equipmentLayoutRoot.SetActive(tabIndex == 0);
        if (skillLayoutRoot != null) skillLayoutRoot.SetActive(tabIndex == 1);
        if (partyLayoutRoot != null) partyLayoutRoot.SetActive(tabIndex == 2);

        if (tabIndex == 1)
        {
            CancelAccessoryReplaceSelect();
            CancelPartyReplaceSelect();
            if (popup != null) popup.Close();
            RefreshSkillTab();
            return;
        }

        if (tabIndex == 2)
        {
            CancelAccessoryReplaceSelect();
            if (popup != null) popup.Close();
            HideSkillPopup();
            RefreshPartyTab();
            return;
        }

        // equipment tab
        CancelPartyReplaceSelect();
        HideSkillPopup();
        RefreshAll();
    }


    // ===================================
    // Equipment Tab
    // =================================== 

    private void PrepareEquippedSlotAnchors()
    {
        if (!Application.isPlaying) return;
        if (equippedAnchorsPrepared) return;

        equippedAnchorsPrepared = true;

        CacheAndDeleteEquippedSlot(ref weaponSlot, 0);
        CacheAndDeleteEquippedSlot(ref headSlot, 1);
        CacheAndDeleteEquippedSlot(ref bodySlot, 2);
        CacheAndDeleteEquippedSlot(ref glovesSlot, 3);
        CacheAndDeleteEquippedSlot(ref ring1Slot, 4);
        CacheAndDeleteEquippedSlot(ref ring2Slot, 5);
    }

    private void CacheAndDeleteEquippedSlot(ref EquipmentItemUI slotUI, int slotIndex)
    {
        if (slotUI == null) return;

        var t = slotUI.transform;
        var a = new SlotAnchor
        {
            parent = t.parent,
            siblingIndex = t.GetSiblingIndex(),
            localPosition = t.localPosition,
            localRotation = t.localRotation,
            localScale = t.localScale,
            isRect = t is RectTransform
        };

        if (a.isRect)
        {
            var rt = (RectTransform)t;
            a.anchorMin = rt.anchorMin;
            a.anchorMax = rt.anchorMax;
            a.pivot = rt.pivot;
            a.sizeDelta = rt.sizeDelta;
            a.anchoredPosition3D = rt.anchoredPosition3D;
        }

        anchors[slotIndex] = a;

        Destroy(slotUI.gameObject);
        slotUI = null;
    }

    private void RefreshEquippedSlots()
    {
        if (OutgameAccountManager.Instance == null) return;

        for (int i = 0; i < 6; i++)
        {
            if (equippedSpawned[i] != null)
            {
                Destroy(equippedSpawned[i].gameObject);
                equippedSpawned[i] = null;
            }
        }

        for (int slotIndex = 0; slotIndex < 6; slotIndex++)
        {
            var eq = OutgameAccountManager.Instance.GetEquippedEquipmentBySlot(slotIndex);
            if (eq == null || eq.instanceId == "")
                continue;

            if (eq.Data == null)
                eq.BindData();

            var a = anchors[slotIndex];

            var ui = Instantiate(itemPrefab, a.parent);
            ApplyAnchor(ui.transform, a);
            ui.transform.SetSiblingIndex(a.siblingIndex);

            ui.Init(eq, true, slotIndex, OnClick_Item);
            equippedSpawned[slotIndex] = ui;
        }
    }

    private void ApplyAnchor(Transform t, SlotAnchor a)
    {
        if (a == null) return;

        t.localPosition = a.localPosition;
        t.localRotation = a.localRotation;
        t.localScale = a.localScale;

        if (!a.isRect) return;

        var rt = t as RectTransform;
        if (rt == null) return;

        rt.anchorMin = a.anchorMin;
        rt.anchorMax = a.anchorMax;
        rt.pivot = a.pivot;
        rt.sizeDelta = a.sizeDelta;
        rt.anchoredPosition3D = a.anchoredPosition3D;
    }

    // EquipmentPage.cs - RefreshList() 교체
    private void RefreshList()
    {
        ClearChildren(gridParent);
        spawnedListItems.Clear();

        if (OutgameAccountManager.Instance == null) return;

        var owned = OutgameAccountManager.Instance.GetOwnedEquipments();
        var equipped = OutgameAccountManager.Instance.GetEquippedEquipments();

        var equippedSet = new HashSet<string>();
        for (int i = 0; i < equipped.Length; i++)
        {
            var e = equipped[i];
            if (e == null) continue;
            if (string.IsNullOrEmpty(e.instanceId)) continue;

            equippedSet.Add(e.instanceId);
        }


        var entries = new List<Equipment>();

        for (int i = 0; i < owned.Count; i++)
        {
            var eq = owned[i];
            if (eq == null) continue;

            // ★ 장착중인 장비는 리스트에서 제외
            if (!string.IsNullOrEmpty(eq.instanceId) && equippedSet.Contains(eq.instanceId))
                continue;

            if (eq.Data == null) eq.BindData();
            if (eq.Data == null) continue;

            entries.Add(eq);
        }

        entries.Sort((a, b) =>
        {
            int r = ((int)b.Data.rarity).CompareTo((int)a.Data.rarity);
            if (r != 0) return r;

            int s = ((int)a.Data.slotType).CompareTo((int)b.Data.slotType);
            if (s != 0) return s;

            int idc = string.CompareOrdinal(a.Data.id, b.Data.id);
            if (idc != 0) return idc;

            return string.CompareOrdinal(a.instanceId, b.instanceId);
        });

        for (int i = 0; i < entries.Count; i++)
        {
            var eq = entries[i];
            var ui = Instantiate(itemPrefab, gridParent);

            // 리스트는 전부 "미장착"으로 표시
            ui.Init(eq, false, -1, OnClick_Item);
            spawnedListItems.Add(ui);
        }
    }

    private void OnClick_Item(Equipment eq, bool isEquipped, int equippedSlotIndex)
    {
        if (tabIndex != 0) return;

        if (isAccessorySelectMode)
        {
            if (equippedSlotIndex == 4 || equippedSlotIndex == 5)
                CompleteAccessoryReplaceSelect(equippedSlotIndex);
            return;
        }

        if (popup == null) return;
        popup.Open(this, eq, isEquipped, equippedSlotIndex, false);
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (child == null) continue;

            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
    }

    public void BeginAccessoryReplaceSelect(Equipment accessory)
    {
        pendingAccessory = accessory;
        isAccessorySelectMode = true;

        if (selectionBlocker != null)
            selectionBlocker.SetActive(true);
    }

    public void CancelAccessoryReplaceSelect()
    {
        pendingAccessory = null;
        isAccessorySelectMode = false;

        if (selectionBlocker != null)
            selectionBlocker.SetActive(false);
    }

    private void CompleteAccessoryReplaceSelect(int slotIndex)
    {
        if (OutgameAccountManager.Instance == null) { CancelAccessoryReplaceSelect(); return; }
        if (pendingAccessory == null) { CancelAccessoryReplaceSelect(); return; }

        OutgameAccountManager.Instance.EquipEquipment(pendingAccessory, slotIndex);
        OutgameAccountManager.Instance.Save();

        CancelAccessoryReplaceSelect();
        RefreshAll();
    }

    // ====================================
    // Skill Tab
    // ====================================

    private void RefreshSkillTab()
    {
        var acc = OutgameAccountManager.Instance;
        if (acc == null) return;

        table = acc.PlayableTable;

        PullSkillTabFromAccount();
        SpawnSkillList();
        RefreshSkillEquippedSlots();
        RefreshSkillEquippedMarks();
    }
    private void PullSkillTabFromAccount()
    {
        var acc = OutgameAccountManager.Instance;
        if (acc == null || table == null)
        {
            tempCommander = null;
            tempSkills = new SkillData[6];
            return;
        }

        tempCommander = acc.CurrentCommander;

        if (tempCommander == null && table.commanders != null && table.commanders.Count > 0)
            tempCommander = table.commanders[0];

        var preset = acc.GetPreset(tempCommander);
        tempSkills = new SkillData[6];
        for (int i = 0; i < 6; i++)
            tempSkills[i] = preset[i];

        HideSkillPopup();
    }
    private void SpawnSkillList()
    {
        if (skillListParent == null || skillItemPrefab == null) return;
        if (table == null || table.skills == null) return;

        ClearChildren(skillListParent);
        skillItems.Clear();

        var usable = new List<SkillData>();
        var unusable = new List<SkillData>();

        for (int i = 0; i < table.skills.Count; i++)
        {
            var sd = table.skills[i];
            if (sd == null) continue;

            if (IsSkillUsableForCurrentCommander(sd)) usable.Add(sd);
            else unusable.Add(sd);
        }

        for (int i = 0; i < usable.Count; i++)
        {
            var sd = usable[i];
            var item = Instantiate(skillItemPrefab, skillListParent);
            item.InitAvailableItem(sd, OnClick_SkillListItem, usable: true);
            skillItems.Add(item);
        }

        for (int i = 0; i < unusable.Count; i++)
        {
            var sd = unusable[i];
            var item = Instantiate(skillItemPrefab, skillListParent);
            item.InitAvailableItem(sd, OnClick_SkillListItem, usable: false);
            skillItems.Add(item);
        }
    }
    private void RefreshSkillEquippedSlots()
    {
        for (int i = 0; i < 6 && i < skillEquippedSlots.Length; i++)
        {
            if (skillEquippedSlots[i] == null) continue;
            skillEquippedSlots[i].SetEquippedSkill(tempSkills[i]);
        }
    }
    private void RefreshSkillEquippedMarks()
    {
        for (int i = 0; i < skillItems.Count; i++)
        {
            var item = skillItems[i];
            int slot = FindSlotOfSkill(item.Data);
            item.SetEquippedMark(slot);
        }
    }
    private void OnClick_EquippedSkillSlot(int slotIndex)
    {
        var s = tempSkills[slotIndex];
        ToggleSkillPopup(s);
    }
    private void OnClick_SkillListItem(SkillData sd)
    {
        ToggleSkillPopup(sd);

        if (!IsSkillUsableForCurrentCommander(sd))
        {
            OutgameUIManager.ShowToast($"{sd.usableClasses[0].characterName} 선택 시 사용 가능");
            return;
        }

        int equippedSlot = FindSlotOfSkill(sd);
        if (equippedSlot >= 0)
        {
            tempSkills[equippedSlot] = null; // 앞으로 당기지 않음

            OutgameAccountManager.Instance.ApplySelection(tempCommander, tempSkills);
            OutgameAccountManager.Instance.Save();

            RefreshSkillEquippedSlots();
            RefreshSkillEquippedMarks();
            return;
        }

        int empty = FindFirstEmptySlot();
        if (empty >= 0)
        {
            tempSkills[empty] = sd; // 앞에서부터 채움

            OutgameAccountManager.Instance.ApplySelection(tempCommander, tempSkills);
            OutgameAccountManager.Instance.Save();

            RefreshSkillEquippedSlots();
            RefreshSkillEquippedMarks();
        }
    }


    private int FindSlotOfSkill(SkillData sd)
    {
        if (sd == null) return -1;

        for (int i = 0; i < 6; i++)
            if (tempSkills[i] == sd) return i;

        return -1;
    }
    private int FindFirstEmptySlot()
    {
        for (int i = 0; i < 6; i++)
            if (tempSkills[i] == null) return i;

        return -1;
    }
    private void ToggleSkillPopup(SkillData skill)
    {
        if (skillPopupUI == null) return;

        if (!popupVisible)
        {
            skillPopupUI.gameObject.SetActive(true);
            skillPopupUI.Init(skill);
            lastPopupSkill = skill;
            popupVisible = true;
            return;
        }

        if (lastPopupSkill == skill)
        {
            HideSkillPopup();
            return;
        }

        skillPopupUI.Init(skill);
        lastPopupSkill = skill;
        popupVisible = true;
    }
    private void HideSkillPopup()
    {
        if (skillPopupUI == null) return;

        skillPopupUI.gameObject.SetActive(false);
        lastPopupSkill = null;
        popupVisible = false;
    }
    private bool IsSkillUsableForCurrentCommander(SkillData s)
    {
        if (s == null || tempCommander == null) return false;

        if (s.usableClasses == null || s.usableClasses.Count == 0)
            return true;

        return s.usableClasses.Contains(tempCommander);
    }
    // ====================================
    // Party Tab
    // ====================================

    private void RefreshPartyTab()
    {
        var acc = OutgameAccountManager.Instance;
        if (acc == null) return;

        var equipped = acc.GetEquippedCharacters();

        // top slots
        for (int i = 0; i < 3 && i < partyEquippedSlots.Length; i++)
        {
            if (partyEquippedSlots[i] == null) continue;
            partyEquippedSlots[i].Set(equipped != null ? equipped[i] : null);
        }

        // list (exclude equipped)
        if (partyListParent == null || partyItemPrefab == null) return;

        ClearChildren(partyListParent);
        spawnedPartyItems.Clear();

        var owned = acc.GetOwnedCharacters();

        var equippedSet = new HashSet<string>();
        if (equipped != null)
        {
            for (int i = 0; i < equipped.Length; i++)
            {
                var ch = equipped[i];
                if (ch == null) continue;
                if (string.IsNullOrEmpty(ch.instanceId)) continue;
                equippedSet.Add(ch.instanceId);
            }
        }

        var entries = new List<Character>();
        for (int i = 0; i < owned.Count; i++)
        {
            var ch = owned[i];
            if (ch == null) continue;

            if (!string.IsNullOrEmpty(ch.instanceId) && equippedSet.Contains(ch.instanceId))
                continue;

            ch.BindData(acc.PlayableTable);
            entries.Add(ch);
        }

        entries.Sort((a, b) =>
        {
            int lv = b.level.CompareTo(a.level);
            if (lv != 0) return lv;
            return string.CompareOrdinal(a.nickname, b.nickname);
        });

        for (int i = 0; i < entries.Count; i++)
        {
            var ch = entries[i];
            var ui = Instantiate(partyItemPrefab, partyListParent);
            ui.Init(ch, false, -1, OnClick_PartyItem);
            spawnedPartyItems.Add(ui);
        }

        if (characterPopupUI != null && characterPopupUI.gameObject.activeSelf)
        {
            // 열린 상태 유지해도 상관없지만, 갱신 꼬임 방지로 닫음
            characterPopupUI.Close();
        }
    }

    private void OnClick_PartyItem(Character ch, bool isEquipped, int equippedSlotIndex)
    {
        if (tabIndex != 2) return;
        if (isPartySelectMode) return;

        if (characterPopupUI == null) return;
        characterPopupUI.Open(this, ch, false, -1);
    }

    private void OnClick_PartyEquippedSlot(int slotIndex)
    {
        if (tabIndex != 2) return;

        if (isPartySelectMode)
        {
            CompletePartyReplaceSelect(slotIndex);
            return;
        }

        var acc = OutgameAccountManager.Instance;
        if (acc == null) return;

        var ch = acc.GetEquippedCharacterBySlot(slotIndex);
        if (ch == null) return;

        if (characterPopupUI == null) return;
        characterPopupUI.Open(this, ch, true, slotIndex);
    }

    public void BeginPartyReplaceSelect(Character ch)
    {
        pendingPartyCharacter = ch;
        isPartySelectMode = true;

        if (selectionBlockerParty != null)
            selectionBlockerParty.SetActive(true);
    }

    public void CancelPartyReplaceSelect()
    {
        pendingPartyCharacter = null;
        isPartySelectMode = false;

        if (selectionBlockerParty != null)
            selectionBlockerParty.SetActive(false);
    }

    private void CompletePartyReplaceSelect(int slotIndex)
    {
        if (OutgameAccountManager.Instance == null) { CancelPartyReplaceSelect(); return; }
        if (pendingPartyCharacter == null) { CancelPartyReplaceSelect(); return; }

        OutgameAccountManager.Instance.EquipCharacter(pendingPartyCharacter, slotIndex);
        OutgameAccountManager.Instance.Save();

        CancelPartyReplaceSelect();
        RefreshAll();
    }

}
