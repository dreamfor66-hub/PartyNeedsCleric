using System.Collections.Generic;
using UnityEngine;

public class CharacterAction
{
    private readonly CharacterBehaviour owner;

    private ActionData current;
    private int currentFrame;
    private float frameTimer;
    private float frameStep;

    private HashSet<int> firedFrames = new HashSet<int>();
    private Dictionary<int, HashSet<CharacterBehaviour>> hitGroupTriggered = new();
    private Dictionary<int, float> hitGroupResetTimers = new();
    private readonly List<VfxObject> spawnedActionVfx = new();
    public bool IsPlaying => current != null;

    private Vector2 actionForward = Vector2.right;   // Action 시작 시 바라보던 기준 방향 (고정)
    private Vector2 lastRefDir = Vector2.right;

    public CharacterAction(CharacterBehaviour owner)
    {
        this.owner = owner;
    }

    public void Start(ActionData data)
    {
        current = data;
        currentFrame = 0;
        frameTimer = 0f;
        frameStep = 1f / 60;

        firedFrames.Clear();
        hitGroupTriggered.Clear();
        hitGroupResetTimers.Clear(); // 추가
        owner.actionHitGizmos.Clear();
        ClearActionVfx();

        ApplyAutoCorrection();
        lastRefDir = actionForward;
        owner.rb.linearVelocity = Vector2.zero;
    }

    public void Cancel()
    {
        current = null;
        owner.rb.linearVelocity = Vector2.zero;
        firedFrames.Clear();
        hitGroupTriggered.Clear();
        hitGroupResetTimers.Clear();
        owner.actionHitGizmos.Clear();
        ClearActionVfx();
    }
    private void End()
    {
        current = null;
        owner.rb.linearVelocity = Vector2.zero;
        firedFrames.Clear();
        hitGroupTriggered.Clear();
        hitGroupResetTimers.Clear();
        owner.actionHitGizmos.Clear();
        ClearActionVfx();
    }

    // return: finished
    public bool Tick(float dt)
    {
        if (current == null) return true;

        ProcessRotations(currentFrame, dt);
        ApplyMovement(currentFrame);
        ProcessHitBoxes();

        frameTimer += dt;

        while (frameTimer >= frameStep)
        {
            frameTimer -= frameStep;
            currentFrame++;

            FrameEvents(currentFrame);

            if (currentFrame >= current.totalFrame)
            {
                End();
                return true;
            }
        }

        return false;
    }

    private void ProcessRotations(int frame, float dt)
    {
        if (current.rotationList == null || current.rotationList.Count == 0)
            return;

        for (int i = 0; i < current.rotationList.Count; i++)
        {
            var r = current.rotationList[i];
            if (frame < r.startFrame || frame > r.endFrame)
                continue;

            Vector2 desired = GetRotationDesiredDir(r.rotationType);
            if (desired.sqrMagnitude <= 0.0001f)
                continue;

            desired.Normalize();

            float curAng = Mathf.Atan2(actionForward.y, actionForward.x) * Mathf.Rad2Deg;
            float tarAng = Mathf.Atan2(desired.y, desired.x) * Mathf.Rad2Deg;

            float delta = Mathf.DeltaAngle(curAng, tarAng);
            float maxStep = Mathf.Abs(r.value) * dt;

            float step = Mathf.Clamp(delta, -maxStep, maxStep);
            float nextAng = curAng + step;

            float rad = nextAng * Mathf.Deg2Rad;
            actionForward = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            if (actionForward.sqrMagnitude > 0.0001f)
                actionForward.Normalize();

            lastRefDir = actionForward;
        }
    }

