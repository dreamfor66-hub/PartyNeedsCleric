using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EntityContainer : MonoBehaviour
{
    public static EntityContainer Instance { get; private set; }

    public List<CharacterBehaviour> Characters { get; private set; } = new List<CharacterBehaviour>();
    public PlayerCommander Commander { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public void Register(CharacterBehaviour c)
    {
        if (!Characters.Contains(c))
            Characters.Add(c);
    }


    public void SetCommander(PlayerCommander c)
    {
        Commander = c;
    }

    public void Unregister(CharacterBehaviour c)
    {
        Characters.Remove(c);
    }

    // owner와 다른 팀의 리스트 반환
    public List<CharacterBehaviour> GetAllEnemies(CharacterBehaviour owner)
    {
        List<CharacterBehaviour> list = new List<CharacterBehaviour>();

        foreach (var c in Characters)
        {
            if (c == owner) continue;
            if (c.state == CharacterState.Die) continue;
            if (c.team != owner.team)
                list.Add(c);
        }

        return list;
    }
    public List<CharacterBehaviour> GetEnemiesInRange(CharacterBehaviour owner, float range)
    {
        var result = new List<CharacterBehaviour>();
        if (owner == null) return result;
        if (range <= 0f) return result;

        float maxSqr = range * range;

        foreach (var c in Characters)
        {
            if (c == null) continue;
            if (c == owner) continue;
            if (c.state == CharacterState.Die) continue;
            if (c.team == owner.team) continue;

            float sqr = (c.transform.position - owner.transform.position).sqrMagnitude;
            if (sqr > maxSqr) continue;

            result.Add(c);
        }

        return result;
    }

    public List<CharacterBehaviour> GetAllAllies(CharacterBehaviour owner)
    {
        List<CharacterBehaviour> list = new List<CharacterBehaviour>();

        foreach (var c in Characters)
        {
            if (c == owner) continue;
            if (c.state == CharacterState.Die) continue;
            if (c.team == owner.team)
                list.Add(c);
        }

        return list;
    }

    public List<CharacterBehaviour> GetAlliesInRange(CharacterBehaviour owner, float range)
    {
        var result = new List<CharacterBehaviour>();
        if (owner == null) return result;
        if (range <= 0f) return result;

        float maxSqr = range * range;

        foreach (var c in Characters)
        {
            if (c == null) continue;
            if (c == owner) continue;
            if (c.state == CharacterState.Die) continue;
            if (c.team != owner.team) continue;

            float sqr = (c.transform.position - owner.transform.position).sqrMagnitude;
            if (sqr > maxSqr) continue;

            result.Add(c);
        }

        return result;
    }

    // 가장 가까운 적 반환
    public CharacterBehaviour GetNearestEnemy(CharacterBehaviour owner)
    {
        float best = float.MaxValue;
        CharacterBehaviour result = null;

        var list = GetAllEnemies(owner);

        foreach (var c in list.Where(x=>x.state != CharacterState.Die && x.data.targettable))
        {
            float sqr = (c.transform.position - owner.transform.position).sqrMagnitude;
            if (sqr < best)
            {
                best = sqr;
                result = c;
            }
        }

        return result;
    }

    public CharacterBehaviour GetNeaestEnemyInRange(CharacterBehaviour owner, float range)
    {
        if (range <= 0f) return null;

        float maxSqr = range * range;

        float best = float.MaxValue;
        CharacterBehaviour result = null;

        var list = GetAllEnemies(owner);

        foreach (var c in list.Where(x => x.state != CharacterState.Die))
        {
            float sqr = (c.transform.position - owner.transform.position).sqrMagnitude;
            if (sqr > maxSqr) continue;

            if (sqr < best)
            {
                best = sqr;
                result = c;
            }
        }

        return result;
    }

    public CharacterBehaviour GetNearestAlly(CharacterBehaviour owner)
    {
        float best = float.MaxValue;
        CharacterBehaviour result = null;

        var list = GetAllAllies(owner);

        foreach (var c in list.Where(x=>x.state != CharacterState.Die))
        {
            float sqr = (c.transform.position - owner.transform.position).sqrMagnitude;
            if (sqr < best)
            {
                best = sqr;
                result = c;
            }
        }

        return result;
    }

    public CharacterBehaviour GetNeaestAllyInRange(CharacterBehaviour owner, float range)
    {
        if (range <= 0f) return null;

        float maxSqr = range * range;

        float best = float.MaxValue;
        CharacterBehaviour result = null;

        var list = GetAllAllies(owner);

        foreach (var c in list.Where(x => x.state != CharacterState.Die))
        {
            float sqr = (c.transform.position - owner.transform.position).sqrMagnitude;
            if (sqr > maxSqr) continue;

            if (sqr < best)
            {
                best = sqr;
                result = c;
            }
        }

        return result;
    }


}
