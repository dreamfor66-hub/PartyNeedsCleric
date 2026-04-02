using UnityEngine;
using static UnityEngine.Analytics.IAnalytic;

public static class CollisionManager
{
    public static void Resolve(CharacterBehaviour A, CharacterBehaviour B)
    {
        float aSpeed = A.lastVelocity.magnitude;
        float bSpeed = B.lastVelocity.magnitude;

        CharacterBehaviour attacker;
        CharacterBehaviour target;

        // attacker / target 결정
        if (aSpeed > bSpeed)
        {
            attacker = A;
            target = B;
        }
        else if (bSpeed > aSpeed)
        {
            attacker = B;
            target = A;
        }
        else
        {
            // === 동일 속도: 자연스럽게 쌍방 대미지 ===
            attacker = A;   // 의미 없음, 그냥 쌍방 처리
            target = B;
        }

        HitContext hitCtx_attacker = new HitContext
        {
            Attacker = attacker,
            Target = target,
            Damage = Mathf.RoundToInt(attacker.GetAttackValue() * attacker.data.baseHitData.attackMultiplier),
            Ignore = false
        };

        HitContext hitCtx_target = new HitContext
        {
            Attacker = target,
            Target = attacker,
            Damage = Mathf.RoundToInt(target.GetAttackValue() * target.data.baseHitData.attackMultiplier),
            Ignore = false
        };
        hitCtx_attacker.Damage = attacker.ApplyDamageEnhance(hitCtx_attacker.Damage, HitFilter.Direct, target);
        hitCtx_target.Damage = target.ApplyDamageEnhance(hitCtx_target.Damage, HitFilter.Direct, attacker);

        // === (2) Buff 즉시 효과 적용 (대미지 조정/무효화) ===
        // 공격자 → OwnerHit 트리거
        attacker.Buffs.ApplyInstantHitEffects(
            BuffTrigger.OwnerHit,
            new BuffEventContext
            {
                Owner = attacker,
                HitSource = attacker,
                HitTarget = target,
                Hit = hitCtx_attacker,
                Damage = hitCtx_attacker.Damage
            }
        );

        // 피격자 → OwnerHurt 트리거
        target.Buffs.ApplyInstantHitEffects(
            BuffTrigger.OwnerHurt,
            new BuffEventContext
            {
                Owner = target,
                HitSource = attacker,
                HitTarget = target,
                Hit = hitCtx_attacker,
                Damage = hitCtx_attacker.Damage
            }
        );

        float attackerSpeed = attacker.lastVelocity.magnitude;
        float targetSpeed = target.lastVelocity.magnitude;

        bool isAttackerRanged = attacker.data.characterType == CharacterType.Ranged;
        bool isTargetRanged = target.data.characterType == CharacterType.Ranged;

        bool attackerImmune = attackerSpeed >= targetSpeed * 2f;

        if (!attackerImmune)
        {
            attacker.Buffs.ApplyInstantHitEffects(
                BuffTrigger.OwnerHurt,
                new BuffEventContext
                {
                    Owner = attacker,
                    HitSource = target,
                    HitTarget = attacker,
                    Hit = hitCtx_target,
                    Damage = hitCtx_target.Damage
                }
            );

            target.Buffs.ApplyInstantHitEffects(
                BuffTrigger.OwnerHit,
                new BuffEventContext
                {
                    Owner = target,
                    HitSource = target,
                    HitTarget = attacker,
                    Hit = hitCtx_target,
                    Damage = hitCtx_target.Damage
                }
            );
        }

        if (isAttackerRanged && attacker.AI is not null)
            attacker.AI.NotifyHit();
        if (isTargetRanged && target.AI is not null)
            target.AI.NotifyHit();

        if (!hitCtx_attacker.Ignore && !attacker.isCastingFSM)
        {
            attacker.PlayAttackAnim();
            //attacker의 공격
            target.ApplyDamage(attacker, hitCtx_attacker.Damage);

            var hd = attacker.data.baseHitData;
            for (int i = 0; i < hd.hitApplyBuffs.Count; i++)
            {
                var e = hd.hitApplyBuffs[i];
                for (int c = 0; c < e.count; c++)
                    target.Buffs.AddBuff(e.buff);
            }


            HitFxTable.Instance.Spawn(attacker.data.baseHitData.hitFxType, target.transform.position);

            attacker.Buffs.NotifyOwnerHit(target, hitCtx_attacker.Damage, HitFilter.Direct, hitCtx_attacker); // OwnerHit
            target.Buffs.NotifyOwnerHurt(attacker, hitCtx_attacker.Damage, HitFilter.Direct, hitCtx_attacker); //OwnerHurt
        }

        if (!attackerImmune && !hitCtx_target.Ignore && !target.isCastingFSM)
        {
            target.PlayAttackAnim();
            //target의 반격
            attacker.ApplyDamage(target, hitCtx_target.Damage);

            var hd2 = target.data.baseHitData;
            for (int i = 0; i < hd2.hitApplyBuffs.Count; i++)
            {
                var e = hd2.hitApplyBuffs[i];
                for (int c = 0; c < e.count; c++)
                    attacker.Buffs.AddBuff(e.buff);
            }

            HitFxTable.Instance.Spawn(target.data.baseHitData.hitFxType, attacker.transform.position);

            target.Buffs.NotifyOwnerHit(attacker, hitCtx_target.Damage, HitFilter.Direct, hitCtx_target); //OwnerHit
            attacker.Buffs.NotifyOwnerHurt(target, hitCtx_target.Damage, HitFilter.Direct, hitCtx_target); // OwnerHurt
        }

        float hs = attacker.data.baseHitData.hitStop;
        attacker.ApplyHitStop(hs);
        target.ApplyHitStop(hs);

        // 넉백은 항상 양쪽
        Vector2 dir = (target.transform.position - attacker.transform.position).normalized;

        if (!hitCtx_attacker.Ignore)
        {
            target.ApplyKnockback(dir, attacker.data.baseHitData.knockbackPower);
            target.lastHitData = attacker.data.baseHitData;
        }

        if (!hitCtx_target.Ignore)
        {
            attacker.ApplyKnockback(-dir, target.data.baseHitData.knockbackPower);
            attacker.lastHitData = target.data.baseHitData;
        }
    }

