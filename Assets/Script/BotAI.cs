using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class BotAI
{
    CharacterBehaviour owner;
    float bulletCooldownTimer = 0f;
    enum RangedState { Idle, Escape, Approach }
    RangedState rState = RangedState.Idle;

    float escapeAngle = 0f;
    float escapeGoalDist = 0f;
    float approachGoalDist = 0f;
    bool gotHit = false;
    Vector2 escapeTargetPoint;

    public BotAI(CharacterBehaviour o)
    {
        owner = o;
    }

    public void Update()
    {
        if (owner.target == null || owner.target.state == CharacterState.Die)
        {
            owner.target = EntityContainer.Instance.GetNearestEnemy(owner);
            owner.MoveDirection = Vector2.zero;
            return;
        }

        Vector2 toEnemy = owner.target.transform.position - owner.transform.position;
        float dist = toEnemy.magnitude;

        switch (owner.data.characterType)
        {
            case CharacterType.Melee:
                UpdateMelee(owner.target, toEnemy);
                break;

            case CharacterType.Ranged:
                UpdateRanged(owner.target, toEnemy, dist);
                break;

            case CharacterType.Totem:
                break;
        }
    }

    void UpdateMelee(CharacterBehaviour target, Vector2 toEnemy)
    {
        Vector2 targetDir = toEnemy.normalized;

        if (owner.MoveDirection == Vector2.zero)
        {
            owner.MoveDirection = targetDir;
            return;
        }

        owner.MoveDirection = Turn(owner.MoveDirection, targetDir);
    }

    void UpdateRanged(CharacterBehaviour target, Vector2 toEnemy, float dist)
    {
        float R = owner.data.rangeRadius;

        switch (rState)
        {
            // IDLE : 기본적으로 안 움직임
            case RangedState.Idle:
                {
                    Vector2 orbit = new Vector2(-toEnemy.y, toEnemy.x).normalized;

                    // orbitSign 적용 (양수=CCW, 음수=CW)
                    if (escapeAngle > 0f)
                        orbit = -orbit;

                    owner.MoveDirection = Turn(owner.MoveDirection, orbit);

                    if (dist > R)
                    {
                        rState = RangedState.Approach;
                        approachGoalDist = R;
                    }
                    break;
                }
            // ESCAPE : 피격 후 뒤로 빠지기
            case RangedState.Escape:
                {
                    Vector2 pos = owner.transform.position;

                    if (Vector2.Distance(pos, escapeTargetPoint) > 5f)
                    {
                        Vector2 dir = (escapeTargetPoint - pos).normalized;
                        owner.MoveDirection = Turn(owner.MoveDirection, dir, 1.5f);
                    }
                    else
                    {
                        rState = RangedState.Idle;
                    }
                    break;
                }
            // APPROACH : 적이 너무 멀리 갔을 때 접근
            case RangedState.Approach:
                {
                    if (dist > approachGoalDist)
                    {
                        Vector2 dir = toEnemy.normalized;
                        owner.MoveDirection = Turn(owner.MoveDirection, dir);
                    }
                    else
                    {
                        rState = RangedState.Idle;
                        owner.MoveDirection = Vector2.zero;
                    }
                    break;
                }
        }

        // ★ 이동 로직 끝난 뒤에 단 한 번만 호출
        HandleFire(target);
    }


    void HandleFire(CharacterBehaviour nearest)
    {
        bulletCooldownTimer -= Time.deltaTime;
        if (bulletCooldownTimer <= 0f)
        {
            owner.SpawnBullet(nearest, owner.data.baseBulletData);
            bulletCooldownTimer = owner.data.bulletCooldown;
        }
    }
    public void NotifyHit()
    {
        if (owner.target == null)
        {
            owner.target = EntityContainer.Instance.GetNearestEnemy(owner);
            owner.MoveDirection = Vector2.zero;
            return;
        }

        Vector2 toEnemy = owner.target.transform.position - owner.transform.position;

        rState = RangedState.Escape;
        escapeGoalDist = owner.data.rangeRadius;

        escapeAngle = Random.Range(-25f, 25f);

        Vector2 dir = Quaternion.Euler(0, 0, escapeAngle) * (-toEnemy.normalized);
        escapeTargetPoint = (Vector2)owner.target.transform.position + dir * escapeGoalDist;
    }

    Vector2 Turn(Vector2 cur, Vector2 tgt, float multiplier = 1f)
    {
        float s = GameVariables.Instance.MoveAngleSpeed * multiplier;
        if (cur == Vector2.zero) return tgt.normalized;

        float ang = Vector2.SignedAngle(cur, tgt);
        float rot = Mathf.Clamp(ang, -s * Time.deltaTime, s * Time.deltaTime);

        return (Quaternion.Euler(0, 0, rot) * cur).normalized;
    }
}
