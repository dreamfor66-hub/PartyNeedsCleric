using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatUI : MonoBehaviour
{
    [Header("Refs")]
    public GameObject root;
    public Image icon;
    public TextMeshProUGUI valueText;

    // EquipmentPopup: 기존과 완전 동일 동작
    public void Apply(EquipmentStatType type, int value, Equipment compareEq, bool isComparePopup)
    {
        if (value == 0)
        {
            if (root != null) root.SetActive(false);
            return;
        }

        if (root != null) root.SetActive(true);

        var gv = GameVariables.Instance;

        if (icon != null)
            icon.sprite = gv != null ? gv.GetStatIcon(type) : null;

        if (valueText == null) return;

        // Compare팝업(장착중 장비)은 항상 흰색/비교표시 없음
        if (isComparePopup || compareEq == null || compareEq.Data == null)
        {
            if (gv != null) valueText.color = gv.equipmentStatBaseColor;
            valueText.text = value.ToString();
            return;
        }

        if (!TryGetCompareValue(compareEq, type, out int compareValue))
        {
            if (gv != null) valueText.color = gv.equipmentStatBaseColor;
            valueText.text = value.ToString();
            return;
        }

        int diff = value - compareValue;

        if (diff == 0)
        {
            if (gv != null) valueText.color = gv.equipmentStatBaseColor;
            valueText.text = value.ToString();
            return;
        }

        if (diff > 0)
        {
            if (gv != null) valueText.color = gv.equipmentStatHigherValueColor;
            string hex = (gv != null) ? ColorToHex(gv.equipmentStatIncreaseDeltaColor) : "FF0000";
            valueText.text = $"{value}(<color=#{hex}>＋{diff}</color>)";
            return;
        }
        else
        {
            if (gv != null) valueText.color = gv.equipmentStatLowerValueColor;
            string hex = (gv != null) ? ColorToHex(gv.equipmentStatDecreaseDeltaColor) : "0000FF";
            valueText.text = $"{value}(<color=#{hex}>－{Mathf.Abs(diff)}</color>)";
            return;
        }
    }

    // CharacterPopup 등: 비교 없이 단순 표기
    public void Apply(EquipmentStatType type, int value)
    {
        if (value == 0)
        {
            if (root != null) root.SetActive(false);
            return;
        }

        if (root != null) root.SetActive(true);

        var gv = GameVariables.Instance;

        if (icon != null)
            icon.sprite = gv != null ? gv.GetStatIcon(type) : null;

        if (valueText != null)
        {
            if (gv != null) valueText.color = gv.equipmentStatBaseColor;
            valueText.text = value.ToString();
        }
    }

    public void Apply(EquipmentStatType type, float value)
    {
        if (Mathf.Approximately(value, 0f))
        {
            if (root != null) root.SetActive(false);
            return;
        }

        if (root != null) root.SetActive(true);

        var gv = GameVariables.Instance;

        if (icon != null)
            icon.sprite = gv != null ? gv.GetStatIcon(type) : null;

        if (valueText != null)
        {
            if (gv != null) valueText.color = gv.equipmentStatBaseColor;

            float r = Mathf.Round(value);
            if (Mathf.Abs(value - r) < 0.0001f) valueText.text = ((int)r).ToString();
            else valueText.text = value.ToString("0.##");
        }
    }

    private bool TryGetCompareValue(Equipment compareEq, EquipmentStatType type, out int value)
    {
        value = 0;
        if (compareEq == null || compareEq.Data == null) return false;

        bool has = false;

        int c1 = Mathf.RoundToInt(compareEq.rolledStat1);
        int c2 = Mathf.RoundToInt(compareEq.rolledStat2);

        if (compareEq.Data.statType1 == type && c1 != 0)
        {
            value += c1;
            has = true;
        }

        if (compareEq.Data.statType2 == type && c2 != 0)
        {
            value += c2;
            has = true;
        }

        return has;
    }

    private static string ColorToHex(Color c)
    {
        Color32 cc = c;
        return $"{cc.r:X2}{cc.g:X2}{cc.b:X2}{cc.a:X2}";
    }
}
