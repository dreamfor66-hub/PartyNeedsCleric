using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/CharacterData")]
public class CharacterData : ScriptableObject
{
    [Header("Base")]
    public string characterName;
    [TextArea]
    public string characterDesc;

    [Header("Sprites")]
    [PreviewField]
    public Sprite icon;
    [InlineProperty]
    public SpriteSet sprites;
    [ShowIf("@characterType == CharacterType.Commander")]
    [HorizontalGroup("A")]
    [PreviewField]
    public Sprite portrait;
    [ShowIf("@characterType == CharacterType.Commander")]
    [HorizontalGroup("A")]
    [PreviewField]
    public Sprite portrait2;

    [Header("Type")]
    // CharacterData.cs
    public bool isBoss;
    [ShowIf("@isBoss == false")]
    public CharacterType characterType;
    [ShowIf("@characterType == CharacterType.Ranged && isBoss == false")]
    public float rangeRadius = 150f;
    [ShowIf("@isBoss == true")]
    public FSMData fsmData;
    public bool destroyOnDie;
    public bool targettable = true;
    [EnumToggleButtons]
    public CharacterTagFlags characterTags;

    [Header("Physics")]
    public float radius = 30f;
    public float mass = 0.5f;

    [Header("Stat")]
    public int baseAtk = 10;
    public int baseHp = 100;
    public float moveSpeed = 10;

    [Header("Mana, 클레릭 전용. 나중에 일반 캐릭터도 스킬게이지 같은걸로 쓸지도?")]
    [ShowIf("@characterType == CharacterType.Commander")]
    public int baseMana = 100;
    [ShowIf("@characterType == CharacterType.Commander")]
    public int baseManaRegen = 0;
    [ShowIf("@characterType == CharacterType.Commander")]
    public int baseResilience = 100;
    [ShowIf("@characterType == CharacterType.Commander")]
    public int baseCooldownHaste = 100;
    [ShowIf("@characterType == CharacterType.Commander")]
    public int baseCastingHaste = 100;

    [Header("Hit")]
    public HitData baseHitData;
    [ShowIf("@characterType == CharacterType.Ranged && isBoss == false")]
    public BulletData baseBulletData;
    [ShowIf("@characterType == CharacterType.Ranged && isBoss == false")]
    public float bulletCooldown = 1f;

    [Header("테스트용")]
    public List<StartBuffEntry> StartBuffs = new List<StartBuffEntry>();
    public List<SkillData> BasicSkills = new List<SkillData>();
    public List<SkillData> StartSkills = new List<SkillData>();
}

[System.Serializable]
public class SpriteSet
{
    public Sprite anim_idleFront;
    public Sprite anim_idleBack;
    public Sprite anim_attackFront;
    public Sprite anim_attackBack;
    public Sprite anim_die;
}
[System.Serializable]
public class StartBuffEntry
{
    public BuffData buff;

    [Min(1)]
    public int count = 1;
}
public enum TeamType { Player, Enemy, Neutral }
public enum CharacterType
{
    Melee,
    Ranged,
    Totem,
    Commander = 1000
}

public enum CharacterState
{
    Search,
    Move,
    Action,
    Knockback,
    Die,
}

[Flags]
public enum CharacterTagFlags
{
    None = 0,

    Ghost = 1 << 0,
    Skeleton = 1 << 1,
    Inanimate = 1 << 2,
    Beast = 1 << 3,
    All = ~0
}