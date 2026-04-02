
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HitData
{
    public string hitId = "1";
    public HitFilter hitFilter;
    public HitFxType hitFxType = HitFxType.Brawl;

    public float attackMultiplier = 1f;
    public float knockbackPower = 500f;
    [Range(0f, 1f)]
    [LabelText("넉백 방향 가중치")]
    public float knockbackDirectionWeight = 0f;

    [HideLabel]
    public List<HitApplyBuffEntry> hitApplyBuffs = new();
    public float hitStop = 0.05f;
    public bool ignoreKnockbackDirection = false;

    public Vector2 GetKnockbackDirection(Vector3 attackerPos, Vector3 hitCenter, Vector3 targetPos)
    {
        Vector2 attackerDir = (Vector2)(targetPos - attackerPos);
        Vector2 hitCenterDir = (Vector2)(targetPos - hitCenter);

        if (attackerDir.sqrMagnitude <= 0.0001f)
            attackerDir = Vector2.right;
        else
            attackerDir.Normalize();

        if (hitCenterDir.sqrMagnitude <= 0.0001f)
            hitCenterDir = attackerDir;
        else
            hitCenterDir.Normalize();

        Vector2 blended = Vector2.Lerp(attackerDir, hitCenterDir, knockbackDirectionWeight);

        if (blended.sqrMagnitude <= 0.0001f)
            blended = attackerDir;

        return blended.normalized;
    }
}


[System.Flags]
public enum HitFilter
{
    None = 0,
    Bullet = 1 << 0,
    Direct = 1 << 1,
    Element = 1 << 2,
    Collision = 1 << 3,
    All = ~0
}

public enum HitFxType
{
    None = 0,
    Collision = 1,

    Brawl = 10,
    Slash = 20,
    Explosion = 30,
}

[System.Serializable]
public struct HitApplyBuffEntry
{
    [HorizontalGroup("Row", Width = 0.8f), HideLabel]
    public BuffData buff;

    [HorizontalGroup("Row", Width = 0.2f), HideLabel]
    public int count;
}
