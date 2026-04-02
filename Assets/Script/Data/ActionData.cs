// ActionData.cs (전체 교체)

using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum ActionDirectionType { Forward, ToTarget, ToNearestEnemy, ToNearestAlly }
public enum ActionSpawnAnchorType { Owner, Target, NearestEnemy, NearestAlly }

public enum AutoCorrectionType
{
    Forward,
    ToTarget,
    ToNearestEnemy,
    ToNearestAlly,
}
public enum ActionRotationType
{
    ToTarget,
    ToNearestEnemy,
    ToNearestAlly,
}
[Serializable]
public class ActionMoveEntry
{
    public int startFrame;
    public int endFrame;
    public float startValue = 0f;
    public float endValue = 0f;

    public bool applyHaste = false;
}
[System.Serializable]
public class ActionRotationEntry
{
    public int startFrame;
    public int endFrame;
    public ActionRotationType rotationType;

    // degrees per second
    public float value;
}
[Serializable]
public class ActionSpawnBulletEntry
{
    public int frame;

    public BulletData bullet;

    public ActionSpawnAnchorType anchor = ActionSpawnAnchorType.Owner;
    public ActionDirectionType directionType = ActionDirectionType.Forward;

    public Vector2 spawnOffset = Vector2.zero; // (right, forward)
    public float angleOffset = 0f;             // Direction 결정 후 회전(도)
}

public enum ActionBuffEventType
{
    Add,
    RemoveByTag,
}

[System.Serializable]
public class ActionHitBoxEntry
{
    public int startFrame;
    public int endFrame;
    public float radius = 100f;
    public Vector2 offset = Vector2.zero;
    public int hitBoxGroup = 0;

    public string hitId = "1"; // hits 리스트에서 HitData.hitId 로 매칭

    public float hitInterval = 0f;
}

[Serializable]
public class ActionBuffEventEntry
{
    public int frame = 1;
    public ActionBuffEventType type = ActionBuffEventType.Add;

    [ShowIf("@type == ActionBuffEventType.Add")]
    public BuffData buff;

    //[Min(1)]
    public int count = 1;

    [ShowIf("@type == ActionBuffEventType.RemoveByTag")]
    public BuffTag removeTag;
}

[Serializable]
public class ActionSpawnCharacterEntry
{
    public int frame;

    public CharacterData character;

    public ActionSpawnAnchorType anchor = ActionSpawnAnchorType.Owner;
    public ActionDirectionType directionType = ActionDirectionType.Forward;

    public Vector2 spawnOffset = Vector2.zero; // (right, forward)
}
public enum ActionRestoreType
{
    Heal,
    Mana,
}

[Serializable]
public class ActionRestoreEntry
{
    public int frame = 1;
    public ActionRestoreType type = ActionRestoreType.Heal;

    public float value = 0f;
}

[Serializable]
public class ActionVfxEntry
{
    public int frame;
    public VfxObject vfxPrefab;
    public Vector2 offset = Vector2.zero; // (right, forward)
    public float offsetAngle = 0f;
}

[CreateAssetMenu(menuName = "Game/ActionData")]
public class ActionData : ScriptableObject
{
    public int totalFrame = 60;

    [Header("Movement")]
    [TableList] public List<ActionMoveEntry> movementList = new();
    [TableList] public List<ActionRotationEntry> rotationList = new();

    [Header("Hit")]
    [TableList] public List<ActionHitBoxEntry> hitBoxes = new();
    public List<HitData> hits = new();

    [Header("Events - SpawnBullet")]
    [TableList] public List<ActionSpawnBulletEntry> spawnBulletList = new();

    [Header("Events - Buff")]
    [TableList] public List<ActionBuffEventEntry> buffList = new();

    [Header("Events - SpawnCharacter")]
    [TableList] public List<ActionSpawnCharacterEntry> spawnCharacterList = new();

    [Header("Events - Restore")]
    [TableList] public List<ActionRestoreEntry> restoreList = new();

    [Header("Events - VFX")]
    [TableList] public List<ActionVfxEntry> vfxList = new();

    [Header("AutoCorrection")]
    public AutoCorrectionType autoCorrectionType = AutoCorrectionType.Forward;
}