    private Vector2 GetRotationDesiredDir(ActionRotationType t)
    {
        if (t == ActionRotationType.ToTarget)
        {
            if (owner.target == null) return Vector2.zero;

            Vector2 d = (Vector2)(owner.target.transform.position - owner.transform.position);
            if (d.sqrMagnitude > 0.0001f) return d.normalized;
            return Vector2.zero;
        }

        if (t == ActionRotationType.ToNearestEnemy)
        {
            var e = EntityContainer.Instance.GetNearestEnemy(owner);
            if (e == null) return Vector2.zero;

            Vector2 d = (Vector2)(e.transform.position - owner.transform.position);
            if (d.sqrMagnitude > 0.0001f) return d.normalized;
            return Vector2.zero;
        }

        // ToNearestAlly
        {
            var a = EntityContainer.Instance.GetNearestAlly(owner);
            if (a == null) return Vector2.zero;

            Vector2 d = (Vector2)(a.transform.position - owner.transform.position);
            if (d.sqrMagnitude > 0.0001f) return d.normalized;
            return Vector2.zero;
        }
    }
    private void ApplyMovement(int frame)
    {
        if (current.movementList == null || current.movementList.Count == 0)
            return;

        for (int i = 0; i < current.movementList.Count; i++)
        {
            var m = current.movementList[i];
            if (frame < m.startFrame || frame > m.endFrame) continue;

            float t = Mathf.InverseLerp(m.startFrame, m.endFrame, frame);
            float v = Mathf.Lerp(m.startValue, m.endValue, t);

            if (m.applyHaste)
            {
                const float refSpeed = 50f;              // 기준점(50일 때 값이 “정확”)
                float currentSpeed = owner.GetSpeed();   // 이미 장비/버프 반영된 최종 속도
                float mul = currentSpeed / refSpeed;
                v *= mul;
            }

            owner.rb.linearVelocity = actionForward * v;
            return;
        }
    }


