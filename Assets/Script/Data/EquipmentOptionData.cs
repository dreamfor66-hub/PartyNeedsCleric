using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Outgame/Equipment/EquipmentOption")]
[Serializable]
public class EquipmentOptionData : ScriptableObject
{
    [ReadOnly]
    public string id;
    [TextArea]
    public string desc;

    public EquipmentOptionType optionType = EquipmentOptionType.None;

    [Header("Payload")]
    [ShowIf("@optionType == EquipmentOptionType.FlatStat")]
    public EquipmentStatType statType;

    [ShowIf("@optionType == EquipmentOptionType.SkillTagCooldownHaste")]
    public SkillTagFlags skillTags;

    [ShowIf("@optionType == EquipmentOptionType.DamageEnhance")]
    public HitFilter hitFlags;

    [ShowIf("@optionType == EquipmentOptionType.DamageEnhance")]
    public CharacterTagFlags targetTags;

    [Header("Roll")]
    public Vector2 valueRange = Vector2.zero;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // "Data_Equipment_{SlotType}_{Rarity}_name" -> id: "Equipment_{SlotType}_name"
        if (!string.IsNullOrEmpty(name) && name.StartsWith("EquipmentOption_"))
        {
            var parts = name.Split('_'); // Data / Equipment / SlotType / Rarity / name...
            if (parts.Length >= 3)
            {
                string colorPart = parts[1];
                string namePart = string.Join("_", parts, 2, parts.Length - 2);
                id = $"EquipmentOption_{colorPart}_{namePart}";
                return;
            }
        }

        // fallback: ±×´ë·Î
        id = name;
    }
#endif
}
