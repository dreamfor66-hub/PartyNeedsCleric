using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum SkillTargetType
{
    SingleCharacter,
    Range,
    Self,
}

public enum SkillConditionType
{
    DistanceToEnemy,   // 가장 가까운 Enemy와의 거리
    OwnerHpPercent,    // Owner HP 퍼센트
}

public enum SkillEqualityType
{
    [LabelText("<")]Less,      // <
    [LabelText(">")]Greater,   // >
    [LabelText("=")]Equal,     // =
}

public enum SkillTeamType
{
    Any,
    PlayerTeam,
    EnemyTeam,
    JustPoint   // SingleCharacter일 땐 숨김
}
public enum SkillEffectType
{
    AddBuff,
    SpawnBullet,
    RemoveBuffByTag,
    SpawnCharacter,
    Heal,
    RestoreMana,
    DoAction,   // 추가
}

public enum SkillUseType
{
    Manual,
    Auto,
    Passive
}
public enum SkillFormulaType
{
    TargetHPPercent,       // 대상 최대 HP 비례
    TargetAttackPercent,   // 대상 공격력 비례
    OwnerMaxMPPercent,     // 스킬 사용자의 최대 MP(Mana) 비례
}

[System.Serializable]
public struct SkillFormulaEntry
{
    [HorizontalGroup("Row"), HideLabel]
    public SkillFormulaType type;

    [HorizontalGroup("Row")]
    [HideLabel, SuffixLabel(" (퍼센트 등)", Overlay = true)]
    public float value;
}
[System.Serializable]
public class SkillCondition
{
    [HorizontalGroup("Row", Width = 0.6f)]
    [HideLabel]
    public SkillConditionType conditionType;
    [HorizontalGroup("Row", Width = 0.1f)]
    [HideLabel]
    public SkillEqualityType equalityType;
    [HorizontalGroup("Row", Width = 0.3f)]
    [HideLabel]
    public float value;
}

[System.Serializable]
public class SkillEffectEntry
{
    [ValueDropdown("@$root.GetEffectTypeOptions()")]
    public SkillEffectType type;
    [ShowIf("@type == SkillEffectType.AddBuff")]
    public BuffData buff;
    [ShowIf("@type == SkillEffectType.SpawnBullet")]
    public BulletData bullet;

    [ShowIf("@type == SkillEffectType.SpawnBullet")]
    [LabelText("Use UI Start Position")]
    public bool useUiStartPosition = false;

    [ShowIf("@type == SkillEffectType.AddBuff || type == SkillEffectType.RemoveBuffByTag")]
    [Min(1)]
    public int buffCount = 1;
    // RemoveBuffByTag 전용 태그
    [ShowIf("@type == SkillEffectType.RemoveBuffByTag")]
    public BuffTag removeTag;

    // SpawnCharacter 전용
    [ShowIf("@type == SkillEffectType.SpawnCharacter")]
    public CharacterData character;

    [ShowIf("@type == SkillEffectType.DoAction")]
    public ActionData action;

    [ShowIf(nameof(NeedsEffectValue))]
    public float effectValue = 0f;

    [ShowIf(nameof(ShowFormulaList))]
    [ListDrawerSettings(ShowPaging = false, DraggableItems = false, HideAddButton = true, HideRemoveButton = false)]
    [LabelText("계산식")]
    public List<SkillFormulaEntry> formulas = new();

    [GUIColor(0.8f, 0.95f, 1f)]
    [Button("＋ 계산식 추가", buttonSize: 200, Stretch = false, ButtonHeight = 20, ButtonAlignment = 1)]
    [ShowIf(nameof(NeedsEffectValue))]
    private void AddFormula()
    {
        if (formulas == null)
            formulas = new List<SkillFormulaEntry>();

        formulas.Add(new SkillFormulaEntry { type = SkillFormulaType.TargetHPPercent, value = 10f });
    }

    private bool NeedsEffectValue()
        => type is SkillEffectType.Heal
        or SkillEffectType.RestoreMana;

    private bool ShowFormulaList()
        => NeedsEffectValue() && formulas != null && formulas.Count > 0;
}


[Flags]
public enum SkillTagFlags
{
    None = 0,

    Heal = 1 << 0,
    Buff = 1 << 1,
    Burn = 1 << 2,
    Lightning = 1 << 3,
    Disrupt = 1 << 4,
    Summon = 1 << 5,
    Casting = 1 << 6,
    Channeling = 1 << 7,
}

[CreateAssetMenu(menuName = "Game/SkillData")]
public class SkillData : ScriptableObject
{
    [PropertyOrder(-2), ReadOnly]
    public string skillId = "Skill_Id";
    public string skillName;
    [PreviewField(FilterMode = FilterMode.Point)]
    public Sprite icon;

    [ShowIf("@useType == SkillUseType.Auto")]
    [TextArea] public string skillScript;

    [Header("UseType")]
    public SkillUseType useType = SkillUseType.Manual;

    [Header("Tag")]
    public SkillTagFlags skillTags = SkillTagFlags.None;

    public int manaCost;
    public float cooldown;
    public float initialCooldown = 0f;

    [Header("Conditions")]
    [ShowIf("@useType == SkillUseType.Auto")]
    [LabelText("Conditions")]
    public List<SkillCondition> conditions = new();

    [ShowIf("@useType == SkillUseType.Manual")]
    public bool useCasting;

    [ShowIf("@useType == SkillUseType.Manual && useCasting")]
    [LabelText("Casting Time")]
    [Min(0.1f)]
    public float castingTime = 1f;
    [TextArea] public string description;

    [Header("Target")]
    [ValueDropdown(nameof(GetTargetOptions))]
    public SkillTargetType targetType;

    [ShowIf("@targetType == SkillTargetType.Range")]
    public float radius;

    [ShowIf("@targetType != SkillTargetType.Self")]
    [ValueDropdown(nameof(GetTeamOptions))]
    public SkillTeamType teamType;

    [Header("Effects")]
    public List<SkillEffectEntry> effects = new();

    [Header("Outgame")]
    public List<CharacterData> usableClasses = new List<CharacterData>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 예: "Data_Skill_AAAAAA" → "Skill_AAAAAA"
        if (!string.IsNullOrEmpty(name) && name.StartsWith("Data_Skill_"))
            skillId = name.Replace("Data_", "");
        else
            skillId = name;
    }
#endif
private IEnumerable<SkillTeamType> GetTeamOptions()
    {
        if (targetType == SkillTargetType.SingleCharacter)
            return new[] { SkillTeamType.Any, SkillTeamType.PlayerTeam, SkillTeamType.EnemyTeam };
        else
            return (SkillTeamType[])System.Enum.GetValues(typeof(SkillTeamType));
    }

    private IEnumerable<SkillTargetType> GetTargetOptions()
    {
        if (useType == SkillUseType.Manual)
            return new[] { SkillTargetType.SingleCharacter, SkillTargetType.Range, SkillTargetType.Self };

        return new[] { SkillTargetType.Self };
    }

    public IEnumerable<SkillEffectType> GetEffectTypeOptions()
    {
        if (useType == SkillUseType.Manual)
        {
            // Manual은 DoAction 숨김
            return new[]
            {
            SkillEffectType.AddBuff,
            SkillEffectType.SpawnBullet,
            SkillEffectType.RemoveBuffByTag,
            SkillEffectType.SpawnCharacter,
            SkillEffectType.Heal,
            SkillEffectType.RestoreMana,
        };
        }

        // Auto/Passive는 전부 노출(원하면 여기서도 제한 가능)
        return (SkillEffectType[])Enum.GetValues(typeof(SkillEffectType));
    }
}
