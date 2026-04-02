using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
[System.Serializable]
public class BuffVfxEntry
{
    [ValueDropdown(nameof(GetVfxTriggers))]      // TriggerVfx만 노출
    public BuffEffect trigger;

    public VfxObject vfxPrefab;

    private static IEnumerable<ValueDropdownItem<BuffEffect>> GetVfxTriggers()
    {
        yield return new ValueDropdownItem<BuffEffect>("TriggerVfx1", BuffEffect.TriggerVfx1);
        yield return new ValueDropdownItem<BuffEffect>("TriggerVfx2", BuffEffect.TriggerVfx2);
        yield return new ValueDropdownItem<BuffEffect>("TriggerVfx3", BuffEffect.TriggerVfx3);
    }
}
[System.Serializable]
public struct BuffFormulaEntry
{
    [HorizontalGroup("Row"), HideLabel]
    public BuffFormulaType type;

    // ★ entity 이름 substring
    [HorizontalGroup("Row")]
    [ShowIf("@type == BuffFormulaType.EntityCount")]
    [HideLabel, SuffixLabel(" (Entity 이름)", Overlay = true)]
    public string entityName;

    [HorizontalGroup("Row")]
    [HideLabel, SuffixLabel(" (스택당 수치 / 제한값 등)", Overlay = true)]
    public float value;
}
[CreateAssetMenu(menuName = "Game/BuffData")]
public class BuffData : ScriptableObject
{
    [PropertyOrder(-2), ReadOnly]
    public string buffId = "Buff_Id";

    [PropertyOrder(-1)]
    [FoldoutGroup("Visual"), LabelText("Use Visual")]
    public bool useVisual = false;

    [PropertyOrder(-1)]
    [FoldoutGroup("Visual"), ShowIf("useVisual"), PreviewField(64)]
    public Sprite icon;

    public enum BuffVisualType { Buff, Debuff }

    [PropertyOrder(-1)]
    [FoldoutGroup("Visual"), ShowIf("useVisual"), EnumToggleButtons, LabelText("Type")]
    public BuffVisualType visualType;

    [PropertyOrder(-1)]
    [FoldoutGroup("Visual"), ShowIf("useVisual"), LabelText("Name")]
    public string buffName;
    [PropertyOrder(-1)]
    [FoldoutGroup("Visual"), ShowIf("useVisual"), LabelText("Description"), TextArea(2, 4)]
    public string buffDesc;

    [PropertyOrder(-1)]
    [Header("Tags")]
    public List<BuffTag> tags = new();

    [PropertyOrder(-1)]
    public List<BuffAbility> abilities = new();

    [Header("Remove / Duplicate")]
    public BuffRemoveCondition removeCondition = BuffRemoveCondition.Permanent;

    [ShowIf(nameof(IsDuration))] public float duration = 5f; // sec
    private bool IsDuration() => removeCondition == BuffRemoveCondition.Duration;

    [ShowIf(nameof(IsTriggerCount))] public int triggerCount = 1;
    private bool IsTriggerCount() => removeCondition == BuffRemoveCondition.TriggerCount;

    public BuffDuplicatePolicy duplicatePolicy = BuffDuplicatePolicy.Separate;
    [ShowIf(nameof(UseStack))] public int maxStacks = 99;
    private bool UseStack() => duplicatePolicy == BuffDuplicatePolicy.Stack;

    [Header("VFX Settings")]
    public List<BuffVfxEntry> vfxList = new();

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 예: "Data_Buff_AAAAAA" → "Buff_AAAAAA"
        if (!string.IsNullOrEmpty(name) && name.StartsWith("Data_Buff_"))
            buffId = name.Replace("Data_", "");
        else
            buffId = name;
    }
#endif
}

[Serializable]
public class BuffAbility
{
    //트리거
    [Title("트리거")]
    [OnValueChanged(nameof(OnTriggerChanged))]
    public BuffTrigger trigger;
    [ShowIf(nameof(ShowTriggerCount))][MinValue(1)] public int triggerCount = 1;
    [ShowIf(nameof(ShowHitFilter))] public HitFilter requiredHitFilter = HitFilter.None;

    //컨디션
    [Title("조건")]
    [ValueDropdown(nameof(GetAllowedConditions))]
    public BuffCondition condition;
    [ShowIf(nameof(NeedsConditionValue))] public float conditionValue = 0f;
    [ShowIf(nameof(NeedsConditionBuffTag))] public BuffTag conditionBuffTag;
    [ShowIf(nameof(NeedsConditionNegative))]  public bool conditionNegative = false;

