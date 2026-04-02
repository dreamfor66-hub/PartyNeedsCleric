using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BulletHitBoxEntry
{
    public int startFrame;
    public int endFrame;
    public float radius = 0.5f;
    public Vector2 offset = Vector2.zero;
    public int hitBoxGroup = 0;
    public float hitInterval = 0f;
}

[Flags]
public enum BulletDespawnBy
{
    None = 0,
    LifeTime = 1 << 0,
    Hit = 1 << 1,
    CollideMap = 1 << 2,
    TargetPointReached = 1 << 3,
}

public enum BulletType
{
    Normal = 0,
    Orbit = 1,
    ToTargetPoint = 2,
}

public enum ReactionTriggerType
{
    Frame,
    Hit,
    Despawn,
    LifeTime,
    CollideMap,
}

public enum ReactionType
{
    SpawnBullet,
    SpawnVfx
}

[System.Serializable]
public class BulletReactionEntry
{
    public ReactionTriggerType triggerType;

    [ShowIf("triggerType", ReactionTriggerType.Frame)]
    public float value = 0f;

    public ReactionType reactionType;

    [ShowIf("reactionType", ReactionType.SpawnBullet)]
    public BulletData bulletData;

    [ShowIf("reactionType", ReactionType.SpawnBullet)]
    public Vector2 angleOffsetRange;


    [ShowIf("reactionType", ReactionType.SpawnVfx)]
    public VfxObject vfxPrefab;

    [ShowIf("reactionType", ReactionType.SpawnVfx)]
    public Vector2 offset;

    [ShowIf("reactionType", ReactionType.SpawnVfx)]
    public float offsetAngle;

}

[CreateAssetMenu(menuName = "Game/BulletData")]
public class BulletData : ScriptableObject
{
    [Header("General")]
    [PreviewField]
    public Sprite sprite;
    public BulletType type = BulletType.Normal;
    public float speed = 10f;
    public float accel = 0f;
    public float radius = 0.3f;

    [Header("Life")]
    public float lifeTime = 1f;
    public BulletDespawnBy despawnBy = BulletDespawnBy.LifeTime | BulletDespawnBy.Hit;

    [Header("ToTargetPoint Settings")]
    public bool arriveByDuration = false;

    [ShowIf(nameof(arriveByDuration))]
    [Min(0.01f)]
    public float arriveDuration = 1f;

    [Header("Orbit Settings")]
    public float orbitRange = 2f;
    public int orbitId = 0;
    public float followSpeed = 10f;

    [Header("Homing Settings")]
    public float homingDetectAngle = 90f;
    public AnimationCurve homingRotateCurve = AnimationCurve.Linear(0, 0, 1, 0);

    [Header("Pierce / Rehit")]
    public int maxPierceCount = 0;
    public float rehitInterval = 0.5f;


    [Header("Hit")]
    public HitData hitData;                    // *** ´ÜÀÏ HitData ***
    [TableList] public List<BulletHitBoxEntry> hitBoxes = new();

    [Header("Reactions")]
    public List<BulletReactionEntry> reactions = new();
}