    private void FrameEvents(int frame)
    {
        if (firedFrames.Contains(frame))
            return;

        firedFrames.Add(frame);

        if (current.spawnBulletList != null && current.spawnBulletList.Count > 0)
        {
            for (int i = 0; i < current.spawnBulletList.Count; i++)
            {
                var e = current.spawnBulletList[i];
                if (e.frame != frame) continue;

                var anchorChar = GetAnchorCharacter(e.anchor);
                Vector3 anchorPos = anchorChar != null ? anchorChar.transform.position : owner.transform.position;

                // 1) directionType 기준으로 "기준 방향" 산출 (AngleOffset 적용 전)
                Vector3 basisDir = GetDir(e.directionType, anchorChar, anchorPos);

                // 2) Offset은 "기준 방향" 기준으로만 적용 (front=+Y, right=+X)
                Vector3 spawnPos = GetSpawnPos(anchorPos, basisDir, e.spawnOffset);

                // 3) 위치/기준방향이 결정된 뒤, AngleOffset으로 방향만 회전 (양수=시계)
                Vector3 finalDir = RotateDir(basisDir, e.angleOffset);

                var prefab = BulletSpawner.Instance.bulletPrefab;

                // front(+Y)가 finalDir을 바라보도록 회전 세팅
                var b = Object.Instantiate(prefab, spawnPos, Quaternion.FromToRotation(Vector3.up, finalDir));

                b.Init(e.bullet, owner, finalDir, owner.team);
                owner.Buffs.NotifyOwnerSpawnBullet(b);
            }
        }


        if (current.buffList != null && current.buffList.Count > 0)
        {
            for (int i = 0; i < current.buffList.Count; i++)
            {
                var e = current.buffList[i];
                if (e.frame != frame) continue;

                switch (e.type)
                {
                    case ActionBuffEventType.Add:
                        for (int k = 0; k < e.count; k++)
                            owner.Buffs.AddBuff(e.buff);
                        break;

                    case ActionBuffEventType.RemoveByTag:
                        owner.Buffs.RemoveByTagRandom(e.removeTag, e.count);
                        break;
                }
            }
        }

        if (current.spawnCharacterList != null && current.spawnCharacterList.Count > 0)
        {
            for (int i = 0; i < current.spawnCharacterList.Count; i++)
            {
                var e = current.spawnCharacterList[i];
                if (e.frame != frame) continue;

                var anchorChar = GetAnchorCharacter(e.anchor);
                Vector3 anchorPos = anchorChar != null ? anchorChar.transform.position : owner.transform.position;

                // directionType 기준 방향(AngleOffset 없음)
                Vector3 basisDir = GetDir(e.directionType, anchorChar, anchorPos);

                // Offset: front=+Y, right=+X
                Vector3 spawnPos = GetSpawnPos(anchorPos, basisDir, e.spawnOffset);

                Quaternion rot = Quaternion.identity;
                Vector3 d = basisDir;
                d.z = 0f;
                if (d.sqrMagnitude > 0.0001f)
                {
                    d.Normalize();
                    rot = Quaternion.FromToRotation(Vector3.up, d);
                }

                var spawned = Object.Instantiate(
                    CharacterSpawner.Instance.characterPrefab,
                    spawnPos,
                    rot
                );
                spawned.Init(e.character, owner.team);
            }
        }

        if (current.restoreList != null && current.restoreList.Count > 0)
        {
            for (int i = 0; i < current.restoreList.Count; i++)
            {
                var e = current.restoreList[i];
                if (e.frame != frame) continue;

                switch (e.type)
                {
                    case ActionRestoreType.Heal:
                        {
                            float baseHeal = e.value;
                            float healerMul = owner.GetResilience() / 100f;
                            int heal = Mathf.RoundToInt(baseHeal * healerMul);
                            owner.Heal(heal);
                        }
                        break;

                    case ActionRestoreType.Mana:
                        {
                            int mana = Mathf.RoundToInt(e.value);
                            owner.RestoreMana(mana);
                        }
                        break;
                }
            }
        }
        if (current.vfxList != null && current.vfxList.Count > 0)
        {
            for (int i = 0; i < current.vfxList.Count; i++)
            {
                var e = current.vfxList[i];
                if (e.frame != frame) continue;
                SpawnActionVfx(e);
            }
        }

    }
    private Vector3 GetDir(ActionDirectionType type, CharacterBehaviour anchorChar, Vector3 fromPos)
    {
        CharacterBehaviour src = anchorChar != null ? anchorChar : owner;

        Vector2 face2 = ResolveFacingDir(src);
        Vector3 forward = new Vector3(face2.x, face2.y, 0f);
        Vector3 last = new Vector3(lastRefDir.x, lastRefDir.y, 0f);

        switch (type)
        {
            case ActionDirectionType.Forward:
                {
                    if (face2.sqrMagnitude > 0.0001f)
                    {
                        lastRefDir = face2.normalized;
                        return forward.normalized;
                    }
                    return last;
                }

            case ActionDirectionType.ToTarget:
                {
                    if (src != null && src.target != null)
                    {
                        Vector3 d = src.target.transform.position - fromPos;
                        if (d.sqrMagnitude > 0.0001f)
                        {
                            lastRefDir = ((Vector2)d).normalized;
                            return d.normalized;
                        }
                    }
                    return last;
                }

            case ActionDirectionType.ToNearestEnemy:
                {
                    var t = EntityContainer.Instance.GetNearestEnemy(src);
                    if (t != null)
                    {
                        Vector3 d = t.transform.position - fromPos;
                        if (d.sqrMagnitude > 0.0001f)
                        {
                            lastRefDir = ((Vector2)d).normalized;
                            return d.normalized;
                        }
                    }
                    return last;
                }

            case ActionDirectionType.ToNearestAlly:
                {
                    var t = EntityContainer.Instance.GetNearestAlly(src);
                    if (t != null)
                    {
                        Vector3 d = t.transform.position - fromPos;
                        if (d.sqrMagnitude > 0.0001f)
                        {
                            lastRefDir = ((Vector2)d).normalized;
                            return d.normalized;
                        }
                    }
                    return last;
                }
        }

        return forward;
    }