    //타겟
    [Title("타겟")]
    [ValueDropdown(nameof(GetAllowedTargets))]
    [OnValueChanged(nameof(OnTargetChanged))]
    public BuffTarget target;

    // Target Range
    [ShowIf(nameof(ShowUseRangeToggle))]
    public bool useRange = false;

    [ShowIf(nameof(ShowTargetRange))]
    [LabelText("Range")]
    public float targetRange = 0f;

    //이펙트
    [Title("효과")]
    [ValueDropdown(nameof(GetAllowedEffects))]
    public BuffEffect effect;
    [ShowIf(nameof(NeedsEffectBuffTag))] public BuffTag effectBuffTag;
    [ShowIf(nameof(NeedsEffectValue))] public float effectValue = 0f;
    [ShowIf(nameof(NeedsEffectCount))] public int effectCount = 1;
    [ShowIf(nameof(ShowFormulaList))]
    [ListDrawerSettings(
        ShowPaging = false,
        DraggableItems = false,
        HideAddButton = true,
        HideRemoveButton = false
    )]
    [LabelText("계산식")]
    public List<BuffFormulaEntry> formulas = new();

    // 기존 NeedsEffectValue + HideIf(IsFormulaListEmpty) 조합 제거

    private bool ShowFormulaList()
        => (NeedsEffectValue() || NeedsEffectCount()) && formulas != null && formulas.Count > 0;

    [GUIColor(0.8f, 0.95f, 1f)]
    [Button("＋ 계산식 추가", buttonSize: 200 , Stretch = false, ButtonHeight = 20, ButtonAlignment = 1)]
    [ShowIf(nameof(ShowFormula))]
    private void AddFormula()
    {
        if (formulas == null)
            formulas = new List<BuffFormulaEntry>();

        formulas.Add(new BuffFormulaEntry { type = BuffFormulaType.StackPer, value = 1f });
    }
    private bool ShowFormula()
    => (NeedsEffectValue() || NeedsEffectCount());

    [ShowIf(nameof(NeedsCharacterField))] public CharacterData characterData;
    [ShowIf(nameof(NeedsBulletField))] public BulletData bulletData;
    [ShowIf(nameof(NeedsBuffField))] public BuffData buffToAdd;
    [ShowIf(nameof(NeedsCharacterField))] public Vector2 offset;
    [ShowIf(nameof(NeedsCharacterField))] public float range;
    [ShowIf(nameof(NeedsBuffIdField))] public string removeBuffId;
    [ShowIf(nameof(NeedsBulletField))] public SpawnDirection direction;
    [ShowIf(nameof(NeedsBulletField))] public Vector2 angleOffsetRange = Vector2.zero;
    [ShowIf(nameof(NeedsTeamSelector))] public BuffTeamSelector teamSelector = BuffTeamSelector.Target;

    // ---------- Odin helpers ----------
    private bool ShowTriggerCount()
        => trigger == BuffTrigger.EverySecond
        || trigger == BuffTrigger.OnEachStack
        || trigger == BuffTrigger.EveryMove;

    private bool ShowHitFilter()
    => trigger == BuffTrigger.OwnerHit || trigger == BuffTrigger.OwnerHurt;

    private bool NeedsEffectValue()
        => effect is BuffEffect.GainAtk
              or BuffEffect.GainResilience
              or BuffEffect.GainSpeedUp
              or BuffEffect.GainSlow
              or BuffEffect.Heal
              or BuffEffect.InstantDamage
              or BuffEffect.HitModifyDamage
              or BuffEffect.GainSkillCastingHaste
              or BuffEffect.GainSkillCooldownHaste;
    private bool NeedsEffectCount()
        => effect is BuffEffect.SpawnCharacter
              or BuffEffect.AddBuff
              or BuffEffect.RemoveBuffByTag;
    private bool NeedsEffectBuffTag()
    => effect == BuffEffect.RemoveBuffByTag;
    private bool NeedsConditionValue() =>
        condition == BuffCondition.Chance;
    private bool NeedsConditionNegative() =>
        condition != BuffCondition.Always;

    private bool NeedsConditionBuffTag()
        => condition is BuffCondition.OwnerHasBuff
        or BuffCondition.TargetHasBuff
           && (target == BuffTarget.Owner
            || target == BuffTarget.HitTarget
            || target == BuffTarget.Attacker
            || target == BuffTarget.NearestEnemy
            || target == BuffTarget.NearestAlly);

