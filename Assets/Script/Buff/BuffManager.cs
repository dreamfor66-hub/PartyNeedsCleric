using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class BuffManager
{
    private readonly CharacterBehaviour owner;
    private readonly List<BuffInstance> list = new();

    // 버프 지급 지연: 다음 프레임에 적용
    private readonly List<(BuffData data, int stacks)> pendingAddApply = new(); // 직전 프레임에 예약된 지급분(이번 Tick에서 적용)
    private readonly List<(BuffData data, int stacks)> pendingAddNext = new(); // 금프레임에 예약한 지급분(다음 Tick에서 적용)

    private float atkGain = 0f;
    private float resilienceGain = 0f;
    private float speedUpGain = 0f;
    private float slowGain = 0f;
    private float skillCastHaste = 0f;
    private float skillCooldownHaste = 0f;

    public float BuffAtkMultiplier => (1f + atkGain / 100f) /** (1f + atkGain2 / 100f)*/;
    public float BuffResilienceMultiplier => (1f + resilienceGain / 100f)/* * (1f + resilienceGain2 / 100f)*/;
    public float BuffHpMultiplier => (1f /*+ hpGain1 / 100f) * (1f + hpGain2 / 100f*/);
    public float BuffSpeedUpMultiplier => (1f + speedUpGain / 100f);
    public float BuffSlowMultiplier => Mathf.Max(0.01f, (1f - slowGain / 100f));
    public float BuffSkillCastMultiplier => (100f + skillCastHaste) / 100f; // ★ 추가
    public float BuffSkillCooldownMultiplier => (100f + skillCooldownHaste) / 100f; // ★ 추가
    public BuffManager(CharacterBehaviour owner) { this.owner = owner; }

    // ---------------- Stats ----------------
    public void AddStat(BuffEffect effect, float value)
    {
        switch (effect)
        {
            case BuffEffect.GainAtk:
                atkGain += value;
                break;

            case BuffEffect.GainResilience:
                resilienceGain += value;
                break;

            case BuffEffect.GainSpeedUp:
                speedUpGain += value;
                break;

            case BuffEffect.GainSlow:
                slowGain += value;
                break;

            case BuffEffect.GainSkillCastingHaste: 
                skillCastHaste += value;
                break;

            case BuffEffect.GainSkillCooldownHaste: 
                skillCooldownHaste += value;
                break;
        }
    }

    // ---------------- Tick ----------------
    public void Tick(float dt)
    {
        // 지속시간 처리
        for (int i = list.Count - 1; i >= 0; i--)
            if (list[i].Tick(dt)) list.RemoveAt(i);

        // ★ 이전 프레임에 예약된 지급분을 "이번 프레임 끝에" 적용
        if (pendingAddApply.Count > 0)
        {
            foreach (var (data, stacks) in pendingAddApply)
                AddBuffInternal(data, stacks);
            pendingAddApply.Clear();
        }

        // ★ 금프레임에 예약된 지급분은 "다음 프레임"에 적용되도록 넘김
        if (pendingAddNext.Count > 0)
        {
            pendingAddApply.AddRange(pendingAddNext);
            pendingAddNext.Clear();
        }
    }

    // ---------------- Add / Remove ----------------
    // 외부에서는 AddBuff 호출 시 "지급 예약"만 수행한다.
    public void AddBuff(BuffData data, int stacks = 1)
    {
        pendingAddNext.Add((data, stacks)); // 다음 프레임에 실제 적용
    }

    // 내부 실제 적용 로직
    // BuffManager.cs
    private void AddBuffInternal(BuffData data, int stacks)
    {
        switch (data.duplicatePolicy)
        {
            case BuffDuplicatePolicy.Separate:
                {
                    var inst = new BuffInstance(data, owner, stacks);
                    list.Add(inst);

                    InternalDispatch(BuffTrigger.OnBuffStart, new BuffEventContext { Owner = owner, ThisBuff = inst });

                    if (data.removeCondition == BuffRemoveCondition.Instant)
                        RemoveInstance(inst);
                    break;
                }

            case BuffDuplicatePolicy.Stack:
                {
                    var inst = list.Find(b => b.Data != null && b.Data.buffId == data.buffId);
                    if (inst == null)
                    {
                        inst = new BuffInstance(data, owner, stacks);
                        list.Add(inst);

                        InternalDispatch(BuffTrigger.OnBuffStart, new BuffEventContext { Owner = owner, ThisBuff = inst });
                    }
                    else
                    {
                        inst.AddStack(stacks);
                    }

                    if (data.removeCondition == BuffRemoveCondition.Instant)
                        RemoveInstance(inst);
                    break;
                }

            case BuffDuplicatePolicy.Refresh:
                {
                    var exist = list.Find(b => b.Data != null && b.Data.buffId == data.buffId);
                    if (exist != null) RemoveInstance(exist);

                    var inst = new BuffInstance(data, owner, 1);
                    list.Add(inst);

                    InternalDispatch(BuffTrigger.OnBuffStart, new BuffEventContext { Owner = owner, ThisBuff = inst });

                    if (data.removeCondition == BuffRemoveCondition.Instant)
                        RemoveInstance(inst);
                    break;
                }

            case BuffDuplicatePolicy.IgnoreNew:
                {
                    var exist = list.Find(b => b.Data != null && b.Data.buffId == data.buffId);
                    if (exist == null)
                    {
                        var inst = new BuffInstance(data, owner, 1);
                        list.Add(inst);

                        InternalDispatch(BuffTrigger.OnBuffStart, new BuffEventContext { Owner = owner, ThisBuff = inst });

                        if (data.removeCondition == BuffRemoveCondition.Instant)
                            RemoveInstance(inst);
                    }
                    break;
                }
        }
    }


    public void RemoveById(string buffId)
    {
        for (int i = list.Count - 1; i >= 0; i--)
            if (list[i].Data != null && list[i].Data.buffId == buffId)
                RemoveInstance(list[i]);
    }

    public void RemoveByTagRandom(BuffTag tag, int stackCount)
    {
        if (stackCount <= 0) return;

        for (int i = 0; i < stackCount; i++)
        {
            // 현재 활성 버프 중에서 해당 태그를 가진 모든 스택을 candidate로 만든다.
            List<BuffInstance> candidates = new List<BuffInstance>();

            foreach (var inst in list)
            {
                if (inst == null || inst.Data == null) continue;
                if (inst.Data.tags == null) continue;
                if (!inst.Data.tags.Contains(tag)) continue;

                // 스택별로 엔트리 추가 (Burn 6, Freeze 1 → 7개 후보)
                for (int s = 0; s < inst.Stacks; s++)
                    candidates.Add(inst);
            }

            if (candidates.Count == 0)
                break;

            int idx = Random.Range(0, candidates.Count);
            var chosen = candidates[idx];
            if (chosen == null) continue;

            chosen.RemoveStacks(1);
        }
    }

    public void RemoveInstance(BuffInstance inst)
    {
        InternalDispatch(BuffTrigger.OnBuffEnd, new BuffEventContext { Owner = owner, ThisBuff = inst });
        list.Remove(inst);
    }

    public bool Contains(BuffInstance inst) => list.Contains(inst);

    // ---------------- Notifies ----------------
    public void NotifyOwnerHit(CharacterBehaviour hitTarget, int damage, HitFilter flags, HitContext hitCtx = null)
    {
        var ctx = new BuffEventContext { Owner = owner, HitTarget = hitTarget, Damage = damage, HitFlags = flags, Hit = hitCtx };
        Dispatch(BuffTrigger.OwnerHit, ctx);
    }

    public void NotifyOwnerHurt(CharacterBehaviour hitSource, int damage, HitFilter flags, HitContext hitCtx = null)
    {
        var ctx = new BuffEventContext { Owner = owner, HitSource = hitSource, Damage = damage, HitFlags = flags, Hit = hitCtx };
        Dispatch(BuffTrigger.OwnerHurt, ctx);
    }
    public void NotifyOwnerDie()
    {
        Dispatch(BuffTrigger.OwnerDie, new BuffEventContext { Owner = owner} );
    }

    public void NotifyOwnerSpawnBullet(BulletBehaviour b)
    {
        Dispatch(BuffTrigger.OwnerSpawnBullet, new BuffEventContext { Owner = owner, Bullet = b });
    }

    public void NotifyOwnerUseSkill(SkillData skill)
    {
        var ctx = new BuffEventContext
        {
            Owner = owner,
            UsedSkill = skill
        };
        Dispatch(BuffTrigger.OwnerUseSkill, ctx);
    }

    // ---------------- Instant hit effects ----------------
    // 히트 순간(대미지 계산 전)에 호출해서 즉시형만 적용. (외부: 공격 처리 코드에서 호출)
    public void ApplyInstantHitEffects(BuffTrigger trigger, BuffEventContext baseCtx)
    {
        int currentFrame = Time.frameCount;
        var snapshot = new List<BuffInstance>(list);

        foreach (var inst in snapshot)
        {
            if (inst == null || !list.Contains(inst)) continue;
            if (inst.CreatedFrame == currentFrame) continue; // 방금 생긴 버프는 이번 히트 무시

            var data = inst.Data;
            if (data == null) continue;

            var abilities = data.abilities;
            if (abilities == null) continue;

            foreach (var a in abilities)
            {
                if (a.trigger != trigger) continue;
                if (!IsInstantHitEffect(a)) continue;
                if (!CanTrigger(a, baseCtx, trigger)) continue;

                // 버프 컨텍스트 붙여서 즉시 적용
                var ctx = new BuffEventContext
                {
                    Owner = owner,
                    HitSource = baseCtx.HitSource,
                    HitTarget = baseCtx.HitTarget,
                    Damage = baseCtx.Damage,
                    ThisBuff = inst,
                    HitFlags = baseCtx.HitFlags,
                    Hit = baseCtx.Hit,
                };
                ExecuteAbility(a, ctx);
                ctx.ThisBuff.OnTriggered();
            }
        }
    }

    private static bool IsInstantHitEffect(BuffAbility a)
        => a.effect == BuffEffect.HitModifyDamage || a.effect == BuffEffect.IgnoreHit;

    // ---------------- Queue & Dispatch ----------------

    private void Dispatch(BuffTrigger trigger, BuffEventContext ctx)
    {
        int currentFrame = Time.frameCount;
        var snapshot = new List<BuffInstance>(list);

        foreach (var inst in snapshot)
        {
            if (inst == null || !list.Contains(inst)) continue;

            InternalDispatch(trigger, new BuffEventContext
            {
                Owner = owner,
                HitSource = ctx.HitSource,
                HitTarget = ctx.HitTarget,
                Damage = ctx.Damage,
                ThisBuff = inst,
                HitFlags = ctx.HitFlags,
                Hit = ctx.Hit,
            });
        }
    }

    // ---------------- Internal Dispatch ----------------
    public void InternalDispatch(BuffTrigger trigger, BuffEventContext ctx)
    {
        if (ctx.ThisBuff == null) return;
        var data = ctx.ThisBuff.Data;
        if (data == null) return;

        var abilities = new List<BuffAbility>(data.abilities);
        bool anyExecuted = false;
        bool hadInstant = HasInstantForTrigger(ctx.ThisBuff, trigger, ctx);

        foreach (var a in abilities)
        {
            if (a.trigger != trigger) continue;
            if (trigger == BuffTrigger.OnEachStack)
            {
                int interval = Mathf.Max(1, a.triggerCount);
                if (ctx.ThisBuff.Stacks % interval != 0)
                    continue;
            }
            if (!CanTrigger(a, ctx, trigger)) continue;

            ExecuteAbility(a, ctx);
            anyExecuted = true;

            if (!Contains(ctx.ThisBuff))
                break;
        }

        // 이벤트당 TriggerCount 1회만 증가.
        if ((anyExecuted || hadInstant) && Contains(ctx.ThisBuff))
            ctx.ThisBuff.OnTriggered();
    }

    private bool HasInstantForTrigger(BuffInstance inst, BuffTrigger trigger, BuffEventContext ctx)
    {
        var data = inst.Data;
        if (data == null) return false;

        foreach (var a in data.abilities)
        {
            if (a.trigger != trigger) continue;
            if (!IsInstantHitEffect(a)) continue;
            if (!CanTrigger(a, ctx, trigger)) continue;
            return true;
        }
        return false;
    }

    private bool CanTrigger(BuffAbility ability, BuffEventContext ctx, BuffTrigger trigger)
    {
        if (HasHitFilter(trigger))
        {
            var filter = ability.requiredHitFilter;
            if (filter != HitFilter.None)
            {
                if ((ctx.HitFlags & filter) == 0)
                    return false;
            }
        }

        // --- 신규 Condition 검사 ---
        switch (ability.condition)
        {
            case BuffCondition.Always:
                return true;

            case BuffCondition.Chance:
                {
                    bool success = Random.value <= (ability.conditionValue / 100f);
                    return ability.conditionNegative ? !success : success;
                }

            case BuffCondition.OwnerHasBuff:
                {
                    bool has = HasBuffWithTag(ctx.Owner, ability.conditionBuffTag);
                    return ability.conditionNegative ? !has : has;
                }

            case BuffCondition.TargetHasBuff:
                {
                    var target = ctx.HitTarget;
                    if (target == null) return false;
                    bool has = HasBuffWithTag(target, ability.conditionBuffTag);
                    return ability.conditionNegative ? !has : has;
                }
            case BuffCondition.SkillUseCasting:
                {
                    var skill = ctx.UsedSkill;
                    bool useSkill = skill != null && skill.useCasting;
                    return ability.conditionNegative ? !useSkill : useSkill;
                }
        }

        return true;
    }

    private static bool HasHitFilter(BuffTrigger trigger)
    {
        return trigger == BuffTrigger.OwnerHit
            || trigger == BuffTrigger.OwnerHurt;
    }

    private static bool HasBuffWithTag(CharacterBehaviour c, BuffTag tag)
    {
        if (c == null || c.Buffs == null) return false;
        foreach (var inst in c.Buffs.GetActiveInstances())
        {
            if (inst?.Data?.tags != null && inst.Data.tags.Contains(tag))
                return true;
        }
        return false;
    }



    // ---------------- Execute ----------------
    public void ExecuteAbility(BuffAbility a, BuffEventContext ctx)
    {
        var resolved = ResolveTarget(a, ctx);
        if (resolved == null) return;

        List<object> targets = new();

        if (resolved is List<CharacterBehaviour> charList)
        {
            foreach (var t in charList)
                targets.Add(t);
        }
        else
        {
            targets.Add(resolved);
        }
        foreach (var targetObj in targets)
        {
            switch (a.effect)
            {
                case BuffEffect.SpawnBullet:
                    {
                        if (a.bulletData == null) continue;
                        if (targetObj is not CharacterBehaviour tChar) continue;

                        Vector3 origin = tChar.transform.position;
                        Vector2 baseDir = Vector2.right;

                        switch (a.direction)
                        {
                            case SpawnDirection.MoveDirection:
                                baseDir = tChar.MoveDirection == Vector2.zero
                                    ? tChar.transform.right
                                    : tChar.MoveDirection.normalized;
                                break;

                            case SpawnDirection.ToTarget:
                                if (tChar.AI != null && tChar.target != null)
                                    baseDir = (tChar.target.transform.position - tChar.transform.position).normalized;
                                break;

                            case SpawnDirection.ToNearestEnemy:
                                {
                                    var n = EntityContainer.Instance.GetNearestEnemy(tChar);
                                    if (n != null) baseDir = (n.transform.position - tChar.transform.position).normalized;
                                    break;
                                }

                            case SpawnDirection.ToNearestAlly:
                                {
                                    var n = EntityContainer.Instance.GetNearestAlly(tChar);
                                    if (n != null) baseDir = (n.transform.position - tChar.transform.position).normalized;
                                    break;
                                }

                            case SpawnDirection.Up:
                                baseDir = tChar.transform.up;
                                break;
                        }
                        float ang = UnityEngine.Random.Range(a.angleOffsetRange.x, a.angleOffsetRange.y);
                        Vector2 finalDir = Quaternion.Euler(0, 0, -ang) * baseDir;

                        var bullet = GameObject.Instantiate(
                            BulletSpawner.Instance.bulletPrefab,
                            origin,
                            Quaternion.LookRotation(Vector3.forward, finalDir)
                        );

                        // === 수정된 Init ===
                        bullet.Init(a.bulletData, tChar, finalDir, ResolveTeam(a.teamSelector, tChar));

                        tChar.Buffs.NotifyOwnerSpawnBullet(bullet);
                    }
                    break;
                case BuffEffect.SpawnCharacter:
                    {
                        if (a.characterData == null) continue;
                        if (targetObj is not CharacterBehaviour tChar) continue;

                        TeamType team = ResolveTeam(a.teamSelector, tChar);

                        Vector2 forward = tChar.MoveDirection == Vector2.zero
                        ? (Vector2)tChar.transform.up      // y+ 가 앞
                        : tChar.MoveDirection.normalized;  // y+ 가 앞

                        Vector2 right = new Vector2(forward.y, -forward.x);

                        Vector3 basePos =
                            tChar.transform.position +
                            (Vector3)(forward * a.offset.y) +   // offset.y = 앞/뒤
                            (Vector3)(right * a.offset.x);

                        for (int i = 0; i < GetFinalEffectCount(a, ctx); i++)
                        {
                            Vector2 rnd = a.range > 0f ? UnityEngine.Random.insideUnitCircle * a.range : Vector2.zero;
                            Vector3 spawnPos = basePos + (Vector3)rnd;

                            var newChar = GameObject.Instantiate(
                                CharacterSpawner.Instance.characterPrefab,
                                spawnPos,
                                Quaternion.identity
                            );

                            newChar.Init(a.characterData, team);
                        }
                    }
                    break;

                case BuffEffect.GainAtk:
                case BuffEffect.GainResilience:
                case BuffEffect.GainSpeedUp:
                case BuffEffect.GainSlow:
                case BuffEffect.GainSkillCastingHaste:
                case BuffEffect.GainSkillCooldownHaste:
                    {
                        if (targetObj is CharacterBehaviour tChar)
                        {
                            tChar.Buffs.AddStat(a.effect, GetFinalEffectValue(a, ctx));
                            tChar.RecalculateFinalStats();
                        }
                    }
                    break;

                case BuffEffect.Heal:
                    {
                        if (targetObj is CharacterBehaviour healChar)
                        {
                            float baseHeal = GetFinalEffectValue(a, ctx);
                            float healerMul = ctx.Owner != null ? ctx.Owner.GetResilience() / 100f : 1f;
                            int heal = Mathf.RoundToInt(baseHeal * healerMul);
                            healChar.Heal(heal);
                        }
                    }
                    break;

                case BuffEffect.InstantDamage:
                    {
                        if (targetObj is CharacterBehaviour dmgChar)
                        {
                            int dmg = Mathf.Max(1, Mathf.RoundToInt(GetFinalEffectValue(a, ctx)));
                            dmgChar.ApplyDamage(ctx.Owner, dmg);

                            ctx.Owner.Buffs.NotifyOwnerHit(dmgChar, ctx.Damage, HitFilter.Element, ctx.Hit); //OwnerHit
                            dmgChar.Buffs.NotifyOwnerHurt(ctx.Owner, ctx.Damage, HitFilter.Element, ctx.Hit); // OwnerHurt
                        }
                    }
                    break;

                case BuffEffect.InstantKill:
                    {
                        if (targetObj is CharacterBehaviour killChar)
                        {
                            killChar.ChangeState(CharacterState.Die);
                        }
                    }
                    break;

                case BuffEffect.AddBuff:
                    if (a.buffToAdd != null && targetObj is CharacterBehaviour addChar)
                        addChar.Buffs.AddBuff(a.buffToAdd, GetFinalEffectCount(a, ctx));
                    break;

                case BuffEffect.RemoveBuffById:
                    if (!string.IsNullOrEmpty(a.removeBuffId) && targetObj is CharacterBehaviour rmChar)
                        rmChar.Buffs.RemoveById(a.removeBuffId);
                    break;
                case BuffEffect.RemoveBuffByTag:
                    if (a.effectBuffTag != BuffTag.None && targetObj is CharacterBehaviour tagChar)
                        tagChar.Buffs.RemoveByTagRandom(a.effectBuffTag, GetFinalEffectCount(a, ctx));
                    break;
                case BuffEffect.RemoveBuff:
                    if (ctx.ThisBuff != null)
                        RemoveInstance(ctx.ThisBuff);
                    break;

                case BuffEffect.HitModifyDamage:
                    if (targetObj is HitContext hit)
                        hit.Damage = Mathf.RoundToInt(hit.Damage * (1f + GetFinalEffectValue(a, ctx) / 100f));
                    break;

                case BuffEffect.IgnoreHit:
                    if (targetObj is HitContext hit2)
                        hit2.Ignore = true;
                    break;

                case BuffEffect.TriggerVfx1:
                case BuffEffect.TriggerVfx2:
                case BuffEffect.TriggerVfx3:
                    {
                        if (ctx.ThisBuff.Data.vfxList == null) continue;
                        foreach (var v in ctx.ThisBuff.Data.vfxList)
                        {
                            if (v.trigger == a.effect && v.vfxPrefab != null && ctx.Owner != null)
                            {
                                var vfx = Object.Instantiate(v.vfxPrefab, ctx.Owner.transform.position, Quaternion.identity);
                                vfx.Init(ctx.Owner.transform, vfx.followType, vfx.duration, ctx.ThisBuff);
                            }
                        }
                        break;
                    }
            }
        }
    }

    // ---------------- Resolve ----------------
    private object ResolveTarget(BuffAbility a, BuffEventContext ctx)
{
    switch (a.target)
    {
        case BuffTarget.ThisBuff: return ctx.ThisBuff;
        case BuffTarget.Owner: return owner;
        case BuffTarget.TriggeredHit: return ctx.Hit;
        case BuffTarget.Attacker: return ctx.HitSource;
        case BuffTarget.HitTarget: return ctx.HitTarget;

        case BuffTarget.NearestEnemy:
            return a.useRange
                ? EntityContainer.Instance.GetNeaestEnemyInRange(owner, a.targetRange)
                : EntityContainer.Instance.GetNearestEnemy(owner);

        case BuffTarget.NearestAlly:
            return a.useRange
                ? EntityContainer.Instance.GetNeaestAllyInRange(owner, a.targetRange)
                : EntityContainer.Instance.GetNearestAlly(owner);

            case BuffTarget.AllAlly:
                return a.useRange
                    ? EntityContainer.Instance.GetAlliesInRange(owner, a.targetRange)
                    : EntityContainer.Instance.GetAllAllies(owner);

            case BuffTarget.AllEnemy:
                return a.useRange
                    ? EntityContainer.Instance.GetEnemiesInRange(owner, a.targetRange)
                    : EntityContainer.Instance.GetAllEnemies(owner);
        }

    return null;
}

    private TeamType ResolveTeam(BuffTeamSelector selector, CharacterBehaviour target)
    {
        return selector switch
        {
            BuffTeamSelector.Target => target.team,
            BuffTeamSelector.Player => TeamType.Player,
            BuffTeamSelector.Enemy => TeamType.Enemy,
            _ => target.team
        };
    }

    // ---------------- ClearAll ----------------
    public void ClearAll()
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var inst = list[i];
            if (inst == null) continue;

            InternalDispatch(BuffTrigger.OnBuffEnd, new BuffEventContext
            {
                Owner = owner,
                ThisBuff = inst
            });

            list.RemoveAt(i);
        }

        pendingAddApply.Clear();
        pendingAddNext.Clear();
    }

    // BuffManager 맨 아래쪽에 추가
    public List<BuffInstance> GetActiveInstances() => new List<BuffInstance>(list);


    //계산식
    private (float floatVal, int intVal) GetFinalEffectValuesRaw(BuffAbility a, BuffEventContext ctx)
    {
        float finalValue = a.effectValue;
        int finalCount = a.effectCount;

        if (a.formulas != null)
        {
            foreach (var f in a.formulas)
            {
                switch (f.type)
                {
                    case BuffFormulaType.StackPer:
                        if (ctx.ThisBuff != null)
                            finalValue += f.value * ctx.ThisBuff.Stacks;
                            finalCount += (int)(f.value * ctx.ThisBuff.Stacks);
                        break;

                    case BuffFormulaType.OwnerAttackPercent:
                        if (ctx.Owner != null)
                            finalValue += ctx.Owner.GetAttackValue() * (f.value / 100f);
                            finalCount += (int)(ctx.Owner.GetAttackValue() * (f.value / 100f));
                        break;

                    case BuffFormulaType.OwnerHPPercent:
                        if (ctx.Owner != null)
                            finalValue += ctx.Owner.GetMaxHealth() * (f.value / 100f);
                            finalCount += (int)(ctx.Owner.GetMaxHealth() * (f.value / 100f));
                        break;

                    case BuffFormulaType.StackPerOwnerAttackPercent: // ★ 추가
                        if (ctx.Owner != null && ctx.ThisBuff != null)
                        {
                            float atkRatio = ctx.Owner.GetAttackValue() * (f.value / 100f);
                            finalValue += atkRatio * ctx.ThisBuff.Stacks;
                            finalCount += (int)(atkRatio * ctx.ThisBuff.Stacks);
                        }
                        break;

                    case BuffFormulaType.StackPerOwnerHPPercent: // ★ 추가
                        if (ctx.Owner != null && ctx.ThisBuff != null)
                        {
                            float atkRatio = ctx.Owner.GetMaxHealth() * (f.value / 100f);
                            finalValue += atkRatio * ctx.ThisBuff.Stacks;
                            finalCount += (int)(atkRatio * ctx.ThisBuff.Stacks);
                        }
                        break;
                    case BuffFormulaType.EntityCount:
                        {
                            int count = 0;
                            string key = f.entityName;
                            if (!string.IsNullOrEmpty(key))
                            {
                                foreach (var c in EntityContainer.Instance.Characters)
                                {
                                    if (c != null && c.gameObject.name.Contains(key))
                                        count++;
                                }
                            }
                            else
                            {
                                Debug.Log("애앵");
                            }
                                finalValue += count * f.value;  
                            finalCount += (int)(count * f.value);  
                            break;
                        }

                    case BuffFormulaType.Min:
                        finalValue = Mathf.Max(finalValue, f.value);
                        finalCount = Mathf.Max(finalCount, (int)f.value);
                        break;

                    case BuffFormulaType.Max:
                        finalValue = Mathf.Min(finalValue, f.value);
                        finalCount = Mathf.Min(finalCount, (int)f.value);
                        break;
                }
            }
        }
        return (finalValue, finalCount);
    }

    private float GetFinalEffectValue(BuffAbility a, BuffEventContext ctx)
    {
        return GetFinalEffectValuesRaw(a, ctx).floatVal;
    }
    private int GetFinalEffectCount(BuffAbility a, BuffEventContext ctx)
    {
        return GetFinalEffectValuesRaw(a, ctx).intVal;
    }
}
