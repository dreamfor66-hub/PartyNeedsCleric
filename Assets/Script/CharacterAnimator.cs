using UnityEngine;

public class CharacterAnimator
{
    CharacterBehaviour owner;
    SpriteRenderer sr;

    float attackTimer = 0f;
    float extraTimer = 0f;
    //저장용
    Sprite extraFront;
    Sprite extraBack;

    public CharacterAnimator(CharacterBehaviour o, SpriteRenderer sr)
    {
        owner = o;
        this.sr = sr;
    }

    public void PlayAttack()
    {
        attackTimer = GameVariables.Instance.attackAnimTime;

        var spr = owner.data.sprites;
        Vector2 v = owner.rb.linearVelocity;
        bool up = v.y >= 0;
        sr.sprite = up ? spr.anim_attackBack : spr.anim_attackFront;
    }
    public void PlayExtra(Sprite sprFront, Sprite sprBack, float duration)
    {
        extraTimer = duration;

        Vector2 v = owner.rb.linearVelocity;
        bool up = v.y >= 0;
        sr.sprite = up ? sprBack : sprFront;
    }

    public void Update()
    {
        var spr = owner.data.sprites;

        if (owner.state == CharacterState.Die)
        {
            sr.sprite = spr.anim_die;
            return;
        }

        // 이동 방향 계산
        Vector2 v = owner.rb.linearVelocity;
        bool up = v.y >= 0;
        
        if (v.x <= 0) sr.flipX = true;
        else if (v.x > 0) sr.flipX = false;

        // === 넉백 상태면 좌우/상하 모두 반전 ===
        if (owner.state is CharacterState.Knockback)
        {
            up = !up;

            sr.flipX = !sr.flipX;
        }
        if (owner.state == CharacterState.Action)
        {
            sr.sprite = up ? spr.anim_attackBack : spr.anim_attackFront;
            return;
        }
        if (extraTimer > 0f)
        {
            extraTimer -= Time.deltaTime;
            sr.sprite = up ? extraBack : extraFront;
            return;
        }

        // 공격
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
            sr.sprite = up ? spr.anim_attackBack : spr.anim_attackFront;
            return;
        }

        if (owner.MoveDirection == Vector2.zero)
            up = false;

        // idle / move / knockback
        sr.sprite = up ? spr.anim_idleBack : spr.anim_idleFront;
    }
}