    private bool CanHitTarget(int groupId, CharacterBehaviour target)
    {
        if (!hitGroupTriggered.TryGetValue(groupId, out var set))
        {
            set = new HashSet<CharacterBehaviour>();
            hitGroupTriggered[groupId] = set;
        }

        if (set.Contains(target))
            return false;

        set.Add(target);
        return true;
    }

    private HitData GetHitData(string hitId)
    {
        if (current.hits != null && current.hits.Count > 0)
        {
            for (int i = 0; i < current.hits.Count; i++)
            {
                var h = current.hits[i];
                if (h != null && h.hitId == hitId)
                    return h;
            }
        }

        return owner.data.baseHitData;
    }

    private void ProcessHitBoxes()
    {
        if (current.hitBoxes == null || current.hitBoxes.Count == 0)
            return;

        // hitBoxGroup reset interval 처리 (활성 프레임 동안만 누적)
        List<int> processedGroups = null;

        for (int i = 0; i < current.hitBoxes.Count; i++)
        {
            var hb = current.hitBoxes[i];
            if (currentFrame < hb.startFrame || currentFrame > hb.endFrame)
                continue;

            if (hb.hitInterval <= 0f)
                continue;

            int gid = hb.hitBoxGroup;

            bool already = false;
            if (processedGroups != null)
            {
                for (int p = 0; p < processedGroups.Count; p++)
                {
                    if (processedGroups[p] == gid)
                    {
                        already = true;
                        break;
                    }
                }
            }

            if (already)
                continue;

            processedGroups ??= new List<int>(4);
            processedGroups.Add(gid);

            hitGroupResetTimers.TryGetValue(gid, out float t);
            t += frameStep;

            if (t >= hb.hitInterval)
            {
                t -= hb.hitInterval;

                if (hitGroupTriggered.TryGetValue(gid, out var set))
                    set.Clear();
            }

            hitGroupResetTimers[gid] = t;
        }

        Vector3 basePos = owner.transform.position;

        // ===== [수정] 액션 기준 방향(actionForward)만 사용 =====
        Vector3 dir = new Vector3(actionForward.x, actionForward.y, 0f);
        dir.z = 0f;

        if (dir.sqrMagnitude <= 0.0001f)
            return;

        dir.Normalize();

        // forward=(0,1)일 때 right=(1,0)
        Vector3 right = new(dir.y, -dir.x, 0f);

        for (int i = 0; i < current.hitBoxes.Count; i++)
        {
            var hb = current.hitBoxes[i];
            if (currentFrame < hb.startFrame || currentFrame > hb.endFrame)
                continue;

            Vector3 hitCenter = basePos + right * hb.offset.x + dir * hb.offset.y;

            owner.actionHitGizmos.Add(new CharacterBehaviour.ActionHitGizmo
            {
                center = hitCenter,
                radius = hb.radius
            });

            HitData hitData = GetHitData(hb.hitId);

            foreach (var t in EntityContainer.Instance.Characters)
            {
                if (t == null) continue;
                if (t == owner) continue;
                if (t.state == CharacterState.Die) continue;
                if (t.team == owner.team) continue;

                float dist = Vector3.Distance(hitCenter, t.transform.position);
                if (dist <= t.col.radius + hb.radius)
                {
                    if (!CanHitTarget(hb.hitBoxGroup, t))
                        continue;

                    ApplyHit(t, hitData, hitCenter);
                }
            }
        }
        owner.actionHitGizmos.Clear();
    }



