using System.Drawing;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class SkillCaster : MonoBehaviour
{
    public static SkillCaster Instance;
    void Awake() => Instance = this;

    public void CastSkill(CharacterBehaviour owner, SkillData data, CharacterBehaviour target)
    {
        foreach (var e in data.effects)
        {
            switch (e.type)
            {
                case SkillEffectType.AddBuff:
                    for (int i = 0; i < e.buffCount; i++)
                        target.Buffs.AddBuff(e.buff);
                    break;

                case SkillEffectType.SpawnBullet:
                    {
                        Vector3 targetPoint = target != null ? target.transform.position : owner.transform.position;
                        Vector3 defaultSpawnPos = targetPoint;
                        Vector3 fallbackDir = target != null
                            ? (target.transform.position - owner.transform.position).normalized
                            : Vector3.right;

                        SpawnSkillBullet(owner, data, e, defaultSpawnPos, targetPoint, fallbackDir);
                        break;
                    }

                case SkillEffectType.RemoveBuffByTag:
                    if (target != null)
                        target.Buffs.RemoveByTagRandom(e.removeTag, e.buffCount);
                    break;

                case SkillEffectType.SpawnCharacter:
                    if (e.character == null || target == null) break;

                    var spawned1 = Instantiate(
                        CharacterSpawner.Instance.characterPrefab,
                        target.transform.position,
                        Quaternion.identity
                    );
                    spawned1.Init(e.character, owner.team);
                    break;

                case SkillEffectType.Heal:
                    {
                        float baseHeal = GetFinalEffectValue(e, owner, target);
                        float healerMul = owner != null ? owner.GetResilience() / 100f : 1f;
                        int heal = Mathf.RoundToInt(baseHeal * healerMul);
                        target.Heal(heal);
                    }
                    break;

                case SkillEffectType.RestoreMana:
                    {
                        int mana = Mathf.RoundToInt(GetFinalEffectValue(e, owner, target));
                        target.RestoreMana(mana);
                    }
                    break;

                case SkillEffectType.DoAction:
                    target.StartAction(e.action);
                    break;
            }
        }

        ShowSkillBubble(owner, data);
    }

    public void CastSkill(CharacterBehaviour owner, SkillData data, Vector3 point)
    {
        if (data.teamType == SkillTeamType.JustPoint)
        {
            foreach (var e in data.effects)
            {
                switch (e.type)
                {
                    case SkillEffectType.AddBuff:
                    case SkillEffectType.RemoveBuffByTag:
                    case SkillEffectType.Heal:
                    case SkillEffectType.DoAction:
                        break;

                    case SkillEffectType.SpawnBullet:
                        {
                            Vector3 targetPoint = point;
                            Vector3 defaultSpawnPos = point;
                            Vector3 fallbackDir = Vector3.right;

                            SpawnSkillBullet(owner, data, e, defaultSpawnPos, targetPoint, fallbackDir);
                            break;
                        }

                    case SkillEffectType.SpawnCharacter:
                        if (e.character == null) break;

                        var spawnedJP = Instantiate(
                            CharacterSpawner.Instance.characterPrefab,
                            point,
                            Quaternion.identity
                        );
                        spawnedJP.Init(e.character, owner.team);
                        break;
                }
            }

            ShowSkillBubble(owner, data);
            return;
        }

        foreach (var c in EntityContainer.Instance.Characters)
        {
            if (c.state == CharacterState.Die) continue;
            if (Vector2.Distance(c.transform.position, point) > data.radius) continue;
            if (!ValidateCharacter(data.teamType, owner, c)) continue;

            foreach (var e in data.effects)
            {
                switch (e.type)
                {
                    case SkillEffectType.AddBuff:
                        for (int i = 0; i < e.buffCount; i++)
                            c.Buffs.AddBuff(e.buff);
                        break;

                    case SkillEffectType.SpawnBullet:
                        var prefab = BulletSpawner.Instance.bulletPrefab;
                        var b = Instantiate(prefab, c.transform.position, Quaternion.identity);

                        Vector3 dir = c.transform.right;
                        b.Init(e.bullet, owner, dir, owner.team);

                        owner.Buffs.NotifyOwnerSpawnBullet(b);
                        break;

                    case SkillEffectType.RemoveBuffByTag:
                        c.Buffs.RemoveByTagRandom(e.removeTag, e.buffCount);
                        break;

                    case SkillEffectType.SpawnCharacter:
                        if (e.character == null) break;

                        var spawnedR = Instantiate(
                            CharacterSpawner.Instance.characterPrefab,
                            c.transform.position,
                            Quaternion.identity
                        );
                        spawnedR.Init(e.character, owner.team);
                        break;

                    case SkillEffectType.Heal:
                        {
                            float baseHeal = GetFinalEffectValue(e, owner, c);
                            float healerMul = owner != null ? owner.GetResilience() / 100f : 1f;
                            int heal = Mathf.RoundToInt(baseHeal * healerMul);
                            c.Heal(heal);
                        }
                        break;

                    case SkillEffectType.RestoreMana:
                        {
                            int mana = Mathf.RoundToInt(GetFinalEffectValue(e, owner, c));
                            c.RestoreMana(mana);
                        }
                        break;

                    case SkillEffectType.DoAction:
                        c.StartAction(e.action);
                        break;
                }
            }
        }

        ShowSkillBubble(owner, data);
    }

    void ShowSkillBubble(CharacterBehaviour owner, SkillData data)
    {
        if (owner == null || data == null)
            return;

        if (owner is PlayerCommander)
            return;

        if (string.IsNullOrWhiteSpace(data.skillScript))
            return;

        SkillBubbleSpawner.Instance.Show(owner, data.skillScript, 1f);
    }

    bool ValidateCharacter(SkillTeamType teamType, CharacterBehaviour owner, CharacterBehaviour target)
    {
        switch (teamType)
        {
            case SkillTeamType.Any:
                return true;

            case SkillTeamType.PlayerTeam:
                return target.team == TeamType.Player;

            case SkillTeamType.EnemyTeam:
                return target.team == TeamType.Enemy;

            case SkillTeamType.JustPoint:
                return false;
        }
        return false;
    }

    private float GetFinalEffectValue(SkillEffectEntry e, CharacterBehaviour owner, CharacterBehaviour target)
    {
        float finalValue = e.effectValue;

        if (e.formulas != null)
        {
            foreach (var f in e.formulas)
            {
                switch (f.type)
                {
                    case SkillFormulaType.TargetHPPercent:
                        if (target != null)
                            finalValue += target.GetMaxHealth() * (f.value / 100f);
                        break;

                    case SkillFormulaType.TargetAttackPercent:
                        if (target != null)
                            finalValue += target.GetAttackValue() * (f.value / 100f);
                        break;

                    case SkillFormulaType.OwnerMaxMPPercent:
                        if (owner != null)
                            finalValue += owner.GetMaxMana() * (f.value / 100f);
                        break;
                }
            }
        }

        return finalValue;
    }

    Vector3 ResolveSkillBulletStartPosition(CharacterBehaviour owner, SkillData skill, SkillEffectEntry effect, Vector3 defaultSpawnPos)
    {
        if (!effect.useUiStartPosition)
            return defaultSpawnPos;

        if (SkillInputController.Instance == null)
            return defaultSpawnPos;

        return SkillInputController.Instance.GetSkillUiWorldPosition(skill, defaultSpawnPos);
    }

    Vector3 ResolveBulletDirection(Vector3 spawnPos, Vector3 targetPoint, Vector3 fallbackDir)
    {
        Vector3 dir = targetPoint - spawnPos;
        dir.z = 0f;

        if (dir.sqrMagnitude <= 0.0001f)
        {
            fallbackDir.z = 0f;
            if (fallbackDir.sqrMagnitude <= 0.0001f)
                return Vector3.right;
            return fallbackDir.normalized;
        }

        return dir.normalized;
    }

    void SpawnSkillBullet(CharacterBehaviour owner, SkillData skill, SkillEffectEntry effect, Vector3 defaultSpawnPos, Vector3 targetPoint, Vector3 fallbackDir)
    {
        if (effect == null || effect.bullet == null)
            return;

        Vector3 spawnPos = ResolveSkillBulletStartPosition(owner, skill, effect, defaultSpawnPos);
        Vector3 dir = ResolveBulletDirection(spawnPos, targetPoint, fallbackDir);

        var prefab = BulletSpawner.Instance.bulletPrefab;
        var b = Instantiate(prefab, spawnPos, Quaternion.FromToRotation(Vector3.up, dir));

        if (effect.bullet.type == BulletType.ToTargetPoint)
            b.Init(effect.bullet, owner, dir, owner.team, targetPoint);
        else
            b.Init(effect.bullet, owner, dir, owner.team);

        owner.Buffs.NotifyOwnerSpawnBullet(b);
    }
}