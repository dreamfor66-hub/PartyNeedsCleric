using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class BuffInstance
{
    public readonly BuffData Data;
    public readonly CharacterBehaviour Owner;
    public readonly CharacterBehaviour Source;
    public readonly CharacterBehaviour Root;
    public int CreatedFrame { get; private set; }   // ← private set으로 변경

    public int Stacks { get; private set; } = 1;
    public float TimeLeft { get; private set; } = -1f;

    private readonly Dictionary<int, float> everySecondAcc = new(); // 추가 (ability index별 누적)

    private Vector2 lastPos;
    private readonly Dictionary<int, float> everyMoveAcc = new();

    private int triggeredCount = 0;

    public BuffInstance(BuffData data, CharacterBehaviour owner, int stacks)
    {
        Data = data;
        Owner = owner;
        Stacks = Mathf.Max(1, stacks);
        CreatedFrame = Time.frameCount;
        
        lastPos = owner.transform.position;

        if (data.removeCondition == BuffRemoveCondition.Duration)
            TimeLeft = data.duration;
    }

    public void AddStack(int amount)
    {
        if (amount <= 0) return;

        for (int i = 0; i < amount; i++)
        {
            if (Stacks >= Data.maxStacks)
                break;

            Stacks++;

            if (Data.removeCondition == BuffRemoveCondition.Duration)
                TimeLeft = Data.duration;

            // 스택 증가 이벤트는 "스택마다 1번"만 던지고,
            // 각 어빌리티의 interval(triggerCount) 판정은 InternalDispatch에서 처리한다.
            Owner.Buffs.InternalDispatch(BuffTrigger.OnEachStack, MakeCtx());
        }
    }
    public void RemoveStacks(int amount)
    {
        if (amount <= 0) return;

        Stacks -= amount;
        if (Stacks <= 0)
        {
            Stacks = 0;
            Owner.Buffs.RemoveInstance(this);
        }
    }

    public void RefreshDuration()
    {
        if (Data.removeCondition == BuffRemoveCondition.Duration)
            TimeLeft = Data.duration;
        // 갱신해도 CreatedFrame은 그대로 유지한다.
    }

    public bool Tick(float dt)
    {
        if (Data.removeCondition == BuffRemoveCondition.Duration)
        {
            TimeLeft -= dt;
            if (TimeLeft <= 0f)
            {
                Owner.Buffs.InternalDispatch(BuffTrigger.OnBuffEnd, MakeCtx());
                return true;
            }
        }

        // EverySecond: 어빌리티별로 누적 시간을 따로 굴린다.
        var abilities = Data.abilities;
        if (abilities != null)
        {
            for (int i = 0; i < abilities.Count; i++)
            {
                var a = abilities[i];
                if (a.trigger != BuffTrigger.EverySecond) continue;

                float interval = Mathf.Max(0.0001f, (float)a.triggerCount);

                everySecondAcc.TryGetValue(i, out float acc);
                acc += dt;

                if (acc >= interval)
                {
                    int times = Mathf.FloorToInt(acc / interval);
                    acc -= times * interval;

                    for (int t = 0; t < times; t++)
                    {
                        Owner.Buffs.ExecuteAbility(a, MakeCtx());
                        OnTriggered(); // 발동 1회당 TriggerCount 누적
                    }
                }

                everySecondAcc[i] = acc;
            }
        }

        Vector2 curPos = Owner.transform.position;
        float moved = Vector2.Distance(curPos, lastPos);
        lastPos = curPos;

        var abilities2 = Data.abilities;
        if (abilities2 != null)
        {
            for (int i = 0; i < abilities2.Count; i++)
            {
                var a = abilities2[i];
                if (a.trigger != BuffTrigger.EveryMove) continue;

                int interval = Mathf.Max(1, a.triggerCount); // "Unit 거리"
                everyMoveAcc.TryGetValue(i, out float acc);
                acc += moved;

                if (acc >= interval)
                {
                    int times = Mathf.FloorToInt(acc / interval);
                    acc -= times * interval;

                    for (int t = 0; t < times; t++)
                    {
                        Owner.Buffs.ExecuteAbility(a, MakeCtx());
                        OnTriggered();
                    }
                }

                everyMoveAcc[i] = acc;
            }
        }

        return false;
    }
    public void OnTriggered()
    {
        if (Data == null || Data.removeCondition != BuffRemoveCondition.TriggerCount)
            return;

        triggeredCount++;
        int required = Mathf.Max(1, Data.triggerCount);

        if (triggeredCount >= required)
        {
            Owner.Buffs.RemoveInstance(this);
        }
    }


    private BuffEventContext MakeCtx() => new BuffEventContext
    {
        Owner = Owner,
        ThisBuff = this
    };
}
