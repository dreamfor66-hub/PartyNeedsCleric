using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSkillObserver
{
    public const int MaxSkills = 5;

    private readonly CharacterBehaviour owner;

    private readonly SkillData[] skills = new SkillData[MaxSkills];
    private readonly float[] cooldowns = new float[MaxSkills];

    private bool passiveApplied = false;

    public SkillData[] Skills => skills;
    public float[] Cooldowns => cooldowns;

    public CharacterSkillObserver(CharacterBehaviour owner)
    {
        this.owner = owner;
    }

    public void SetSkills(SkillData[] arr)
    {
        Array.Clear(skills, 0, skills.Length);
        Array.Clear(cooldowns, 0, cooldowns.Length);
        passiveApplied = false;

        if (arr == null) return;

        int count = Mathf.Min(arr.Length, MaxSkills);
        for (int i = 0; i < count; i++)
        {
            skills[i] = arr[i];

            var s = skills[i];
            if (s == null) continue;

            float haste = owner.GetSkillCooldownHaste();
            float tagHaste = owner.SkillTagCooldownHasteSum(s.skillTags);
            float totalHaste = Mathf.Max(1f, haste + tagHaste);

            float duration = s.cooldown * 100f / totalHaste;

            float initBase = Mathf.Clamp(s.initialCooldown, 0f, s.cooldown);
            cooldowns[i] = initBase * 100f / totalHaste;

            if (cooldowns[i] > duration) cooldowns[i] = duration;
        }

        UpdateCharacterSkillCooldownUI();
    }



    public void ApplyPassiveOnce()
    {
        if (passiveApplied) return;
        passiveApplied = true;

        for (int i = 0; i < skills.Length; i++)
        {
            var s = skills[i];
            if (s == null) continue;
            if (s.useType != SkillUseType.Passive) continue;

            SkillCaster.Instance.CastSkill(owner, s, owner);
        }
    }

    public void Tick(float dt)
    {
        for (int i = 0; i < cooldowns.Length; i++)
        {
            if (cooldowns[i] > 0f)
            {
                cooldowns[i] -= dt;
                if (cooldowns[i] < 0f) cooldowns[i] = 0f;
            }
        }
        UpdateCharacterSkillCooldownUI();

        if (owner.state == CharacterState.Action) return;
        if (owner.state == CharacterState.Knockback) return;
        if (owner.state == CharacterState.Die) return;

        for (int i = 0; i < skills.Length; i++)
        {
            var s = skills[i];
            if (s == null) continue;

            if (s.useType != SkillUseType.Auto)
                continue;

            if (cooldowns[i] > 0f)
                continue;

            if (!owner.HasMana(s.manaCost))
                continue;

            if (!CheckSkillConditions(s))
                continue;


            owner.UseMana(s.manaCost);
            SkillCaster.Instance.CastSkill(owner, s, owner);

            cooldowns[i] = GetCooldownDuration(s);
            owner.Buffs?.NotifyOwnerUseSkill(s);
        }
        UpdateCharacterSkillCooldownUI();
    }

    public bool CanUseSkill(int index)
    {
        var s = skills[index];
        if (s == null) return false;
        if (cooldowns[index] > 0f) return false;
        if (!owner.HasMana(s.manaCost)) return false;
        if (!CheckSkillConditions(s)) return false;
        return true;
    }

    public void TriggerSkill(int index)
    {
        var s = skills[index];

        owner.UseMana(s.manaCost);
        cooldowns[index] = GetCooldownDuration(s);

        owner.Buffs?.NotifyOwnerUseSkill(s);

        UpdateCharacterSkillCooldownUI();
    }

    public float GetCooldownPercent(int index)
    {
        var s = skills[index];
        if (s == null) return 0f;

        float duration = GetCooldownDuration(s);
        return duration <= 0f ? 0f : cooldowns[index] / duration;
    }

    private float GetCooldownDuration(SkillData s)
    {
        float haste = owner.GetSkillCooldownHaste();
        float tagHaste = owner.SkillTagCooldownHasteSum(s.skillTags);
        float totalHaste = Mathf.Max(1f, haste + tagHaste);
        return s.cooldown * 100f / totalHaste;
    }

    // CharacterSkillObserver.cs (클래스 내부 아무 아래쪽에 추가)

    private bool CheckSkillConditions(SkillData s)
    {
        if (s.useType != SkillUseType.Auto) return true;
        if (s.conditions == null || s.conditions.Count == 0) return true;

        for (int i = 0; i < s.conditions.Count; i++)
        {
            var cond = s.conditions[i];

            switch (cond.conditionType)
            {
                case SkillConditionType.DistanceToEnemy:
                    {
                        var enemy = EntityContainer.Instance.GetNearestEnemy(owner);
                        if (enemy == null) return false;

                        float dist = Vector2.Distance(owner.transform.position, enemy.transform.position);
                        if (!Compare(dist, cond.equalityType, cond.value)) return false;
                        break;
                    }

                case SkillConditionType.OwnerHpPercent:
                    {
                        float hpPct = owner.GetCurrentHealth() * 100f / Mathf.Max(1, owner.GetMaxHealth());
                        if (!Compare(hpPct, cond.equalityType, cond.value)) return false;
                        break;
                    }
            }
        }

        return true;
    }

    private static bool Compare(float lhs, SkillEqualityType eq, float rhs)
    {
        switch (eq)
        {
            case SkillEqualityType.Less: return lhs < rhs;
            case SkillEqualityType.Greater: return lhs > rhs;
            case SkillEqualityType.Equal: return Mathf.Abs(lhs - rhs) <= 0.001f;
        }
        return false;
    }

    private void UpdateCharacterSkillCooldownUI()
    {
        int index = -1;

        for (int i = 0; i < skills.Length; i++)
        {
            var s = skills[i];
            if (s == null) continue;
            if (s.useType != SkillUseType.Auto) continue;

            index = i;
            break;
        }

        if (index < 0)
        {
            owner.OnCharacterSkillCooldownChanged(false, 0f);
            return;
        }

        float fillAmount;

        if (CanUseSkill(index))
        {
            fillAmount = 1f;
        }
        else
        {
            float remainPct = GetCooldownPercent(index); // 1=남음, 0=끝
            if (remainPct > 0f) fillAmount = 1f - remainPct;
            else fillAmount = 1f; // 쿨은 끝났는데 (마나/조건)으로 사용 불가
        }

        owner.OnCharacterSkillCooldownChanged(true, fillAmount);
    }


}