    private void ApplyHit(CharacterBehaviour target, HitData hitData, Vector3 hitCenter)
    {
        int baseDamage = Mathf.RoundToInt(owner.GetAttackValue() * hitData.attackMultiplier);
        int enhancedDamage = owner.ApplyDamageEnhance(baseDamage, HitFilter.Direct, target);

        HitContext hitCtx = new HitContext
        {
            Attacker = owner,
            Target = target,
            Damage = enhancedDamage,
            Ignore = false
        };
        hitCtx.Damage = owner.ApplyDamageEnhance(hitCtx.Damage, HitFilter.Direct, target);

        owner.Buffs.ApplyInstantHitEffects(
            BuffTrigger.OwnerHit,
            new BuffEventContext
            {
                Owner = owner,
                HitSource = owner,
                HitTarget = target,
                Hit = hitCtx,
                Damage = hitCtx.Damage,
                HitFlags = HitFilter.Direct
            }
        );

        target.Buffs.ApplyInstantHitEffects(
            BuffTrigger.OwnerHurt,
            new BuffEventContext
            {
                Owner = target,
                HitSource = owner,
                HitTarget = target,
                Hit = hitCtx,
                Damage = hitCtx.Damage,
                HitFlags = HitFilter.Direct
            }
        );

        if (!hitCtx.Ignore)
        {
            target.ApplyDamage(owner, hitCtx.Damage);

            for (int i = 0; i < hitData.hitApplyBuffs.Count; i++)
            {
                var e = hitData.hitApplyBuffs[i];
                for (int c = 0; c < e.count; c++)
                    target.Buffs.AddBuff(e.buff);
            }

            HitFxTable.Instance.Spawn(hitData.hitFxType, target.transform.position);

            owner.Buffs.NotifyOwnerHit(target, hitCtx.Damage, HitFilter.Direct, hitCtx);
            target.Buffs.NotifyOwnerHurt(owner, hitCtx.Damage, HitFilter.Direct, hitCtx);

            float hs = hitData.hitStop;
            owner.ApplyHitStop(hs);
            target.ApplyHitStop(hs);

            Vector2 dir = hitData.GetKnockbackDirection(
                owner.transform.position,
                hitCenter,
                target.transform.position
            );

            target.ApplyKnockback(dir, hitData.knockbackPower);
            target.lastHitData = hitData;
        }
    }
    private Vector3 GetSpawnPos(Vector3 anchorPos, Vector3 basisDir, Vector2 offset)
    {
        basisDir.z = 0f;
        if (basisDir.sqrMagnitude <= 0.0001f)
            basisDir = new Vector3(actionForward.x, actionForward.y, 0f);

        basisDir.Normalize();

        // forward=(0,1)일 때 right=(1,0) 되도록
        Vector3 right = new Vector3(basisDir.y, -basisDir.x, 0f);
        return anchorPos + right * offset.x + basisDir * offset.y;
    }

    private Vector3 RotateDir(Vector3 dir, float angleDeg)
    {
        dir.z = 0f;
        if (dir.sqrMagnitude <= 0.0001f)
            dir = new Vector3(actionForward.x, actionForward.y, 0f);

        dir.Normalize();

        // (+) = 시계방향
        float rad = (-angleDeg) * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        float x = dir.x * cos - dir.y * sin;
        float y = dir.x * sin + dir.y * cos;

        Vector3 r = new Vector3(x, y, 0f);
        if (r.sqrMagnitude <= 0.0001f)
            return dir;

        return r.normalized;
    }

    private CharacterBehaviour GetAnchorCharacter(ActionSpawnAnchorType anchor)
    {
        switch (anchor)
        {
            case ActionSpawnAnchorType.Owner:
                return owner;

            case ActionSpawnAnchorType.Target:
                return owner.target != null ? owner.target : owner;

            case ActionSpawnAnchorType.NearestEnemy:
                {
                    var t = EntityContainer.Instance.GetNearestEnemy(owner);
                    return t != null ? t : owner;
                }

            case ActionSpawnAnchorType.NearestAlly:
                {
                    var t = EntityContainer.Instance.GetNearestAlly(owner);
                    return t != null ? t : owner;
                }
        }

        return owner;
    }