    public static void ResolveWall(CharacterBehaviour target, Collision2D col)
    {
        if (target.state != CharacterState.Knockback)
            return;
        if (col.contactCount == 0)
            return;

        // 진행 중 속도
        Vector2 v = target.lastVelocity;
        float speed = v.magnitude;

        // 너무 느리면 튕김 없음
        if (speed < GameVariables.Instance.wallCollisionMinSpeed)
            return;

        // 충돌 normal
        var contact = col.GetContact(0);
        Vector2 normal = contact.normal;
        if (Vector2.Dot(v, normal) > 0f)
            normal = -normal;

        // 반사 방향
        Vector2 reflectDir = Vector2.Reflect(v.normalized, normal);

        // HitContext (대미지 0짜리 히트)
        HitContext hitCtx = new HitContext
        {
            Attacker = null,
            Target = target,
            Damage = 0,
            Ignore = false
        };

        var ctx = new BuffEventContext
        {
            Owner = target,
            HitSource = null,
            HitTarget = target,
            Bullet = null,
            Damage = 0,
            ThisBuff = null,
            HitFlags = HitFilter.Collision,
            Hit = hitCtx
        };

        // 버프 즉시효과 (OwnerHurt, Collision 플래그)
        target.Buffs.ApplyInstantHitEffects(BuffTrigger.OwnerHurt, ctx);

        // HitStop
        float hs = target.data.baseHitData.hitStop;
        target.ApplyHitStop(hs);

        // ★ 핵심: 속도 유지용 넉백 파워 계산
        // Impulse = mass * deltaV => deltaV 원하는 속도(speed)
        float power = target.rb.mass * speed;

        // HitStop 중이면 pendingKnockback으로 들어가고,
        // 끝난 뒤에 AddForce(power) → 최종 속도 = speed 유지
        target.ApplyKnockback(reflectDir, power);
        target.lastHitData = target.data.baseHitData;

        HitFxTable.Instance.Spawn(HitFxType.Collision, target.transform.position);

        // 버프 알림
        target.Buffs.NotifyOwnerHurt(null, 0, HitFilter.Collision, hitCtx);
    }
}
