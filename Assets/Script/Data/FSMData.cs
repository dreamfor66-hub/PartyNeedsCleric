using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public enum FSMActionType
{
    Casting,
    Melee,
    Range,
    Rush
}

public enum ExtraActionType
{
    Search = 0,
    AddBuff = 10,
    SpawnBullet = 20,
    SpawnCharacter = 30,
}

[Serializable]
public class ExtraAction
{
    public int id;

    [LabelText("Action")]
    public ExtraActionType type;


    // =============== 공용 range ===============
    [ShowIf(nameof(NeedsRange))]
    public float range = 150f;        // AddBuff + SpawnCharacter 공유


    // =============== 공용 offset ===============
    [ShowIf(nameof(NeedsOffset))]
    public Vector2 offset;            // SpawnBullet + SpawnCharacter 공유


    // =============== AddBuff ===============
    [ShowIf(nameof(IsAddBuff))]
    public BuffData buff;

    [ShowIf(nameof(IsAddBuff))]
    public int buffStacks = 1;

    public enum TargetSelector { Self, CurrentTarget, NearestEnemy, EnemyInRange, AllEnemy, AllAlly }

    [ShowIf(nameof(IsAddBuff))]
    public TargetSelector effTarget;


    // =============== SpawnBullet ===============
    [ShowIf(nameof(IsSpawnBullet))]
    public BulletData bullet;

    public enum BulletDir { ToNearestEnemy, ToCurrentTarget, Up }

    [ShowIf(nameof(IsSpawnBullet))]
    public BulletDir bulletDir;

    [ShowIf(nameof(IsSpawnBullet))]
    public Vector2 angleOffsetRange;


    // =============== SpawnCharacter ===============
    [ShowIf(nameof(IsSpawnCharacter))]
    public CharacterData characterData;

    [ShowIf(nameof(IsSpawnCharacter))]
    public int spawnCount = 1;


    // =============== Visual ==================

    public bool useAnim;

    [ShowIf("useAnim")]
    [PreviewField]
    [HorizontalGroup("Row", marginLeft: 75), HideLabel, SuffixLabel("Front")]
    public Sprite animSpriteFront;
    [ShowIf("useAnim")]
    [PreviewField]
    [HorizontalGroup("Row"), HideLabel, SuffixLabel("Back")]
    public Sprite animSpriteBack;

    [ShowIf("useAnim")]
    public float animDuration = 0.2f;


    // ============================================================
    //                --------- 패턴 핵심 ---------
    // ============================================================

    bool IsAddBuff() => type == ExtraActionType.AddBuff;
    bool IsSpawnBullet() => type == ExtraActionType.SpawnBullet;
    bool IsSpawnCharacter() => type == ExtraActionType.SpawnCharacter;


    bool NeedsRange()
    {
        // AddBuff: EnemyInRange일 때만 사용
        if (type == ExtraActionType.AddBuff && effTarget == TargetSelector.EnemyInRange)
            return true;

        // SpawnCharacter: 항상 range 사용
        if (type == ExtraActionType.SpawnCharacter)
            return true;

        return false;
    }

    bool NeedsOffset()
    {
        // SpawnBullet / SpawnCharacter 공용 offset
        return type == ExtraActionType.SpawnBullet
            || type == ExtraActionType.SpawnCharacter;
    }
}

[Serializable]
public class FSMStateEntry
{
    public int index;
    public float duration = 3f;
    public FSMActionType type;
    public int onStartExtra = -1;
    public int onEndExtra = -1;
    public int next = -1;
}

[CreateAssetMenu(menuName = "Game/FSMData")]
public class FSMData : ScriptableObject
{
    [Title("State List")]
    [TableList]
    public List<FSMStateEntry> fsmList = new();

    [Title("Extra Action List")]
    public List<ExtraAction> extraActions = new();

    public ExtraAction GetExtra(int id)
    {
        foreach (var ex in extraActions)
            if (ex.id == id)
                return ex;
        return null;
    }

    public FSMStateEntry GetState(int idx)
    {
        foreach (var s in fsmList)
            if (s.index == idx)
                return s;
        return null;
    }
}