    private Vector2 ResolveFacingDir(CharacterBehaviour c)
    {
        // Action 재생 중에는 "이동 방향"을 바라봄으로 취급하지 않는다.
        // AutoCorrection으로 고정된 actionForward가 곧 Forward다.
        if (current != null)
        {
            if (actionForward.sqrMagnitude > 0.0001f)
                return actionForward.normalized;
            return Vector2.right;
        }

        if (c == null) return Vector2.right;

        if (c.lastVelocity.sqrMagnitude > 0.0001f)
            return c.lastVelocity.normalized;

        if (c.MoveDirection.sqrMagnitude > 0.0001f)
            return c.MoveDirection.normalized;

        if (c.sr != null)
            return c.sr.flipX ? Vector2.left : Vector2.right;

        return Vector2.right;
    }

    private Vector2 ResolveFacingDirNow()
    {
        // Action 재생 중에는 "현재 이동"을 바라봄으로 취급하지 않는다.
        if (current != null)
        {
            if (actionForward.sqrMagnitude > 0.0001f)
                return actionForward.normalized;
            return Vector2.right;
        }

        if (owner.lastVelocity.sqrMagnitude > 0.0001f)
            return owner.lastVelocity.normalized;

        if (owner.MoveDirection.sqrMagnitude > 0.0001f)
            return owner.MoveDirection.normalized;

        if (owner.sr != null)
            return owner.sr.flipX ? Vector2.left : Vector2.right;

        return Vector2.right;
    }
    private void SpawnActionVfx(ActionVfxEntry e)
    {
        if (e.vfxPrefab == null) return;

        Vector2 f2 = actionForward;
        if (f2.sqrMagnitude <= 0.0001f) f2 = Vector2.right;
        f2.Normalize();

        Vector2 r2 = new Vector2(-f2.y, f2.x);

        Vector3 basePos = owner.transform.position;
        Vector3 spawnPos = basePos + (Vector3)(r2 * e.offset.x + f2 * e.offset.y);

        var vfx = Object.Instantiate(e.vfxPrefab, spawnPos, Quaternion.identity);

        if (vfx.followType != VfxFollowType.None)
            vfx.Init(owner.transform, vfx.followType, vfx.duration, f2, e.offset, null);

        spawnedActionVfx.Add(vfx);
    }

    private void ClearActionVfx()
    {
        for (int i = 0; i < spawnedActionVfx.Count; i++)
        {
            var x = spawnedActionVfx[i];
            if (x.endBy == VfxEndBy.Manual)
                x.Release();
        }

        spawnedActionVfx.Clear();
    }

    ///
    private void ApplyAutoCorrection()
    {
        AutoCorrectionType t = current != null ? current.autoCorrectionType : AutoCorrectionType.Forward;

        Vector2 dir = ResolveFacingDirNow();

        if (t == AutoCorrectionType.ToTarget)
        {
            if (owner.target != null)
            {
                Vector2 d = (Vector2)(owner.target.transform.position - owner.transform.position);
                if (d.sqrMagnitude > 0.0001f) dir = d.normalized;
            }
        }
        else if (t == AutoCorrectionType.ToNearestEnemy)
        {
            var e = EntityContainer.Instance.GetNearestEnemy(owner);
            if (e != null)
            {
                Vector2 d = (Vector2)(e.transform.position - owner.transform.position);
                if (d.sqrMagnitude > 0.0001f) dir = d.normalized;
            }
        }
        else if (t == AutoCorrectionType.ToNearestAlly)
        {
            var a = EntityContainer.Instance.GetNearestAlly(owner);
            if (a != null)
            {
                Vector2 d = (Vector2)(a.transform.position - owner.transform.position);
                if (d.sqrMagnitude > 0.0001f) dir = d.normalized;
            }
        }
        // Forward는 ResolveFacingDirNow 그대로

        if (dir.sqrMagnitude <= 0.0001f)
            dir = Vector2.right;

        actionForward = dir.normalized;
        lastRefDir = actionForward;
    }

}
