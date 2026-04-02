using Unity.VisualScripting;
using UnityEngine;

public class PlayerCommander : CharacterBehaviour
{
    public SkillData[] skills = new SkillData[6];
    public float[] cooldowns = new float[6];
    CommanderUI ui;

    public bool IsCasting { get; private set; }
    public float CastingTimer { get; private set; }
    public SkillData CastingSkill { get; private set; }
    public CharacterBehaviour CastingTarget { get; private set; }
    public Vector3 CastingPoint { get; private set; }


    public override void Awake()
    {
        //아무것도 안함. 실행 방지용 오버라이드...
    }

    public override void Init(CharacterData data, TeamType team)
    {
        this.data = data;
        this.team = team;

        InitBaseStats();     // HP/Mana/Atk 등 공통
        InitSystems();       // BuffManager
        InitSearchState();

        ui = CommanderUISpawner.Instance.Spawn(this);

        skills = GetSkillObserver().Skills;
        cooldowns = GetSkillObserver().Cooldowns;
        ui?.HideCastingBar();

        EntityContainer.Instance.SetCommander(this);

        //개발용
        foreach (var entry in data.StartBuffs)
        {
            for (int i = 0; i < entry.count; i++)
                Buffs.AddBuff(entry.buff);
        }
        if (data != null && data.StartSkills != null)
        {
            var skillsArr = new SkillData[CharacterSkillObserver.MaxSkills];
            for (int i = 0; i < data.StartSkills.Count && i < skillsArr.Length; i++)
                skillsArr[i] = data.StartSkills[i];

            GetSkillObserver().SetSkills(skillsArr);
        }

        GetSkillObserver().ApplyPassiveOnce();
    }
    public override void FixedUpdate()
    {
        UpdateCasting();

        Buffs?.Tick(Time.deltaTime);
        TickManaRegen(Time.deltaTime);
        GetSkillObserver().Tick(Time.deltaTime);
    }

    public override void ApplyDamage(CharacterBehaviour attacker, int finalDamage) { }
    public override void Heal(int amount) {}

    public override void UseMana(int cost)
    {
        base.UseMana(cost);
        ui?.UpdateMana(GetCurrentMana(), GetMaxMana());
    }

    public override void RestoreMana(int amount)
    {
        base.RestoreMana(amount);
        ui?.UpdateMana(GetCurrentMana(), GetMaxMana());
    }

    public bool CanUseSkill(int index)
    {
        return GetSkillObserver().CanUseSkill(index);
    }

    public void TriggerSkill(int index)
    {
        GetSkillObserver().TriggerSkill(index);
    }

    public float GetCooldownPercent(int index)
    {
        return GetSkillObserver().GetCooldownPercent(index);
    }

    public void StartCasting(SkillData skill, CharacterBehaviour target, Vector3 point)
    {
        IsCasting = true;
        CastingSkill = skill;
        CastingTarget = target;
        CastingPoint = point;
        CastingTimer = 0f;
        ui?.ShowCastingBar();  // 캐스팅바 활성화
        SkillInputController.Instance.StartCastingMode(skill, target, point);
    }

    public void CancelCasting()
    {
        if (!IsCasting) return;
        IsCasting = false;
        CastingSkill = null;
        CastingTarget = null;
        ui?.HideCastingBar();  // 캐스팅바 숨김
        SkillInputController.Instance.EndCastingMode();
    }

    public void UpdateCasting()
    {
        if (!IsCasting) return;

        CastingTimer += Time.deltaTime * (GetSkillCastHaste() /100);
        float t = Mathf.Clamp01(CastingTimer / CastingSkill.castingTime);
        ui?.UpdateCastingBar(t);

        // 완료
        if (CastingTimer >= CastingSkill.castingTime)
        {
            FinishCasting();
        }
    }

    void FinishCasting()
    {
        if (!IsCasting) return;

        ui?.HideCastingBar();
        var skill = CastingSkill;

        IsCasting = false;
        CastingSkill = null;

        // 대상 스킬
        if (skill.targetType == SkillTargetType.SingleCharacter)
        {
            if (CastingTarget != null)
                SkillCaster.Instance.CastSkill(this, skill, CastingTarget);
        }
        else
        {
            SkillCaster.Instance.CastSkill(this, skill, CastingPoint);
        }

        TriggerSkill(System.Array.IndexOf(skills, skill));
        SkillInputController.Instance.EndCastingMode();

    }
}
