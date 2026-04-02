using Sirenix.OdinInspector;
using System;

public enum BuffTrigger
{
    //이 버프에서 뭔가 함
    OnBuffStart = 100,
    OnBuffEnd,
    OnEachStack,
    EverySecond,
    EveryMove,

    //Owner가 뭔가 함
    OwnerHit = 200,          // 오너가 타격 성공
    OwnerHurt,         // 오너가 피격됨
    OwnerSpawnBullet,  // 오너가 투사체 발사
    OwnerDie,
    OwnerUseSkill = 250,
}
public enum BuffCondition
{
    Always = 0,
    Chance = 100,

    //버프검사
    OwnerHasBuff = 200,
    TargetHasBuff,

    //스킬이라면, 상태 검사
    SkillUseCasting = 300,
}
public enum BuffTarget
{
    ThisBuff = 0,
    Owner = 10,
    NearestEnemy,
    NearestAlly,
    AllAlly,
    AllEnemy,

    //전투 관련
    Attacker = 20,       // 타격한 사람
    HitTarget,       // 피격한 사람
    
    //트리거 된 객체 관련
    TriggeredHit = 30,
}

public enum BuffEffect
{

    //단순 스탯 변화, 단순 이벤트
    GainAtk = 100,   // 공격력 버프 그룹 (곱연산)
    GainResilience,
    GainSpeedUp,     // 추가
    GainSlow,        // 추가
    GainSkillCastingHaste,
    GainSkillCooldownHaste,

    //전투 이벤트
    InstantDamage = 200,      // 즉시 대미지
    Heal,               // 체력 회복
    InstantKill,
    
    //Spawn
    SpawnBullet = 300,        // 타겟의 명의로
    SpawnCharacter,     // 타겟 팀으로

    //Buff
    AddBuff = 400,          // 버프 부여
    RemoveBuffById,     // 해당 id 전부 제거
    RemoveBuff,
    RemoveBuffByTag,

    //Bullet
    //BulletSpeedUp = 500,      // 최근 스폰 불릿에 가속도 부여 (2차 확장에서 실구현)
    //BulletPierceUp,      // 최근 스폰 불릿 관통 수 증가 (2차 확장에서 실구현)

    //Hit
    HitModifyDamage = 600, // 신규
    IgnoreHit,               // 신규

    TriggerVfx1 = 9000,
    TriggerVfx2,
    TriggerVfx3
}

public enum BuffRemoveCondition
{
    Permanent,
    Duration,
    Instant,
    TriggerCount,
}

public enum BuffDuplicatePolicy
{
    Separate, // 별개 버프로 획득
    Stack,    // 스택
    Refresh,   // 갱신 (지속시간 리셋)
    IgnoreNew,
}
public enum BuffTeamSelector
{
    Target, // 타겟의 teamType을 따른다
    Player,
    Enemy
}
public enum BuffFormulaType
{
    StackPer,         // 스택 당
    OwnerAttackPercent, // owner 공격력 비례
    OwnerHPPercent, // owner 체력 비례
    StackPerOwnerAttackPercent = 20, // owner 공격력 비례한 숫자 x 스택을 반환
    StackPerOwnerHPPercent, // owner 체력에 비례한 숫자 x 스택을 반환
    EntityCount, //EntityContainer에서 string 이름을 포함한 캐릭터의 수 x 스택을 반환
    Min = 100,              // 최소치 제한
    Max               // 최대치 제한
}
public enum SpawnDirection
{
    MoveDirection,
    ToTarget,
    ToNearestEnemy,
    ToNearestAlly,
    Up
}