    private bool NeedsCharacterField() => effect == BuffEffect.SpawnCharacter;
    private bool NeedsBulletField() => effect == BuffEffect.SpawnBullet;
    private bool NeedsBuffField() => effect == BuffEffect.AddBuff;
    private bool NeedsBuffIdField() => effect == BuffEffect.RemoveBuffById;
    private bool NeedsTeamSelector() => effect == BuffEffect.SpawnBullet || effect == BuffEffect.SpawnCharacter;

    private bool TargetSupportsRange() =>
        target is BuffTarget.NearestEnemy
        or BuffTarget.NearestAlly
        or BuffTarget.AllAlly
        or BuffTarget.AllEnemy;

    private bool ShowUseRangeToggle() => TargetSupportsRange();

    private bool ShowTargetRange() => TargetSupportsRange() && useRange;

    // ---------- Enum 필터링 ----------
    private IEnumerable<ValueDropdownItem<BuffTarget>> GetAllowedTargets()
    {
        // 공통 캐릭터 타겟
        var common = new[] { BuffTarget.Owner, BuffTarget.ThisBuff, BuffTarget.NearestEnemy, BuffTarget.NearestAlly };

        switch (trigger)
        {
            case BuffTrigger.OnBuffStart:
            case BuffTrigger.OnBuffEnd:
            case BuffTrigger.OnEachStack:
            case BuffTrigger.EverySecond:
            case BuffTrigger.EveryMove:
            case BuffTrigger.OwnerDie:
                return new[]
{
                    new ValueDropdownItem<BuffTarget>("Owner", BuffTarget.Owner),
                    new ValueDropdownItem<BuffTarget>("ThisBuff", BuffTarget.ThisBuff),
                    new ValueDropdownItem<BuffTarget>("NearestEnemy", BuffTarget.NearestEnemy),
                    new ValueDropdownItem<BuffTarget>("NearestAlly", BuffTarget.NearestAlly),
                    new ValueDropdownItem<BuffTarget>("AllAlly", BuffTarget.AllAlly),
                    new ValueDropdownItem<BuffTarget>("AllEnemy", BuffTarget.AllEnemy),
                };
            case BuffTrigger.OwnerHit:
            case BuffTrigger.OwnerHurt:
                return new[]
                {
                    new ValueDropdownItem<BuffTarget>("Owner", BuffTarget.Owner),
                    new ValueDropdownItem<BuffTarget>("HitTarget", BuffTarget.HitTarget),
                    new ValueDropdownItem<BuffTarget>("HitAttacker", BuffTarget.Attacker),
                    new ValueDropdownItem<BuffTarget>("TriggeredHit", BuffTarget.TriggeredHit),
                    new ValueDropdownItem<BuffTarget>("ThisBuff", BuffTarget.ThisBuff),
                    new ValueDropdownItem<BuffTarget>("NearestEnemy", BuffTarget.NearestEnemy),
                    new ValueDropdownItem<BuffTarget>("NearestAlly", BuffTarget.NearestAlly),

                };

            case BuffTrigger.OwnerSpawnBullet:
                return new[]
                {
                    new ValueDropdownItem<BuffTarget>("Owner",           BuffTarget.Owner),
                    new ValueDropdownItem<BuffTarget>("ThisBuff",        BuffTarget.ThisBuff),
                    new ValueDropdownItem<BuffTarget>("NearestEnemy",    BuffTarget.NearestEnemy),
                    new ValueDropdownItem<BuffTarget>("NearestAlly",        BuffTarget.NearestAlly),
                    new ValueDropdownItem<BuffTarget>("AllAlly", BuffTarget.AllAlly),
                    new ValueDropdownItem<BuffTarget>("AllEnemy", BuffTarget.AllEnemy),
                };

            default:
                return common.Select(t => new ValueDropdownItem<BuffTarget>(t.ToString(), t));
        }
    }
    private IEnumerable<ValueDropdownItem<BuffCondition>> GetAllowedConditions()
    {
        yield return new ValueDropdownItem<BuffCondition>("Always", BuffCondition.Always);
        yield return new ValueDropdownItem<BuffCondition>("Chance", BuffCondition.Chance);
        yield return new ValueDropdownItem<BuffCondition>("OwnerHasBuff", BuffCondition.OwnerHasBuff);

        // TargetHasBuff는 target이 Character 계열일 때만 허용
        if (target == BuffTarget.Owner
            || target == BuffTarget.NearestEnemy
            || target == BuffTarget.NearestAlly
            || target == BuffTarget.Attacker
            || target == BuffTarget.HitTarget)
        {
            yield return new ValueDropdownItem<BuffCondition>("TargetHasBuff", BuffCondition.TargetHasBuff);
        }
        if (trigger == BuffTrigger.OwnerUseSkill)
        {
            yield return new ValueDropdownItem<BuffCondition>("SkillUseCasting", BuffCondition.SkillUseCasting);
        }
    }
    private IEnumerable<ValueDropdownItem<BuffEffect>> GetAllowedEffects()
    {
        // 타겟별 허용 이펙트
        switch (target)
        {
            case BuffTarget.TriggeredHit:
                return new[]
                {
                new ValueDropdownItem<BuffEffect>("HitModifyDamage", BuffEffect.HitModifyDamage),
                new ValueDropdownItem<BuffEffect>("IgnoreHit",       BuffEffect.IgnoreHit),
            };

            case BuffTarget.ThisBuff:
                // 버프 자신을 대상으로 의미 있는 작업만 허용
                return new[]
                {
                new ValueDropdownItem<BuffEffect>("RemoveBuff", BuffEffect.RemoveBuff),
                new ValueDropdownItem<BuffEffect>("TriggerVfx1", BuffEffect.TriggerVfx1),
                new ValueDropdownItem<BuffEffect>("TriggerVfx2", BuffEffect.TriggerVfx2),
                new ValueDropdownItem<BuffEffect>("TriggerVfx3", BuffEffect.TriggerVfx3),
            };

            case BuffTarget.Owner:
            case BuffTarget.Attacker:
            case BuffTarget.HitTarget:
            case BuffTarget.NearestEnemy:
            case BuffTarget.NearestAlly:
            case BuffTarget.AllAlly:
            case BuffTarget.AllEnemy:
            default:
                return new[]
                {
                new ValueDropdownItem<BuffEffect>("SpawnBullet",      BuffEffect.SpawnBullet),
                new ValueDropdownItem<BuffEffect>("SpawnCharacter",   BuffEffect.SpawnCharacter),
                new ValueDropdownItem<BuffEffect>("GainAtk",            BuffEffect.GainAtk),
                new ValueDropdownItem<BuffEffect>("GainResilience", BuffEffect.GainResilience),
                new ValueDropdownItem<BuffEffect>("GainSpeedUp", BuffEffect.GainSpeedUp),
                new ValueDropdownItem<BuffEffect>("GainSlow",       BuffEffect.GainSlow),
                new ValueDropdownItem<BuffEffect>("GainSkillCastingHaste",       BuffEffect.GainSkillCastingHaste),
                new ValueDropdownItem<BuffEffect>("GainSkillCooldownHaste",       BuffEffect.GainSkillCooldownHaste),
                new ValueDropdownItem<BuffEffect>("Heal",             BuffEffect.Heal),
                new ValueDropdownItem<BuffEffect>("InstantDamage",    BuffEffect.InstantDamage),
                new ValueDropdownItem<BuffEffect>("InstantKill",    BuffEffect.InstantKill),
                new ValueDropdownItem<BuffEffect>("AddBuff",        BuffEffect.AddBuff),
                new ValueDropdownItem<BuffEffect>("RemoveBuffById",   BuffEffect.RemoveBuffById),
            };
        }
    }

    // ---------- 자동 보정 ----------
    private void OnTriggerChanged()
    {
        // 트리거 변경 시 허용되지 않는 타겟 자동 보정
        var first = GetAllowedTargets().FirstOrDefault();
        if (!GetAllowedTargets().Any(i => i.Value.Equals(target)))
            target = first.Value;
        // 타겟 변경에 연쇄 보정
        OnTargetChanged();
    }

    private void OnTargetChanged()
    {
        // 타겟 변경 시 허용되지 않는 이펙트 자동 보정
        var first = GetAllowedEffects().FirstOrDefault();
        if (!GetAllowedEffects().Any(i => i.Value.Equals(effect)))
            effect = first.Value;

        if (!TargetSupportsRange())
        {
            useRange = false;
            targetRange = 0f;
        }

        // ThisBuff 대상으로 RemoveBuffById 선택 시 기본값 보정
        if (target == BuffTarget.ThisBuff && effect == BuffEffect.RemoveBuffById && string.IsNullOrEmpty(removeBuffId))
            removeBuffId = buffToAdd != null ? buffToAdd.buffId : ""; // 필요시 외부에서 세팅
    }
}
