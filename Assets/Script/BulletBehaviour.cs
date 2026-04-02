using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class BulletBehaviour : MonoBehaviour
{
    [SerializeField] private BulletData data;
    public BulletData Data => data;

    CharacterBehaviour owner;
    TeamType team;

    Vector3 direction;
    Vector3 orbitCenter;
    float baseAngle = 0f;
    float targetBaseAngle = 0f;

    float timer;
    float hitStopTimer;
    float lifetime;
    Vector3 targetPoint;
    bool hasTargetPoint;
    bool reachedTargetPoint;

    float homingDetectAngle;
    AnimationCurve homingRotateCurve;

    int maxPierceCount;
    int pierceCount;

    int currentFrame = 0;

    // groupId + target → re-hit cooldown
    Dictionary<(int, CharacterBehaviour), float> rehitTimers = new();

    // frame / hit / end reactions 체크
    HashSet<BulletReactionEntry> triggeredReactions = new();

    // group trigger 관리 (불필요한 hit 중복 방지)
    Dictionary<int, HashSet<CharacterBehaviour>> hitGroupTriggered = new();
    Dictionary<int, float> hitGroupResetTimers = new();

    private readonly List<VfxObject> spawnedBulletVfx = new();


    // ============================================================
    // INIT
    // ============================================================
    // BulletBehaviour.cs
    public void Init(BulletData data, CharacterBehaviour owner, Vector3 shootDir, TeamType teamType)
    {
        Init(data, owner, shootDir, teamType, Vector3.zero, false);
    }
    public void Init(BulletData data, CharacterBehaviour owner, Vector3 shootDir, TeamType teamType, Vector3 targetPoint)
    {
        Init(data, owner, shootDir, teamType, targetPoint, true);
    }

    void Init(BulletData data, CharacterBehaviour owner, Vector3 shootDir, TeamType teamType, Vector3 targetPoint, bool useTargetPoint)
    {
        this.data = data;
        this.owner = owner;
        this.team = teamType;

        direction = shootDir.normalized;
        timer = 0f;
        hitStopTimer = 0f;
        currentFrame = 0;
        pierceCount = 0;

        this.hasTargetPoint = useTargetPoint;
        this.targetPoint = targetPoint;
        reachedTargetPoint = false;

        homingDetectAngle = data.homingDetectAngle;
        homingRotateCurve = new AnimationCurve(data.homingRotateCurve.keys);
        maxPierceCount = data.maxPierceCount;

        rehitTimers.Clear();
        hitGroupTriggered.Clear();
        triggeredReactions.Clear();
        hitGroupResetTimers.Clear();

        spawnedBulletVfx.Clear();

        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null && data.sprite != null)
            sr.sprite = data.sprite;

        GlobalSpriteSorter.Instance.RegisterRenderer(sr);

        lifetime = 0f;
        if ((data.despawnBy & BulletDespawnBy.LifeTime) != 0)
            lifetime = data.lifeTime;

        if (data.type == BulletType.Orbit)
        {
            orbitCenter = owner.transform.position;
            baseAngle = 0f;
            targetBaseAngle = 0f;
            RegisterOrbitBullet();
        }
    }
    // ============================================================
    // UPDATE
    // ============================================================
    void Update()
    {
        if (data == null)
            return;

        if (hitStopTimer > 0f)
        {
            hitStopTimer -= Time.deltaTime;
            if (hitStopTimer <= 0f)
            {
                hitStopTimer = 0f;
                SetManualBulletVfxPaused(false);
            }
            return;
        }

        UpdateRehitTimers();
        BulletMove();

        timer += Time.deltaTime;
        if ((data.despawnBy & BulletDespawnBy.LifeTime) != 0)
        {
            if (timer >= lifetime)
            {
                ProcessReactions(ReactionTriggerType.LifeTime);
                Despawn();
                return;
            }
        }

        if (reachedTargetPoint && (data.despawnBy & BulletDespawnBy.TargetPointReached) != 0)
        {
            Despawn();
            return;
        }

        ProcessHitBoxes();
        ProcessReactions(ReactionTriggerType.Frame);

        Collider2D wall = Physics2D.OverlapCircle(transform.position, data.radius, LayerMask.GetMask("Ground"));
        if (wall != null)
        {
            ProcessReactions(ReactionTriggerType.CollideMap);
            if ((data.despawnBy & BulletDespawnBy.CollideMap) != 0)
            {
                Despawn();
                return;
            }
        }

        currentFrame++;
    }


    // ============================================================
    // BULLET MOVE
    // ============================================================
    void BulletMove()
    {
        switch (data.type)
        {
            case BulletType.Normal:
                {
                    if (data.homingRotateCurve != null)
                    {
                        CharacterBehaviour nearest = null;
                        float nearestAngle = float.MaxValue;

                        foreach (var t in EntityContainer.Instance.GetAllEnemies(owner))
                        {
                            if (t == null || t.team == team) continue;

                            Vector2 toTarget = t.transform.position - transform.position;
                            float ang = Vector2.Angle(direction, toTarget);
                            if (ang < homingDetectAngle && ang < nearestAngle)
                            {
                                nearest = t;
                                nearestAngle = ang;
                            }
                        }

                        if (nearest != null)
                        {
                            float homingT = lifetime > 0f ? Mathf.Clamp01(timer / lifetime) : 0f;
                            float rotDeg = homingRotateCurve.Evaluate(homingT);

                            Vector2 desired = (nearest.transform.position - transform.position);
                            desired.Normalize();

                            Vector2 cur2d = new(direction.x, direction.y);
                            Vector2 new2d = Vector2.Lerp(cur2d, desired, rotDeg * Time.deltaTime).normalized;

                            direction = new Vector3(new2d.x, new2d.y, 0f);
                        }
                    }

                    float curSpeed = data.speed;
                    if (Mathf.Abs(data.accel) > 0.0001f)
                    {
                        curSpeed += data.accel * timer;
                        if (curSpeed < 0f) curSpeed = 0f;
                    }

                    transform.position += direction * curSpeed * Time.deltaTime;

                    if (direction.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.FromToRotation(Vector3.right, direction);

                    break;
                }

            case BulletType.Orbit:
                {
                    if (owner == null) return;

                    orbitCenter = Vector3.MoveTowards(orbitCenter, owner.transform.position, data.followSpeed * Time.deltaTime);

                    baseAngle = Mathf.LerpAngle(baseAngle, targetBaseAngle, Time.deltaTime);

                    float phase = Mathf.Repeat(Time.time * data.speed, 360f);

                    float finalAngle = baseAngle + phase;

                    Vector3 prev = transform.position;
                    Vector3 orbitPos = orbitCenter + Quaternion.Euler(0, 0, finalAngle) * Vector3.right * data.orbitRange;
                    transform.position = orbitPos;

                    Vector3 moveDir = orbitPos - prev;
                    if (moveDir.sqrMagnitude > 0.001f)
                    {
                        moveDir.Normalize();
                        transform.rotation = Quaternion.FromToRotation(Vector3.right, moveDir);
                        direction = moveDir;
                    }

                    break;
                }

            case BulletType.ToTargetPoint:
                {
                    if (!hasTargetPoint)
                    {
                        float curSpeed = data.speed;
                        if (Mathf.Abs(data.accel) > 0.0001f)
                        {
                            curSpeed += data.accel * timer;
                            if (curSpeed < 0f) curSpeed = 0f;
                        }

                        transform.position += direction * curSpeed * Time.deltaTime;

                        if (direction.sqrMagnitude > 0.001f)
                            transform.rotation = Quaternion.FromToRotation(Vector3.right, direction);

                        break;
                    }

                    if (reachedTargetPoint)
                    {
                        transform.position = targetPoint;
                        break;
                    }

                    Vector3 toTarget = targetPoint - transform.position;
                    toTarget.z = 0f;

                    if (toTarget.sqrMagnitude <= 0.0001f)
                    {
                        transform.position = targetPoint;
                        reachedTargetPoint = true;
                        direction = Vector3.zero;
                        break;
                    }

                    Vector3 moveDir = toTarget.normalized;

                    float curMoveSpeed;

                    if (data.arriveByDuration)
                    {
                        float remainTime = Mathf.Max(0.0001f, data.arriveDuration - timer);
                        curMoveSpeed = toTarget.magnitude / remainTime;
                    }
                    else
                    {
                        curMoveSpeed = data.speed;
                        if (Mathf.Abs(data.accel) > 0.0001f)
                        {
                            curMoveSpeed += data.accel * timer;
                            if (curMoveSpeed < 0f) curMoveSpeed = 0f;
                        }
                    }

                    float moveDist = curMoveSpeed * Time.deltaTime;
                    float remainDist = toTarget.magnitude;

                    if (moveDist >= remainDist || (data.arriveByDuration && timer >= data.arriveDuration))
                    {
                        transform.position = targetPoint;
                        direction = moveDir;
                        transform.rotation = Quaternion.FromToRotation(Vector3.right, moveDir);
                        reachedTargetPoint = true;
                    }
                    else
                    {
                        transform.position += moveDir * moveDist;
                        direction = moveDir;
                        transform.rotation = Quaternion.FromToRotation(Vector3.right, moveDir);
                    }

                    break;
                }
        }
    }



    // ============================================================
    // HIT LOGIC
    // ============================================================
    void UpdateRehitTimers()
    {
        if (rehitTimers.Count == 0) return;

        var keys = new List<(int, CharacterBehaviour)>(rehitTimers.Keys);
        List<(int, CharacterBehaviour)> expired = null;

        foreach (var key in keys)
        {
            float remain = rehitTimers[key];
            if (data.rehitInterval > 0f)
            {
                remain -= Time.deltaTime;
                if (remain <= 0f)
                {
                    expired ??= new();
                    expired.Add(key);
                }
                else rehitTimers[key] = remain;
            }
        }

        if (expired != null)
            foreach (var key in expired)
                rehitTimers.Remove(key);
    }

    bool CanHitTarget(int groupId, CharacterBehaviour target)
    {
        var key = (groupId, target);

        if (rehitTimers.ContainsKey(key))
            return false;

        rehitTimers[key] = data.rehitInterval;
        return true;
    }

    // BulletBehaviour.cs : ProcessHitBoxes() 교체
    void ProcessHitBoxes()
    {
        if (data.hitBoxes == null || data.hitBoxes.Count == 0)
            return;

        float pendingBulletHitStop = 0f;

        foreach (var hb in data.hitBoxes)
        {
            if (currentFrame < hb.startFrame || currentFrame > hb.endFrame)
                continue;

            Vector3 basePos = transform.position;

            Vector3 dir = direction;
            dir.z = 0f;
            if (dir.sqrMagnitude <= 0.0001f) continue;
            dir.Normalize();

            Vector3 right = new(dir.y, -dir.x, 0f);
            Vector3 center = basePos + right * hb.offset.x + dir * hb.offset.y;

            foreach (var t in EntityContainer.Instance.Characters)
            {
                if (t == null || t.team == team) continue;
                if (t.state == CharacterState.Die) continue;

                float dist = Vector3.Distance(center, t.transform.position);
                if (dist > t.col.radius + hb.radius)
                    continue;

                if (!CanHitTarget(hb.hitBoxGroup, t))
                    continue;

                float hitStop = ApplyHit(t);
                if (hitStop > pendingBulletHitStop)
                    pendingBulletHitStop = hitStop;

                pierceCount++;

                ProcessReactions(ReactionTriggerType.Hit);

                if (data.maxPierceCount <= 0 || pierceCount >= data.maxPierceCount)
                {
                    if ((data.despawnBy & BulletDespawnBy.Hit) != 0)
                    {
                        Despawn();
                        return;
                    }

                    break;
                }
            }
        }

        if (pendingBulletHitStop > 0f)
            ApplyBulletHitStop(pendingBulletHitStop);
    }

    // BulletBehaviour.cs : ApplyHit(CharacterBehaviour target) 교체

    float ApplyHit(CharacterBehaviour target)
    {
        int baseDamage = Mathf.RoundToInt(owner.GetAttackValue() * data.hitData.attackMultiplier);
        int enhancedDamage = owner.ApplyDamageEnhance(baseDamage, HitFilter.Bullet, target);

        HitContext hitCtx = new()
        {
            Attacker = owner,
            Target = target,
            Damage = enhancedDamage,
            Ignore = false
        };
        hitCtx.Damage = owner.ApplyDamageEnhance(hitCtx.Damage, HitFilter.Bullet, target);

        owner.Buffs.ApplyInstantHitEffects(
            BuffTrigger.OwnerHit,
            new BuffEventContext
            {
                Owner = owner,
                HitSource = owner,
                HitTarget = target,
                Hit = hitCtx,
                Damage = hitCtx.Damage,
                HitFlags = HitFilter.Bullet
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
                HitFlags = HitFilter.Bullet,
            }
        );

        if (hitCtx.Ignore)
            return 0f;

        target.ApplyDamage(owner, hitCtx.Damage);

        for (int i = 0; i < data.hitData.hitApplyBuffs.Count; i++)
        {
            var e = data.hitData.hitApplyBuffs[i];
            for (int c = 0; c < e.count; c++)
                target.Buffs.AddBuff(e.buff);
        }

        HitFxTable.Instance.Spawn(data.hitData.hitFxType, target.transform.position);

        owner.Buffs.NotifyOwnerHit(target, hitCtx.Damage, HitFilter.Bullet, hitCtx);
        target.Buffs.NotifyOwnerHurt(owner, hitCtx.Damage, HitFilter.Bullet, hitCtx);

        float hs = data.hitData.hitStop;
        if (hs > 0f)
            target.ApplyHitStop(hs);

        if (!target.data.isBoss)
        {
            Vector2 dir = data.hitData.GetKnockbackDirection(
                owner.transform.position,
                transform.position,
                target.transform.position
            );

            target.ApplyKnockback(dir, data.hitData.knockbackPower);
            target.lastHitData = data.hitData;
        }

        return hs;
    }

    // ============================================================
    // REACTIONS
    // ============================================================
    void ProcessReactions(ReactionTriggerType trigger)
    {
        if (data.reactions == null || data.reactions.Count == 0)
            return;

        foreach (var r in data.reactions)
        {
            if (triggeredReactions.Contains(r))
                continue;

            if (r.triggerType == ReactionTriggerType.Frame && trigger == ReactionTriggerType.Frame)
            {
                if (Mathf.Approximately(currentFrame, r.value))
                    ExecuteReaction(r);
            }
            else if (r.triggerType == trigger)
            {
                ExecuteReaction(r);
            }
        }
    }

    void ExecuteReaction(BulletReactionEntry r)
    {
        triggeredReactions.Add(r);

        switch (r.reactionType)
        {
            case ReactionType.SpawnBullet:
                {
                    if (r.bulletData == null || owner == null)
                        return;

                    Vector3 forward = direction;

                    float randomAngle = 0f;
                    if (r.angleOffsetRange != Vector2.zero)
                        randomAngle = Random.Range(r.angleOffsetRange.x, r.angleOffsetRange.y);

                    Vector3 finalDir =
                        (Quaternion.AngleAxis(-randomAngle, Vector3.forward) * forward).normalized;

                    var newBullet = Instantiate(
                        BulletSpawner.Instance.bulletPrefab,
                        transform.position,
                        Quaternion.FromToRotation(Vector3.right, finalDir)
                    );

                    newBullet.Init(r.bulletData, owner, finalDir, team);
                    owner.Buffs.NotifyOwnerSpawnBullet(newBullet);

                    break;
                }

            case ReactionType.SpawnVfx:
                {
                    if (r.vfxPrefab == null) return;

                    Vector2 f2 = new Vector2(direction.x, direction.y);
                    if (f2.sqrMagnitude <= 0.0001f) f2 = Vector2.right;
                    f2.Normalize();

                    Vector2 r2 = new Vector2(-f2.y, f2.x);

                    Vector3 basePos = transform.position;
                    Vector3 spawnPos = basePos + (Vector3)(r2 * r.offset.x + f2 * r.offset.y);

                    var vfx = Instantiate(r.vfxPrefab, spawnPos, Quaternion.identity);
                    vfx.Init(transform, vfx.followType, vfx.duration, f2, r.offset, null);

                    spawnedBulletVfx.Add(vfx);
                    break;
                }
        }
    }


    // ============================================================
    // Despawn
    // ============================================================
    void Despawn()
    {
        ProcessReactions(ReactionTriggerType.Despawn);

        for (int i = 0; i < spawnedBulletVfx.Count; i++)
        {
            if (spawnedBulletVfx[i] != null && spawnedBulletVfx[i].endBy == VfxEndBy.Manual)
                spawnedBulletVfx[i].Release();
        }
        spawnedBulletVfx.Clear();

        if (data.type == BulletType.Orbit)
            UnregisterOrbitBullet();

        Destroy(gameObject);
    }


    // ============================================================
    // ORBIT REGISTER
    // ============================================================
    void RegisterOrbitBullet()
    {
        List<BulletBehaviour> group = new();
        foreach (var b in FindObjectsByType<BulletBehaviour>(FindObjectsSortMode.None))
        {
            if (b != null && b != this && b.owner == owner &&
                b.data != null && b.data.type == BulletType.Orbit &&
                b.data.orbitId == data.orbitId)
            {
                group.Add(b);
            }
        }
        group.Add(this);
        group.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));

        int n = group.Count;
        float step = 360f / n;

        for (int i = 0; i < n; i++)
            group[i].targetBaseAngle = step * i;
    }

    void UnregisterOrbitBullet()
    {
        List<BulletBehaviour> group = new();
        foreach (var b in FindObjectsByType<BulletBehaviour>(FindObjectsSortMode.None))
        {
            if (b != null && b != this && b.owner == owner &&
                b.data != null && b.data.type == BulletType.Orbit &&
                b.data.orbitId == data.orbitId)
            {
                group.Add(b);
            }
        }

        int n = group.Count;
        if (n == 0) return;

        group.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));

        float step = 360f / n;
        for (int i = 0; i < n; i++)
            group[i].targetBaseAngle = step * i;
    }

    void ApplyBulletHitStop(float duration)
    {
        if (duration <= 0f)
            return;

        if (hitStopTimer < duration)
            hitStopTimer = duration;

        SetManualBulletVfxPaused(true);
    }

    void SetManualBulletVfxPaused(bool paused)
    {
        for (int i = 0; i < spawnedBulletVfx.Count; i++)
        {
            var vfx = spawnedBulletVfx[i];
            if (vfx == null) continue;
            if (vfx.endBy != VfxEndBy.Manual) continue;

            vfx.SetPaused(paused);
        }
    }
    // ============================================================
    // GIZMOS
    // ============================================================
    void OnDrawGizmosSelected()
    {
        if (data != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, data.radius);
        }
    }
}
