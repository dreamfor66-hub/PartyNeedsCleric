using UnityEngine;
using System.Collections;

public class FSMAI
{
    CharacterBehaviour owner;
    FSMData data;

    FSMStateEntry curState;
    int curIndex;

    // ===========================
    // BotAI RANGE INTERNAL STATE 
    // (BotAI와 100% 동일한 변수)
    // ===========================
    enum RangedState { Idle, Escape, Approach }
    RangedState rState = RangedState.Idle;

    float escapeAngle = 0f;
    float escapeGoalDist = 0f;
    float approachGoalDist = 0f;
    Vector2 escapeTargetPoint;

    // ===========================
    // RUSH 
    // ===========================
    bool rushLock = false;
    Vector2 rushDir;

    public FSMAI(CharacterBehaviour o, FSMData d)
    {
        owner = o;
        data = d;
    }

    public void StartFSM()
    {
        curIndex = data.fsmList[0].index;
        curState = data.GetState(curIndex);
        owner.StartCoroutine(FSMCoroutine());
    }

    IEnumerator FSMCoroutine()
    {
        while (owner.state != CharacterState.Die && curState != null)
        {
            EnterState(curState);

            float timer = curState.duration;
            rushLock = false;

            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                owner.MoveDirection = CalcMove(curState.type);
                yield return null;
            }

            ExitState(curState);
            curIndex = curState.next;
            curState = data.GetState(curIndex);
        }
    }

    // ===========================
    // ENTER / EXIT
    // ===========================

    void EnterState(FSMStateEntry s)
    {
        if (s.onStartExtra >= 0)
            RunExtra(s.onStartExtra);

        owner.isCastingFSM = (s.type == FSMActionType.Casting);

        if (s.type == FSMActionType.Rush)
        {
            if (owner.target == null)
                owner.target = FindTarget();
            rushDir = (owner.target.transform.position - owner.transform.position).normalized;

            rushLock = true;
        }
    }

    void ExitState(FSMStateEntry s)
    {
        if (s.onEndExtra >= 0)
            RunExtra(s.onEndExtra);
    }

    // ===========================
    // MOVE CALCULATION
    // ===========================

    Vector2 CalcMove(FSMActionType t)
    {
        switch (t)
        {
            case FSMActionType.Casting:
                return Vector2.zero;

            case FSMActionType.Melee:
                return Move_Melee();

            case FSMActionType.Range:
                return Move_Ranged();

            case FSMActionType.Rush:
                return Move_Rush();
        }
        return Vector2.zero;
    }


    // ===========================================================
    // MELEE (BotAI의 Melee 로직 100% 유지)
    // ===========================================================

    Vector2 Move_Melee()
    {
        if (owner.target == null)
            owner.target = FindTarget();
        if (owner.target == null) return Vector2.zero;

        Vector2 toEnemy = owner.target.transform.position - owner.transform.position;
        Vector2 targetDir = toEnemy.normalized;

        if (owner.MoveDirection == Vector2.zero)
            return targetDir;

        return Turn(owner.MoveDirection, targetDir);
    }


    // ===========================================================
    // RANGED (BotAI의 RangedState + Idle/Escape/Approach 완벽 복제)
    // ===========================================================

    Vector2 Move_Ranged()
    {
        if (owner.target == null)
            owner.target = FindTarget();
        if (owner.target == null) return Vector2.zero;

        Vector2 toEnemy = owner.target.transform.position - owner.transform.position;
        float dist = toEnemy.magnitude;
        float R = owner.data.rangeRadius;

        switch (rState)
        {
            case RangedState.Idle:
                {
                    Vector2 orbit = new Vector2(-toEnemy.y, toEnemy.x).normalized;

                    // BotAI와 동일하게 escapeAngle > 0이면 방향 반전
                    if (escapeAngle > 0f)
                        orbit = -orbit;

                    Vector2 newDir = Turn(owner.MoveDirection, orbit);

                    if (dist > R)
                    {
                        rState = RangedState.Approach;
                        approachGoalDist = R;
                    }

                    return newDir;
                }

            case RangedState.Escape:
                {
                    Vector2 pos = owner.transform.position;

                    if (Vector2.Distance(pos, escapeTargetPoint) > 5f)
                    {
                        Vector2 dir = (escapeTargetPoint - pos).normalized;
                        return Turn(owner.MoveDirection, dir, 1.5f);
                    }
                    else
                    {
                        rState = RangedState.Idle;
                        return owner.MoveDirection;
                    }
                }

            case RangedState.Approach:
                {
                    if (dist > approachGoalDist)
                    {
                        Vector2 dir = toEnemy.normalized;
                        return Turn(owner.MoveDirection, dir);
                    }
                    else
                    {
                        rState = RangedState.Idle;
                        return Vector2.zero;
                    }
                }
        }

        return Vector2.zero;
    }


    // ===========================================================
    // RUSH
    // ===========================================================

    Vector2 Move_Rush()
    {
        if (rushLock) return rushDir;
        return rushDir;
    }


    // ===========================================================
    // SUPPORT
    // ===========================================================

    CharacterBehaviour FindTarget()
    {
        return EntityContainer.Instance.GetNearestEnemy(owner);
    }

    Vector2 Turn(Vector2 cur, Vector2 tgt, float mult = 1f)
    {
        float s = GameVariables.Instance.MoveAngleSpeed * mult;

        if (cur == Vector2.zero)
            return tgt.normalized;

        float ang = Vector2.SignedAngle(cur, tgt);
        float rot = Mathf.Clamp(ang, -s * Time.deltaTime, s * Time.deltaTime);

        return (Quaternion.Euler(0, 0, rot) * cur).normalized;
    }

    // ===========================================================
    // EXTRA ACTIONS
    // ===========================================================

    void RunExtra(int id)
    {
        var ex = data.GetExtra(id);
        if (ex == null) return;

        if (ex.useAnim)
            owner.PlayExtraAnim(ex.animSpriteFront, ex.animSpriteBack, ex.animDuration);

        switch (ex.type)
        {
            case ExtraActionType.Search:
                owner.target = FindTarget();
                break;

            case ExtraActionType.AddBuff:
                Do_AddBuff(ex);
                break;

            case ExtraActionType.SpawnBullet:
                Do_SpawnBullet(ex);
                break;
            case ExtraActionType.SpawnCharacter:
                Do_SpawnCharacter(ex);
                break;

        }
    }

    void Do_AddBuff(ExtraAction ex)
    {
        switch (ex.effTarget)
        {
            case ExtraAction.TargetSelector.Self:
                owner.Buffs.AddBuff(ex.buff, ex.buffStacks);
                break;

            case ExtraAction.TargetSelector.CurrentTarget:
                if (owner.target != null)
                    owner.target.Buffs.AddBuff(ex.buff, ex.buffStacks);
                break;

            case ExtraAction.TargetSelector.NearestEnemy:
                var n = FindTarget();
                if (n != null) n.Buffs.AddBuff(ex.buff, ex.buffStacks);
                break;

            case ExtraAction.TargetSelector.EnemyInRange:
                foreach (var c in EntityContainer.Instance.Characters)
                    if (c.team != owner.team &&
                        Vector2.Distance(owner.transform.position, c.transform.position) <= ex.range)
                        c.Buffs.AddBuff(ex.buff, ex.buffStacks);
                break;

            case ExtraAction.TargetSelector.AllEnemy:
                foreach (var c in EntityContainer.Instance.Characters)
                    if (c.team != owner.team)
                        c.Buffs.AddBuff(ex.buff, ex.buffStacks);
                break;

            case ExtraAction.TargetSelector.AllAlly:
                foreach (var c in EntityContainer.Instance.Characters)
                    if (c.team == owner.team)
                        c.Buffs.AddBuff(ex.buff, ex.buffStacks);
                break;
        }
    }

    void Do_SpawnBullet(ExtraAction ex)
    {
        Vector2 baseDir = Vector2.up;

        switch (ex.bulletDir)
        {
            case ExtraAction.BulletDir.ToNearestEnemy:
                var n = FindTarget();
                if (n != null)
                    baseDir = (n.transform.position - owner.transform.position).normalized;
                break;

            case ExtraAction.BulletDir.ToCurrentTarget:
                if (owner.target != null)
                    baseDir = (owner.target.transform.position - owner.transform.position).normalized;
                break;

            case ExtraAction.BulletDir.Up:
                baseDir = Vector2.up;
                break;
        }

        float ang = Random.Range(ex.angleOffsetRange.x, ex.angleOffsetRange.y);
        Vector2 dir = Quaternion.Euler(0, 0, ang) * baseDir;

        Vector3 spawnPos =
            owner.transform.position +
            (Vector3)(dir.normalized * ex.offset.y) +
            new Vector3(ex.offset.x, 0, 0);

        var b = Object.Instantiate(
            BulletSpawner.Instance.bulletPrefab,
            spawnPos,
            Quaternion.identity);

        // 방향 회전
        b.transform.right = dir;

        // ★★ 수정된 Init 호출 (3개 인자)
        b.Init(ex.bullet, owner, dir, owner.team);
    }

    void Do_SpawnCharacter(ExtraAction ex)
    {
        if (ex.characterData == null) return;

        CharacterBehaviour baseChar = owner;

        Vector2 forward = baseChar.MoveDirection == Vector2.zero
            ? (Vector2)baseChar.transform.up
            : baseChar.MoveDirection.normalized;

        Vector2 right = new Vector2(forward.y, -forward.x);

        Vector3 basePos =
            baseChar.transform.position +
            (Vector3)(forward * ex.offset.y) +
            (Vector3)(right * ex.offset.x);

        for (int i = 0; i < ex.spawnCount; i++)
        {
            Vector2 rnd = ex.range > 0
                ? Random.insideUnitCircle * ex.range
                : Vector2.zero;

            Vector3 spawnPos = basePos + (Vector3)rnd;

            var c = Object.Instantiate(
                CharacterSpawner.Instance.characterPrefab,
                spawnPos,
                Quaternion.identity
            );

            c.Init(ex.characterData, owner.team);
        }
    }

}
