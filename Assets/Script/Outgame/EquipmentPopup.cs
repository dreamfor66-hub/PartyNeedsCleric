using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentPopup : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("Info")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI rarityTypeText;
    public TextMeshProUGUI levelText;
    public Image icon;
    public Image frame;

    [Header("Stats")]
    public Transform statParent;
    public StatUI statPrefab;

    [Header("Options")]
    public Transform optionParent;
    public EquipmentOptionUI optionPrefab;

    [Header("Buttons")]
    public Button equipButton;
    public Button unequipButton;
    public Button closeButton;
    public Button compareButton;
    public Button compareBackButton; // 추가

    private EquipmentPopup comparePopup;
    private EquipmentPopup masterPopup; // 추가
    private RectTransform rect;
    private Vector2 originPos;
    private Vector2 leftPos;
    private Vector2 rightPos;
    private bool compareSwapped;
    private bool isComparePopup;

    private EquipmentPage page;
    private Equipment equipment;
    private bool isEquipped;
    private int equippedSlotIndex;

    public float compareMoveDuration = 0.3f;

    Coroutine compareMoveCo;

    // EquipmentPopup.cs - Open(...) 교체
    public void Open(EquipmentPage ownerPage, Equipment eq, bool equipped, int slotIndex, bool readOnly)
    {
        page = ownerPage;
        equipment = eq;
        isEquipped = equipped;
        equippedSlotIndex = slotIndex;

        if (root != null) root.SetActive(true);

        if (rect == null) rect = GetComponent<RectTransform>();

        originPos = rect.anchoredPosition;

        float w = rect.rect.width;
        float off = w + 40f;
        leftPos = originPos + new Vector2(-off, 0f);
        rightPos = originPos + new Vector2(off, 0f);

        rect.anchoredPosition = originPos;
        compareSwapped = false;

        if (!isComparePopup)
            DestroyComparePopup();

        var d = equipment != null ? equipment.Data : null;

        if (icon != null) icon.sprite = d != null ? d.sprite : null;
        if (frame != null) frame.color = EquipmentUIUtil.GetFrameColor(d != null ? d.rarity : EquipmentRarity.Common);

        if (nameText != null)
        {
            nameText.text = d != null ? d.equipmentName : "";
            nameText.color = EquipmentUIUtil.GetNameColor(d != null ? d.rarity : EquipmentRarity.Common);
        }

        if (descText != null) descText.text = d != null ? d.desc : "";

        if (rarityTypeText != null)
        {
            string rarity = d != null ? GetRarityKorean(d.rarity) : "";
            string type = d != null ? GetSlotTypeKorean(d.slotType) : "";
            rarityTypeText.text = $"{rarity} {type}".Trim();
        }

        if (equipButton != null)
        {
            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(OnClick_Equip);
            equipButton.gameObject.SetActive(!isEquipped && !isComparePopup);
        }

        if (unequipButton != null)
        {
            unequipButton.onClick.RemoveAllListeners();
            unequipButton.onClick.AddListener(OnClick_Unequip);
            unequipButton.gameObject.SetActive(isEquipped && !isComparePopup);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
            closeButton.gameObject.SetActive(true);
        }

        if (compareButton != null)
        {
            compareButton.onClick.RemoveAllListeners();
            compareButton.onClick.AddListener(OnClick_Compare);
            compareButton.gameObject.SetActive(false);
        }

        if (compareBackButton != null)
        {
            compareBackButton.onClick.RemoveAllListeners();
            compareBackButton.onClick.AddListener(OnClick_Compare);
            compareBackButton.gameObject.SetActive(false);
        }

        if (readOnly)
        {
            if (equipButton != null) equipButton.gameObject.SetActive(false);
            if (unequipButton != null) unequipButton.gameObject.SetActive(false);
            if (compareButton != null) compareButton.gameObject.SetActive(false);
            if (compareBackButton != null) compareBackButton.gameObject.SetActive(false);

            // 비교 팝업이 이미 떠있던 경우도 제거
            DestroyComparePopup();

            // 스탯/옵션 표시는 기존대로 유지하려면, 기존 코드에서 쓰던 방식 그대로 호출
            // (프로젝트에 이미 있는 메서드 그대로 사용)
            ApplyStatSlots(null);
            ApplyOptions();

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
                closeButton.gameObject.SetActive(true);
            }

            return;
        }


        // Compare Popup도 스탯/옵션은 표기
        if (isComparePopup)
        {
            ApplyStatSlots(null);
            ApplyOptions();

            if (compareButton != null) compareButton.gameObject.SetActive(false);
            if (equipButton != null) equipButton.gameObject.SetActive(false);
            if (unequipButton != null) unequipButton.gameObject.SetActive(false);
            return;
        }

        Equipment compareEq = null;

        if (!isEquipped && d != null && OutgameAccountManager.Instance != null)
        {
            int compareSlotIndex;
            var equippedEq = GetCompareEquippedEquipment(d.slotType, out compareSlotIndex);

            if (equippedEq != null && equippedEq != equipment)
            {
                compareEq = equippedEq;

                comparePopup = Instantiate(this, transform.parent);
                comparePopup.isComparePopup = true;
                comparePopup.masterPopup = this;

                comparePopup.Open(page, equippedEq, true, compareSlotIndex, false);

                if (comparePopup.rect == null) comparePopup.rect = comparePopup.GetComponent<RectTransform>();
                comparePopup.rect.anchoredPosition = rightPos;

                if (comparePopup.compareButton != null) comparePopup.compareButton.gameObject.SetActive(false);

                if (comparePopup.compareBackButton != null)
                {
                    comparePopup.compareBackButton.onClick.RemoveAllListeners();
                    comparePopup.compareBackButton.onClick.AddListener(OnClick_Compare);
                    comparePopup.compareBackButton.gameObject.SetActive(false);
                }

                if (comparePopup.equipButton != null) comparePopup.equipButton.gameObject.SetActive(false);
                if (comparePopup.unequipButton != null) comparePopup.unequipButton.gameObject.SetActive(false);

                UpdateCompareButtons();
            }
        }

        ApplyStatSlots(compareEq);
        ApplyOptions();

        UpdateCompareButtons();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)optionParent);
    }
    public void OpenReadOnly(Equipment eq)
    {
        Open(null, eq, false, -1, true);
    }

    public void Close()
    {
        // Clone에서 닫기 누르면 마스터를 닫아서 둘 다 정리
        if (isComparePopup)
        {
            if (masterPopup != null)
            {
                masterPopup.Close();
                return;
            }
        }

        DestroyComparePopup();

        if (rect == null) rect = GetComponent<RectTransform>();
        rect.anchoredPosition = originPos;

        compareSwapped = false;

        if (root != null) root.SetActive(false);
        page = null;
        equipment = null;
        isEquipped = false;
        equippedSlotIndex = -1;
    }



    private void OnClick_Equip()
    {
        if (OutgameAccountManager.Instance == null || equipment == null)
            return;

        var result = OutgameAccountManager.Instance.EquipEquipment(equipment, -1);

        if (result == OutgameAccountManager.EquipResult.Failed)
            return;

        if (result == OutgameAccountManager.EquipResult.NeedAccessoryChoice)
        {
            page?.BeginAccessoryReplaceSelect(equipment);
            Close();
            return;
        }

        OutgameAccountManager.Instance.Save();
        page?.RefreshAll();
        Close();
    }

    private void OnClick_Unequip()
    {
        if (OutgameAccountManager.Instance == null)
            return;
        if (equippedSlotIndex < 0)
            return;

        OutgameAccountManager.Instance.UnequipEquipment(equippedSlotIndex);

        page?.RefreshAll();
        Close();
    }
    private void OnClick_Compare()
    {
        if (comparePopup == null) return;

        if (rect == null) rect = GetComponent<RectTransform>();
        if (comparePopup.rect == null) comparePopup.rect = comparePopup.GetComponent<RectTransform>();

        compareSwapped = !compareSwapped;

        if (compareMoveCo != null)
            StopCoroutine(compareMoveCo);

        Vector2 aFrom = rect.anchoredPosition;
        Vector2 bFrom = comparePopup.rect.anchoredPosition;

        Vector2 aTo;
        Vector2 bTo;

        if (compareSwapped)
        {
            aTo = leftPos;
            bTo = originPos;
        }
        else
        {
            aTo = originPos;
            bTo = rightPos;
        }

        compareMoveCo = StartCoroutine(CoMoveCompare(aFrom, aTo, bFrom, bTo, compareMoveDuration));
        UpdateCompareButtons();
    }
    private IEnumerator CoMoveCompare(Vector2 aFrom, Vector2 aTo, Vector2 bFrom, Vector2 bTo, float dur)
    {
        float t = 0f;

        if (dur <= 0f)
        {
            rect.anchoredPosition = aTo;
            comparePopup.rect.anchoredPosition = bTo;
            yield break;
        }

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = t / dur;
            if (p > 1f) p = 1f;

            rect.anchoredPosition = Vector2.LerpUnclamped(aFrom, aTo, p);
            comparePopup.rect.anchoredPosition = Vector2.LerpUnclamped(bFrom, bTo, p);

            yield return null;
        }

        rect.anchoredPosition = aTo;
        comparePopup.rect.anchoredPosition = bTo;
    }

    private void DestroyComparePopup()
    {
        if (comparePopup != null)
        {
            Destroy(comparePopup.gameObject);
            comparePopup = null;
        }

        compareSwapped = false;

        UpdateCompareButtons();
    }

    // EquipmentPopup.cs - ApplyStatSlots(...) 교체

    private void ApplyStatSlots(Equipment compareEq)
    {
        ClearStats();

        statParent.gameObject.SetActive(true);

        var d = equipment.Data;

        int stat1 = Mathf.RoundToInt(equipment.rolledStat1);
        int stat2 = Mathf.RoundToInt(equipment.rolledStat2);

        if (stat1 != 0)
        {
            var ui = Instantiate(statPrefab, statParent);
            ui.Apply(d.statType1, stat1, compareEq, isComparePopup);
        }

        if (stat2 != 0)
        {
            var ui = Instantiate(statPrefab, statParent);
            ui.Apply(d.statType2, stat2, compareEq, isComparePopup);
        }

        if (stat1 == 0 && stat2 == 0)
        {
            statParent.gameObject.SetActive(false);
        }
    }


    private void ClearStats()
    {
        for (int i = statParent.childCount - 1; i >= 0; i--)
            Destroy(statParent.GetChild(i).gameObject);
    }

    // EquipmentPopup.cs - (추가) ApplyOptions()
    private void ApplyOptions()
    {
        ClearOptions();

        var gv = GameVariables.Instance;

        var map = new Dictionary<string, EquipmentOptionData>();

        foreach (var kv in gv.equipmentOptionPoolRed.BuildMap()) map[kv.Key] = kv.Value;
        foreach (var kv in gv.equipmentOptionPoolBlue.BuildMap()) map[kv.Key] = kv.Value;
        foreach (var kv in gv.equipmentOptionPoolGreen.BuildMap()) map[kv.Key] = kv.Value;
        foreach (var kv in gv.equipmentOptionPoolYellow.BuildMap()) map[kv.Key] = kv.Value;

        for (int i = 0; i < equipment.options.Count; i++)
        {
            var opt = equipment.options[i];

            var def = map[opt.id];
            string desc = def.desc;

            bool isPercent = false;

            switch (def.optionType)
            {
                case EquipmentOptionType.FlatStat:
                    switch (def.statType)
                    {
                        case EquipmentStatType.CastHaste:
                        case EquipmentStatType.CooldownHaste:
                            isPercent = true;
                            break;

                        // BaseManaRegen 포함 나머지는 % 아님
                        default:
                            isPercent = false;
                            break;
                    }
                    break;

                case EquipmentOptionType.SkillTagCooldownHaste:
                case EquipmentOptionType.DamageEnhance:
                    isPercent = true;
                    break;

                default:
                    isPercent = false;
                    break;
            }

            string valueText = isPercent ? $"{opt.value}%" : opt.value.ToString();

            var ui = Instantiate(optionPrefab, optionParent);
            ui.Set(desc, valueText, GetOptionIconColor(opt.id));
        }
    }
    // EquipmentPopup.cs - (추가) ClearOptions()
    private void ClearOptions()
    {
        for (int i = optionParent.childCount - 1; i >= 0; i--)
            Destroy(optionParent.GetChild(i).gameObject);
    }

    // EquipmentPopup.cs - (추가) GetOptionIconColor(...)
    private Color GetOptionIconColor(string optionId)
    {
        var parts = optionId.Split('_'); // EquipmentOption, Green, AtkUp...
        var gv = GameVariables.Instance;

        switch (parts[1])
        {
            case "Red": return gv.equipmentOptionColorRed;
            case "Blue": return gv.equipmentOptionColorBlue;
            case "Green": return gv.equipmentOptionColorGreen;
            case "Yellow": return gv.equipmentOptionColorYellow;
        }

        throw new System.Exception($"Unknown option color: {parts[1]} ({optionId})");
    }



    private void UpdateCompareButtons()
    {
        bool has = (comparePopup != null);

        if (compareButton != null)
            compareButton.gameObject.SetActive(has && !compareSwapped && !isComparePopup);

        if (compareBackButton != null)
            compareBackButton.gameObject.SetActive(false); // 마스터는 CompareBack 안씀

        if (has && comparePopup.compareBackButton != null)
            comparePopup.compareBackButton.gameObject.SetActive(compareSwapped);

        if (has && comparePopup.compareButton != null)
            comparePopup.compareButton.gameObject.SetActive(false);
    }

    private Equipment GetCompareEquippedEquipment(EquipmentSlotType slotType, out int slotIndex)
    {
        slotIndex = -1;

        if (OutgameAccountManager.Instance == null) return null;

        if (slotType == EquipmentSlotType.Ring)
        {
            var e4 = OutgameAccountManager.Instance.GetEquippedEquipmentBySlot(4);
            if (e4 != null && !string.IsNullOrEmpty(e4.instanceId))
            {
                slotIndex = 4;
                return e4;
            }

            var e5 = OutgameAccountManager.Instance.GetEquippedEquipmentBySlot(5);
            if (e5 != null && !string.IsNullOrEmpty(e5.instanceId))
            {
                slotIndex = 5;
                return e5;
            }

            return null;
        }

        int idx = -1;

        switch (slotType)
        {
            case EquipmentSlotType.Weapon: idx = 0; break;
            case EquipmentSlotType.Head: idx = 1; break;
            case EquipmentSlotType.Body: idx = 2; break;
            case EquipmentSlotType.Gloves: idx = 3; break;
        }

        if (idx < 0) return null;

        var eq = OutgameAccountManager.Instance.GetEquippedEquipmentBySlot(idx);
        if (eq == null || string.IsNullOrEmpty(eq.instanceId))
            return null;

        slotIndex = idx;
        return eq;
    }


    private string GetRarityKorean(EquipmentRarity r)
    {
        switch (r)
        {
            case EquipmentRarity.Common: return "커먼";
            case EquipmentRarity.Uncommon: return "언커먼";
            case EquipmentRarity.Rare: return "레어";
            case EquipmentRarity.Epic: return "에픽";
            case EquipmentRarity.Legendary: return "전설";
            case EquipmentRarity.Unique: return "유니크";
            default: return "커먼";
        }
    }

    private string GetSlotTypeKorean(EquipmentSlotType t)
    {
        switch (t)
        {
            case EquipmentSlotType.Weapon: return "무기";
            case EquipmentSlotType.Head: return "모자";
            case EquipmentSlotType.Body: return "갑옷";
            case EquipmentSlotType.Gloves: return "장갑";
            case EquipmentSlotType.Ring: return "장신구";
            default: return "";
        }
    }
}
