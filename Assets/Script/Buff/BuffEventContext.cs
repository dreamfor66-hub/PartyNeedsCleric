using UnityEngine;

public class BuffEventContext
{
    public CharacterBehaviour Owner;
    public CharacterBehaviour Source;
    public CharacterBehaviour Root;
    public CharacterBehaviour HitSource;
    public CharacterBehaviour HitTarget;
    public BulletBehaviour Bullet;
    public int Damage;
    public BuffInstance ThisBuff;
    public HitFilter HitFlags;
    public HitContext Hit;
    public SkillData UsedSkill;
}
public class HitContext
{
    public CharacterBehaviour Attacker;
    public CharacterBehaviour Target;
    public int Damage;
    public bool Ignore;
}