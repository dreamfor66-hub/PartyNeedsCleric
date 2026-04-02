using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/GameVariables")]
public class GameVariables : ScriptableObject
{
    static GameVariables _instance;
    public static GameVariables Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<GameVariables>("GameVariables");
            return _instance;
        }
    }

    [Title("Character")]
    [Header("AI")]
    public float MoveAngleSpeed = 50f;

    [Header("Search상태")]
    public float searchTime = 0.5f;

    [Header("Knockback상태")]
    public float knockbackDuration = 0.3f;

    [Header("Attack모션")]
    public float attackAnimTime = 0.2f;

    [Header("충돌 관련")]
    public float wallCollisionMinSpeed = 100f;

    [Title("UI")]
    [Header("HPBar Gradient")]
    public Gradient hpGradient;

    [Header("HPBar Delay")]
    public float delayHold = 0.5f;
    public float delaySpeed = 0.5f;

    [Title("Equipment UI")]
    [Header("Rarity Colors (Frame/Name)")]
    public Color equipmentCommon = new Color(0.75f, 0.75f, 0.75f, 1f);
    public Color equipmentUncommon = new Color(0.2f, 0.9f, 0.3f, 1f);
    public Color equipmentRare = new Color(0.3f, 0.55f, 1f, 1f);
    public Color equipmentEpic = new Color(0.75f, 0.35f, 1f, 1f);
    public Color equipmentLegendary = new Color(1f, 0.65f, 0.1f, 1f);
    public Color equipmentUnique = new Color(1f, 0.2f, 0.2f, 1f);

    [Title("Outgame UI")]
    [Header("Equipment Stat Compare Colors")]
    public Color equipmentStatBaseColor = Color.white;                 // 장착중 팝업(Compare) 기본값
    public Color equipmentStatHigherValueColor = new Color(0.2f, 1f, 0.2f); // 선택 장비 값이 더 높을 때(초록)
    public Color equipmentStatLowerValueColor = new Color(1f, 0.2f, 0.2f);  // 선택 장비 값이 더 낮을 때(빨강)
    public Color equipmentStatIncreaseDeltaColor = new Color(1f, 0.2f, 0.2f); // ▲(빨강)
    public Color equipmentStatDecreaseDeltaColor = new Color(0.2f, 0.4f, 1f); // ▼(파랑)

    [Header("Outgame - Stat Icons")]
    public StatIcons statIcons;

    [Title("Outgame")]
    [Header("Equipment Option Pool")]
    public EquipmentDataTable equipmentTable;
    public Color equipmentOptionColorRed;
    public EquipmentOptionPool equipmentOptionPoolRed;
    public Color equipmentOptionColorBlue;
    public EquipmentOptionPool equipmentOptionPoolBlue;
    public Color equipmentOptionColorGreen;
    public EquipmentOptionPool equipmentOptionPoolGreen;
    public Color equipmentOptionColorYellow;
    public EquipmentOptionPool equipmentOptionPoolYellow;
    public Color GetEquipmentRarityColor(EquipmentRarity r)
    {
        switch (r)
        {
            case EquipmentRarity.Uncommon: return equipmentUncommon;
            case EquipmentRarity.Rare: return equipmentRare;
            case EquipmentRarity.Epic: return equipmentEpic;
            case EquipmentRarity.Legendary: return equipmentLegendary;
            case EquipmentRarity.Unique: return equipmentUnique;
            default: return equipmentCommon;
        }
    }

    public Color GetEquipmentFrameColor(EquipmentRarity r) => GetEquipmentRarityColor(r);
    public Color GetEquipmentNameColor(EquipmentRarity r) => GetEquipmentRarityColor(r);

    public Sprite GetStatIcon(EquipmentStatType type)
    {
        if (statIcons == null) return null;
        return statIcons.GetIcon(type);
    }
}
