using UnityEngine;

public class CharacterMove
{
    CharacterBehaviour owner;
    Rigidbody2D rb;

    public CharacterMove(CharacterBehaviour o)
    {
        owner = o;
        rb = owner.rb;
    }

    public void Update()
    {
        if (owner.state == CharacterState.Move)
        {
            Vector2 dir = owner.MoveDirection;
            float spd = owner.GetSpeed();      // 여기
            rb.linearVelocity = dir * spd;
            return;
        }

        if (owner.state == CharacterState.Knockback)
        {
            owner.knockbackTime += Time.deltaTime; // 추가

            bool slow = rb.linearVelocity.sqrMagnitude < 0.1f;
            bool timeout = owner.knockbackTime >= GameVariables.Instance.knockbackDuration; // 추가

            if (slow || timeout)
            {
                owner.MoveDirection = owner.lastVelocity.normalized;
                //rb.linearVelocity = Vector2.zero;
                owner.knockbackTime = 0f; // 초기화
                owner.ChangeState(CharacterState.Search);
            }
            return;
        }
    }
}
