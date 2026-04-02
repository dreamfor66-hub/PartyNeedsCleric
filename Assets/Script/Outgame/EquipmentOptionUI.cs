using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentOptionUI : MonoBehaviour
{
    [Header("Refs")]
    public Image icon;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI valueText;

    public void Set(string desc, string value, Color iconColor)
    {
        descText.text = desc;
        valueText.text = value;

        if (icon != null)
        {
            icon.color = iconColor;
        }
    }
}
