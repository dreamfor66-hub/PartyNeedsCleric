using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum EquipmentRarity
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4,
    Unique = 99
}

public enum EquipmentSlotType
{
    Weapon = 0,
    Head = 1,
    Body = 2,
    Gloves = 3,
    Ring = 4
}

public enum EquipmentStatType
{
    Attack = 0,            // 공격력
    Resilience = 1,        // 회복력/회복계열(이름 고정: Resilience)
    BaseManaRegen = 2,     // 기본 마나 재생속도
    CastHaste = 3,         // 캐스팅 가속 % (30 => 30% 더 빠름)
    CooldownHaste = 4,     // 쿨타임 가속 % (30 => 30% 더 빠름)
    Health = 5,            // 체력
    BaseMoveSpeed = 6,     // 기본 이동 속도
    Mp = 7,                 // 마나통
    
    BulletCooldown = 8,    // (캐릭터 UI용) 공격 쿨타임
    RangeRadius = 9        // (캐릭터 UI용) 사거리
}

[CreateAssetMenu(menuName = "Outgame/Equipment/EquipmentData")]
public class EquipmentData : ScriptableObject
{
    [Header("Base")]
    [ReadOnly]
    public string id;
    [PreviewField]
    public Sprite sprite;
    public string equipmentName;
    [TextArea] public string desc;
    public EquipmentRarity rarity;
    public EquipmentSlotType slotType;

    [Header("Ingame Passive Buffs")]
    public List<BuffData> buffs = new List<BuffData>();

    [Header("Stats (2 pairs)")]
    public EquipmentStatType statType1;

    // ★ float -> Vector2 (min~max)
    public Vector2 statValueRange1;

    public EquipmentStatType statType2;

    // ★ float -> Vector2 (min~max)
    public Vector2 statValueRange2;

    [Header("Skill Replace")]
    public List<EquipmentSkillReplace> skillReplaces = new List<EquipmentSkillReplace>();

    [Header("Random Options Roll (0 ~ Max)")]
    [Min(0)] public int RedMaxCount = 0;
    [Min(0)] public int BlueMaxCount = 0;
    [Min(0)] public int GreenMaxCount = 0;
    [Min(0)] public int YellowMaxCount = 0;


#if UNITY_EDITOR
    private void OnValidate()
    {
        // "Data_Equipment_{SlotType}_{Rarity}_name" -> id: "Equipment_{SlotType}_name"
        if (!string.IsNullOrEmpty(name) && (name.StartsWith("Data_Equipment_") || name.StartsWith("Data_CompanionEquipment")))
        {
            var parts = name.Split('_'); // Data / Equipment / SlotType / Rarity / name...
            if (parts.Length >= 5)
            {
                string titlePart = parts[1];
                string slotTypePart = parts[2];
                string namePart = string.Join("_", parts, 4, parts.Length - 4);
                id = $"{titlePart}_{slotTypePart}_{namePart}";
                return;
            }
        }

        // fallback: 그대로
        id = name;
    }
#endif
}

[Serializable]
public class EquipmentSkillReplace
{
    public SkillData from;
    public SkillData to;
}
