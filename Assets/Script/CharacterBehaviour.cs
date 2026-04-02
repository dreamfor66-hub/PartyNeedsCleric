using NUnit.Framework.Constraints;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class CharacterBehaviour : MonoBehaviour
{
    public CharacterData data;

    public Rigidbody2D rb { get; private set; }
    public CircleCollider2D col { get; private set; }
    public SpriteRenderer sr { get; private set; }
    public Vector2 lastVelocity;
    public float lastSpeed;
    [HideInInspector] public HitData lastHitData;

    public CharacterBehaviour target;
    [HideInInspector] public Vector2 MoveDirection;
    BotAI ai;
    FSMAI fsm;
    CharacterMove mover;
    CharacterAnimator anim;
    CharacterAction action;
    CharacterSkillObserver skillObserver;
    HPBar hpBarWorld;  // 머리 위 체력바
    CanvasHPBar hpBarUI;     // UI 고정 체력바

    public CharacterSkillObserver GetSkillObserver() => skillObserver;

    public List<Equipment> EquipmentList = new List<Equipment>();

    private int equipmentAddAtk = 0;
    private int equipmentAddHp = 0;
    private int equipmentAddMp = 0;
    private float equipmentAddResilience = 0f;
    private float equipmentAddManaRegen = 0f;
    private float equipmentCastingHaste = 0f;
    private float equipmentCooldownHaste = 0f;
    private float equipmentAddMoveSpeed = 0f;

    // 다른 시스템에서 쓸 수 있게 노출 (현재 프로젝트에 해당 스탯 적용 지점이 없어서 getter만 제공)
    public float EquipmentAddResilience => equipmentAddResilience;
    public float EquipmentAddManaRegen => equipmentAddManaRegen;
    public float EquipmentCastingHaste => equipmentCastingHaste;
    public float EquipmentCooldownHaste => equipmentCooldownHaste;
    public float EquipmentAddMoveSpeed => equipmentAddMoveSpeed;

    public CharacterState state = CharacterState.Search;
    public TeamType team = TeamType.Player;
    public SpriteRenderer shadow;

    float searchTimer = 0f;
    public float knockbackTime = 0f;
    float hitStopTimer = 0f;
    Vector2 pendingKnockbackDir;
    float pendingKnockbackPower;
    bool hasPendingKnockback = false;
    public bool isCastingFSM = false;
    // Mana Regen
    private float manaRegenCarry = 0f;


    public BotAI AI => ai;
    public FSMAI FSM => fsm;
    public BuffManager Buffs { get; private set; }

    // ===== Stat =====
    [SerializeField] private int maxHP;
    [SerializeField] private int currentHP;
    [SerializeField] private int currentAtk;
    [SerializeField] private float currentResilience = 100f; // 기본값 100%
    [SerializeField] private float currentSpeed = 100f; // 기본값 100%
    [SerializeField] private float currentSkillCastHaste = 100f; // 기본값 100%
    [SerializeField] private float currentSkillCooldownHaste = 100f; // 기본값 100%
    [SerializeField] int maxMana;
    [SerializeField] int currentMana;
    [SerializeField] private float currentManaRegen = 0; // 기본값 0
    public int GetCurrentHealth() => currentHP;  // read-only 접근자
    public int GetCurrentMana() => currentMana;
    public float GetResilience() => currentResilience;
    public int GetAttackValue() => currentAtk;
    public float GetSpeed() => currentSpeed;
    public int GetMaxHealth() => maxHP;
    public int GetMaxMana() => maxMana;
    public float GetManaRegen() => currentManaRegen;
    public float GetSkillCastHaste() => currentSkillCastHaste;
    public float GetSkillCooldownHaste() => currentSkillCooldownHaste;

    public bool HasMana(int cost) => currentMana >= cost;


    public void SetResilience(float value) => currentResilience = Mathf.Max(0, value);
    public void SetAttackValue(int newAtk) => currentAtk = Mathf.Max(1, newAtk);
    public void SetSpeed(float value) => currentSpeed = Mathf.Max(0.1f, value);
    public void SetMaxHealth(int value) => maxHP = Mathf.Max(1, value);
    public void SetMaxMana(int value) => maxMana = Mathf.Max(0, value);
    public void SetManaRegen(float value) => currentManaRegen = Mathf.Max(0, value);
    public  void SetSkillCastHaste(float value) => currentSkillCastHaste = Mathf.Max(0,value);
    public  void SetSkillCooldownHaste(float value) => currentSkillCooldownHaste = Mathf.Max(0,value);

    public void RecalculateFinalStats()
    {
        var buffs = Buffs;

        // --- ATK ---
        int newAtk = Mathf.RoundToInt((data.baseAtk + equipmentAddAtk) * (buffs.BuffAtkMultiplier));
        SetAttackValue(newAtk);

        // --- MAX HP ---
        int oldMaxHp = maxHP;
        int newMaxHp =
        Mathf.RoundToInt((data.baseHp + equipmentAddHp) * buffs.BuffHpMultiplier);
        SetMaxHealth(newMaxHp);
        float ratio = (float)currentHP / Mathf.Max(1, oldMaxHp);
        currentHP = Mathf.RoundToInt(newMaxHp * ratio);
        currentHP = Mathf.Clamp(currentHP, 1, newMaxHp); // 오버플로우 조정
        hpBarWorld?.OnHPChanged(currentHP, newMaxHp); // UI갱신
        hpBarUI?.OnHPChanged(currentHP, newMaxHp);

        // --- SPEED ---
        float baseSpeed = (data.moveSpeed + equipmentAddMoveSpeed);
        float speedUpMul = 1f + buffs.BuffSpeedUpMultiplier - 1f; // (= BuffSpeedUpMultiplier)
        float slowMul = buffs.BuffSlowMultiplier;               // 이미 0~1 보정
        float finalSpeed = baseSpeed * speedUpMul * slowMul;
        SetSpeed(finalSpeed);

        // --- MAX Mana ---
        // --- MAX Mana ---
        int oldMaxMp = maxMana;
        int newMaxMp = Mathf.RoundToInt((data.baseMana + equipmentAddMp));
        SetMaxMana(newMaxMp);

        // HP랑 동일하게 비율 유지 (초기엔 currentMana==oldMaxMp라서 장비 적용 후 풀마나가 됨)
        float mpRatio = (float)currentMana / Mathf.Max(1, oldMaxMp);
        currentMana = Mathf.RoundToInt(newMaxMp * mpRatio);
        currentMana = Mathf.Clamp(currentMana, 0, newMaxMp);

        // --- ManaRegen ---
        int newManaRegen = Mathf.RoundToInt((data.baseManaRegen + equipmentAddManaRegen));
        SetManaRegen(newManaRegen);

        // --- RESILIENCE ---
        float newResilience = (data.baseResilience+equipmentAddResilience) * buffs.BuffResilienceMultiplier;
        SetResilience(newResilience);

        // --- SkillCastingHaste. 일반적으로 사용 안하는 커맨더 전용 ---
        float newCastHaste = (data.baseCastingHaste+equipmentCastingHaste) * buffs.BuffSkillCastMultiplier;
        SetSkillCastHaste(newCastHaste);

        // --- CooldownHaste 일반적으로 사용 안하는 커맨더 전용 ---
        float newCooldownHaste = (data.baseCooldownHaste + equipmentCooldownHaste) * buffs.BuffSkillCooldownMultiplier;
        SetSkillCooldownHaste(newCooldownHaste);
    }

    public virtual void UseMana(int cost)
    {
        currentMana = Mathf.Max(0, currentMana - cost);
    }

    public virtual void RestoreMana(int amount)
    {
        currentMana = Mathf.Min(maxMana, currentMana + amount);
    }

    public virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        col = GetComponent<CircleCollider2D>();
        rb.gravityScale = 0;
    }
    public virtual void Init(CharacterData newData, TeamType team)
    {
        data = newData;
        this.team = team;

        InitSystems();
        InitBaseStats();
        InitPhysical();
        InitUI();
        InitSearchState();

        // EntityContainer & SpriteSorter 등록
        EntityContainer.Instance.Register(this);
        GlobalSpriteSorter.Instance.RegisterRenderer(sr);

        //InitVisual
        sr.sprite = data.sprites.anim_idleFront;
        var outline = GetComponentInChildren<SpriteOutline>();
        if (outline != null) outline.OutlineColor = Color.clear;
        var size = data.sprites.anim_idleFront.texture.Size();
        var shadowY = 13.5f / 32 * size.y;
        shadow.transform.localPosition = new Vector3(0, -shadowY, 1);
        shadow.transform.localScale = new Vector3(size.x / 32, size.y / 32, 1);

        //개발용
        foreach (var entry in data.StartBuffs)
        {
            for (int i = 0; i < entry.count; i++)
                Buffs.AddBuff(entry.buff);
        }

        if (data != null && data.StartSkills != null)
        {
            var skillsArr = new SkillData[CharacterSkillObserver.MaxSkills];
            for (int i = 0; i < data.StartSkills.Count && i < skillsArr.Length; i++)
                skillsArr[i] = data.StartSkills[i];

            skillObserver.SetSkills(skillsArr);
        }

        skillObserver.ApplyPassiveOnce();
    }
    protected void InitBaseStats()
    {
        gameObject.name = data.name.Split('_').Last();

        maxHP = data.baseHp + equipmentAddHp;
        currentHP = maxHP;
        currentAtk = data.baseAtk + equipmentAddAtk;
        currentSpeed = data.moveSpeed + equipmentAddMoveSpeed;
        currentResilience = data.baseResilience + EquipmentAddResilience;
        maxMana = data.baseMana + equipmentAddMp;
        currentMana = maxMana;
        currentManaRegen = data.baseManaRegen + equipmentAddManaRegen;
        manaRegenCarry = 0f;

        // 팀 레이어 설정
        switch (team)
        {
            case TeamType.Player: gameObject.layer = LayerMask.NameToLayer("Player"); break;
            case TeamType.Enemy: gameObject.layer = LayerMask.NameToLayer("Enemy"); break;
            case TeamType.Neutral: gameObject.layer = LayerMask.NameToLayer("Neutral"); break;
        }
    }
    protected virtual void InitPhysical()
    {
        rb.mass = data.mass;
        col.radius = data.radius;
        if (data.radius == 0)
            col.enabled = false;

        // AI, Move, Animator
        mover = new CharacterMove(this);
        if (data.isBoss && data.fsmData != null)
        {
            // 기존 BotAI 대신 FSMAI 사용
            ai = null;   // BotAI 제거
            fsm = new FSMAI(this, data.fsmData);   // 새 FSM AI
        }
        else
        {
            // 기존 BotAI
            ai = new BotAI(this);
            fsm = null;
        }
        anim = new CharacterAnimator(this, sr);

        if (fsm != null)
            fsm.StartFSM();
    }
    protected virtual void InitSystems()
    {
        Buffs = new BuffManager(this);
        ApplyEquipmentBuffs();
        action = new CharacterAction(this);
        skillObserver = new CharacterSkillObserver(this);
    }

    protected virtual void InitUI()
    {
        hpBarWorld = HPBarSpawner.Instance.SpawnWorldBar(this);
        hpBarUI = team == TeamType.Player ? HPBarSpawner.Instance.SpawnUIBar(this) : null;
    }
    protected void InitSearchState()
    {
        searchTimer = GameVariables.Instance.searchTime;
        ChangeState(CharacterState.Search);
    }

    public virtual void FixedUpdate()
    {
        if (hitStopTimer > 0f)
        {
            UpdateHitStop();
            return;
        }

        Buffs?.Tick(Time.deltaTime);
        TickManaRegen(Time.deltaTime);
        skillObserver.Tick(Time.deltaTime);

        lastVelocity = rb.linearVelocity;
        lastSpeed = lastVelocity.magnitude;
        switch (state)
        {
            case CharacterState.Search:
                UpdateSearch();
                break;
            case CharacterState.Move:
                UpdateMove();
                break;
            case CharacterState.Action:         // [추가]
                UpdateAction();                 // [추가 메서드]
                break;
            case CharacterState.Knockback:
                UpdateKnockback();
                break;
            case CharacterState.Die:
                break;
        }
        anim.Update();
    }
    void UpdateHitStop()
    {
        if (state == CharacterState.Die)
        {
            hitStopTimer = 0f;
            hasPendingKnockback = false;
            return;
        }
        hitStopTimer -= Time.deltaTime;
        rb.linearVelocity = Vector2.zero;

        if (hitStopTimer <= 0f && hasPendingKnockback)
        {
            rb.AddForce(pendingKnockbackDir * pendingKnockbackPower, ForceMode2D.Impulse);
            ChangeState(CharacterState.Knockback);
            hasPendingKnockback = false;
        }
    }
    void UpdateSearch()
    {
        if (fsm != null)
        {
            ChangeState(CharacterState.Move);
            return;
        }
        searchTimer -= Time.deltaTime;

        if (searchTimer <= 0f)
        {
            var enemy = EntityContainer.Instance.GetNearestEnemy(this);
            target = enemy;
            if (enemy != null && lastHitData != null && lastHitData.ignoreKnockbackDirection)
            {
                Vector2 dir = (enemy.transform.position - transform.position).normalized;
                MoveDirection = dir;
            }
            ChangeState(CharacterState.Move);
        }
    }

    void UpdateMove()
    {
        if (fsm == null)
            ai.Update();
        mover.Update();
    }
    void UpdateKnockback()
    {
        mover.Update();
    }
    public void StartAction(ActionData data)
    {
        ChangeState(CharacterState.Action);
        action.Start(data);
    }
    public void ChangeState(CharacterState newState)
    {
        if (state == newState) return;
        if (state == CharacterState.Die && newState != CharacterState.Die)
        {
            hpBarWorld?.SetActive(true);
            col.isTrigger = false;
        }
        if (state == CharacterState.Action && newState != CharacterState.Action)
            action.Cancel();

        state = newState;

        switch (newState)
        {
            case CharacterState.Search:
                searchTimer = GameVariables.Instance.searchTime;
                if (lastHitData == null || !lastHitData.ignoreKnockbackDirection)
                {
                    Vector2 v = rb.linearVelocity;
                    if (v.sqrMagnitude > 0.01f)
                        MoveDirection = v.normalized;
                }
                break;

            case CharacterState.Move:
                // Move 시작 시 별도 초기화 없음
                break;

            case CharacterState.Knockback:
                // Knockback 시작 시 별도 처리 없음 (ApplyKnockback에서 Force 처리)
                break;

            case CharacterState.Die:
                Buffs?.NotifyOwnerDie();
                Buffs?.ClearAll();
                currentHP = 0;
                rb.linearVelocity = Vector2.zero;
                hpBarWorld?.SetActive(false);
                col.isTrigger = true;
                if (data.isBoss)
                {
                    foreach(var p in EntityContainer.Instance.GetAllAllies(this))
                    {
                        p.ChangeState(CharacterState.Die);
                    }
                }
                if (data.destroyOnDie)
                {
                    EntityContainer.Instance.Unregister(this);
                    Destroy(gameObject);
                }  
                break;
        }
    }
    private void UpdateAction()
    {
        bool finished = action.Tick(Time.deltaTime);

        if (finished)
            ChangeState(CharacterState.Search);
    }

    public void ApplyHitStop(float duration) 
    {
        hitStopTimer = duration;
        rb.linearVelocity = Vector2.zero;
    }

    public virtual void ApplyDamage(CharacterBehaviour attacker, int finalDamage)
    {
        if (state == CharacterState.Die) return;

        currentHP -= finalDamage;

        float scale = Mathf.Clamp((float)finalDamage / attacker.GetAttackValue(), 0.8f, 2f);
        DamageUIManager.Instance.Show(finalDamage, transform.position + new Vector3(Random.Range(-10, 10), Random.Range(-10, 10)), Color.white, scale);

        hpBarWorld?.OnHPChanged(currentHP, maxHP);
        hpBarUI?.OnHPChanged(currentHP, maxHP);

        if (currentHP <= 0)
        {
            ChangeState(CharacterState.Die);
            return;
        }
    }


    public void ApplyKnockback(Vector2 dir, float power)
    {
        if (state == CharacterState.Die) return;

        // hitStop 중이라면, 바로 힘을 주지 않고 종료 후 적용
        if (hitStopTimer > 0f)
        {
            hasPendingKnockback = true;
            pendingKnockbackDir = dir;
            pendingKnockbackPower = power;
            return;
        }

        // 평상시 즉시 적용
        rb.AddForce(dir * power, ForceMode2D.Impulse);
        ChangeState(CharacterState.Knockback);
    }
    public virtual void Heal(int amount)
    {
        if (state == CharacterState.Die) return;

        int finalHeal = Mathf.Max(0, amount);
        currentHP = Mathf.Min(maxHP, currentHP + finalHeal);
        if (currentHP > maxHP)
            currentHP = maxHP;

        float scale = Mathf.Clamp((float)amount / GetAttackValue(), 0.8f, 2f);
        DamageUIManager.Instance.Show(amount, transform.position + new Vector3(Random.Range(-10, 10), Random.Range(-10, 10)), Color.green, scale);

        hpBarWorld?.OnHPChanged(currentHP, maxHP);   // [추가]
        hpBarUI?.OnHPChanged(currentHP, maxHP);      // [추가]
    }

    public void PlayAttackAnim()
    {
        anim.PlayAttack();
    }
    public void PlayExtraAnim(Sprite sprFront, Sprite sprBack, float duration)
    {
        anim.PlayExtra(sprFront, sprBack, duration);
    }
    void OnCollisionEnter2D(Collision2D col)
    {
        if (state == CharacterState.Die) return;

        var other = col.collider.GetComponent<CharacterBehaviour>();
        if (other != null)
        {
            // 캐릭터-캐릭터 충돌 (기존 로직 그대로)
            if (other.state == CharacterState.Die) return;

            if (GetInstanceID() < other.GetInstanceID())
            {
                CollisionManager.Resolve(this, other);
            }
        }
        else
        {
            // 캐릭터-맵/Collision 충돌 → 벽 튕김 처리
            CollisionManager.ResolveWall(this, col);
        }
    }
    public void SpawnBullet(CharacterBehaviour target, BulletData data)
    {
        var prefab = BulletSpawner.Instance.bulletPrefab;
        var b = Instantiate(prefab, transform.position, Quaternion.identity);

        Vector3 dir = (target.transform.position - transform.position).normalized;

        // === 수정된 Init ===
        b.Init(data, this, dir, team);

        PlayAttackAnim();
        Buffs?.NotifyOwnerSpawnBullet(b);
    }

    public void TickManaRegen(float dt)
    {
        if (state == CharacterState.Die) return;
        if (maxMana <= 0) return;
        if (currentMana >= maxMana) return;

        float regenPerSec = GetManaRegen(); // "초당 회복량"으로 사용
        if (regenPerSec <= 0f) return;

        manaRegenCarry += regenPerSec * dt;

        int add = Mathf.FloorToInt(manaRegenCarry);
        if (add <= 0) return;

        manaRegenCarry -= add;
        RestoreMana(add);
    }


    public void Despawn()
    {
        // SpriteRenderer 해제
        var srs = GetComponentsInChildren<SpriteRenderer>();
        foreach (var s in srs)
            GlobalSpriteSorter.Instance.UnregisterRenderer(s);

        // EntityContainer 해제
        EntityContainer.Instance.Unregister(this);
    }

    // ==================================================
    // Equipment
    // ==================================================

    public void SetEquipmentLoadout(List<Equipment> list)
    {
        EquipmentList.Clear();
        if (list != null) EquipmentList.AddRange(list);

        RebuildEquipmentStatAdds();     // base + flatStat 옵션까지 여기서 합산
        RebuildEquipmentOptionRuntime(); // damageEnhance / skillTag cd 등 런타임 테이블 생성

        if (Buffs != null)
            ApplyEquipmentBuffs();

        RecalculateFinalStats();
    }


    private void RebuildEquipmentStatAdds()
    {
        equipmentAddAtk = 0;
        equipmentAddHp = 0;
        equipmentAddMp = 0;

        equipmentAddResilience = 0f;
        equipmentAddManaRegen = 0f;
        equipmentCastingHaste = 0f;
        equipmentCooldownHaste = 0f;
        equipmentAddMoveSpeed = 0f;

        equipmentOptionsRuntime.Clear(); // 옵션 런타임은 따로 rebuild에서 채움

        for (int i = 0; i < EquipmentList.Count; i++)
        {
            var e = EquipmentList[i];
            if (e == null) continue;

            if (e.Data == null)
                e.BindData();

            if (e.Data == null) continue;

            // Base 2 stats
            AddStat(e.Data.statType1, e.rolledStat1);
            AddStat(e.Data.statType2, e.rolledStat2);

            // FlatStat 옵션은 “스탯 합산”에서 바로 누적 (OptionPool 없이 Equipment.options만 사용)
            if (e.options != null)
            {
                foreach (var op in e.options)
                {
                    if (op == null) continue;
                    if (op.optionType != EquipmentOptionType.FlatStat) continue;

                    AddStat(op.statType, op.value);
                }
            }
        }
    }

    private void RebuildEquipmentOptionRuntime()
    {
        equipmentOptionsRuntime.Clear();

        for (int i = 0; i < EquipmentList.Count; i++)
        {
            var e = EquipmentList[i];
            if (e == null) continue;
            if (e.options == null) continue;

            foreach (var op in e.options)
            {
                if (op == null) continue;

                // FlatStat은 이미 합산 완료
                if (op.optionType == EquipmentOptionType.FlatStat) continue;

                equipmentOptionsRuntime.Add(new EquipmentOptionRuntime
                {
                    type = op.optionType,

                    statType = op.statType,
                    skillTags = op.skillTags,

                    hitFlags = op.hitFlags,
                    targetTags = op.targetTags,

                    value = op.value,
                });
            }
        }
    }



    private void AddStat(EquipmentStatType type, float value)
    {
        switch (type)
        {
            case EquipmentStatType.Attack: equipmentAddAtk += (int)value; break;
            case EquipmentStatType.Health: equipmentAddHp += (int)value; break;
            case EquipmentStatType.Mp: equipmentAddMp += (int)value; break;

            case EquipmentStatType.Resilience: equipmentAddResilience += value; break;
            case EquipmentStatType.BaseManaRegen: equipmentAddManaRegen += value; break;
            case EquipmentStatType.BaseMoveSpeed: equipmentAddMoveSpeed += value; break;

            // "가속 %" 누적 (30이면 30% 더 빠름)
            case EquipmentStatType.CastHaste: equipmentCastingHaste += value; break;
            case EquipmentStatType.CooldownHaste: equipmentCooldownHaste += value; break;
        }
    }

    private void ApplyEquipmentBuffs()
    {
        // 장비 패시브 버프는 "AddBuff"로 넣는다 (중복 정책은 BuffData 설정에 따름)
        for (int i = 0; i < EquipmentList.Count; i++)
        {
            var e = EquipmentList[i];
            if (e == null) continue;

            var d = e.Data;
            if (d == null || d.buffs == null) continue;

            for (int b = 0; b < d.buffs.Count; b++)
            {
                var bd = d.buffs[b];
                if (bd == null) continue;
                Buffs.AddBuff(bd, 1);
            }
        }
    }

    // ===== Equipment Option Runtime =====
    private List<EquipmentOptionRuntime> equipmentOptionsRuntime = new List<EquipmentOptionRuntime>();

    private class EquipmentOptionRuntime
    {
        public EquipmentOptionType type;

        public EquipmentStatType statType;
        public SkillTagFlags skillTags;

        public HitFilter hitFlags;
        public CharacterTagFlags targetTags;

        public float value;
    }
    public float DamageEnhanceSum(HitFilter hitFlags, CharacterBehaviour target)
    {
        float sum = 0f;

        var tTags = (target != null && target.data != null) ? target.data.characterTags : CharacterTagFlags.None;

        for (int i = 0; i < equipmentOptionsRuntime.Count; i++)
        {
            var o = equipmentOptionsRuntime[i];
            if (o.type != EquipmentOptionType.DamageEnhance) continue;

            bool okHit = (o.hitFlags == HitFilter.None) || ((hitFlags & o.hitFlags) != 0);
            bool okTarget = (o.targetTags == CharacterTagFlags.None) || ((tTags & o.targetTags) != 0);

            if (okHit && okTarget)
                sum += o.value;
        }

        return sum;
    }

    public int ApplyDamageEnhance(int baseDamage, HitFilter hitFlags, CharacterBehaviour target)
    {
        if (baseDamage <= 0) return baseDamage;

        float sum = DamageEnhanceSum(hitFlags, target);
        if (sum == 0f) return baseDamage;

        float mul = 1f + (sum / 100f);
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * mul));
    }
    public float SkillTagCooldownHasteSum(SkillTagFlags tags)
    {
        float sum = 0f;

        for (int i = 0; i < equipmentOptionsRuntime.Count; i++)
        {
            var o = equipmentOptionsRuntime[i];
            if (o.type != EquipmentOptionType.SkillTagCooldownHaste) continue;

            if (o.skillTags == SkillTagFlags.None) continue;
            if ((tags & o.skillTags) != 0)
                sum += o.value;
        }

        return sum;
    }
    public void OnCharacterSkillCooldownChanged(bool visible, float fillAmount)
    {
        hpBarWorld?.OnCharacterSkillCooldownChanged(visible, fillAmount);
    }

    [System.Serializable]
    public struct ActionHitGizmo
    {
        public Vector3 center;
        public float radius;
    }

    [HideInInspector] public List<ActionHitGizmo> actionHitGizmos = new();

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (actionHitGizmos == null || actionHitGizmos.Count == 0) return;

        Gizmos.color = new Color(1f, 1f, 0f, 0.22f);
        for (int i = 0; i < actionHitGizmos.Count; i++)
            Gizmos.DrawSphere(actionHitGizmos[i].center, actionHitGizmos[i].radius);

        Gizmos.color = new Color(1f, 1f, 0f, 0.9f);
        for (int i = 0; i < actionHitGizmos.Count; i++)
            Gizmos.DrawWireSphere(actionHitGizmos[i].center, actionHitGizmos[i].radius);
    }
}
