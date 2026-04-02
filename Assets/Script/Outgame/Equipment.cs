using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Equipment
{
    // ===== identity / base =====
    public string instanceId;
    public string dataId;

    public EquipmentData Data;

    // ===== rolled base stats (EquipmentData의 2쌍 스탯) =====
    public int rolledStat1;
    public int rolledStat2;

    [SerializeField] private bool hasRolledStats = false;

    // ===== rolled random options (instance) =====
    // (기존 코드에서 equipment.options 로 접근하던 것 유지)
    public List<EquipmentOption> options = new List<EquipmentOption>();

    [SerializeField] private bool hasRolledOptions = false;

    // ==================================================
    // Ensure / Bind (기존 사용처 유지용)
    // ==================================================
    public void EnsureIds()
    {
        if (string.IsNullOrEmpty(instanceId))
            instanceId = Guid.NewGuid().ToString("N");

        if (string.IsNullOrEmpty(dataId) && Data != null)
        {
            // 프로젝트에 EquipmentData.id가 있는 전제
            dataId = Data.id;
        }
    }

    // 호출부가 eq.BindData() 형태로 쓰는 경우 대비
    public void BindData()
    {
        // 1) Resources에서 id 경로로 로드 시도
        var loaded = GameVariables.Instance.equipmentTable.list.Find(x => x.id == dataId);
        if (loaded != null)
        {
            Data = loaded;
            return;
        }

        // 2) 이미 로드된 에셋들에서 탐색 (에디터/런타임 공용)
        var all = Resources.FindObjectsOfTypeAll<EquipmentData>();
        for (int i = 0; i < all.Length; i++)
        {
            var d = all[i];
            if (d == null) continue;
            if (d.id == dataId)
            {
                Data = d;
                return;
            }
        }
    }

    // ==================================================
    // Init
    // ==================================================
    public Equipment() { }

    public Equipment(EquipmentData data)
    {
        Init(data);
    }

    public void Init(EquipmentData data)
    {
        Data = data;

        EnsureIds();

        // 획득/생성 시점에만 굴림 확정
        RollAtAcquire();
    }


    // ==================================================
    // Roll
    // ==================================================

    public void RollAtAcquire()
    {
        EnsureIds();
        BindData();

        if (Data == null) return;

        // ===== Stats roll (획득 시 1회) =====
        if (!hasRolledStats)
        {
            var prev = UnityEngine.Random.state;
            try
            {
                rolledStat1 = RollInt(Data.statValueRange1);
                rolledStat2 = RollInt(Data.statValueRange2);
            }
            finally
            {
                UnityEngine.Random.state = prev;
            }
            hasRolledStats = true;
        }

        // ===== Options roll (획득 시 1회) =====
        if (!hasRolledOptions)
        {
            options ??= new List<EquipmentOption>();
            options.Clear();

            var prev = UnityEngine.Random.state;
            try
            {
                var GV = GameVariables.Instance;
                RollFromPool(GV.equipmentOptionPoolRed, Data.RedMaxCount);
                RollFromPool(GV.equipmentOptionPoolBlue, Data.BlueMaxCount);
                RollFromPool(GV.equipmentOptionPoolGreen, Data.GreenMaxCount);
                RollFromPool(GV.equipmentOptionPoolYellow, Data.YellowMaxCount);
            }
            finally
            {
                UnityEngine.Random.state = prev;
            }
            hasRolledOptions = true;
        }
    }



    private void RollFromPool(EquipmentOptionPool pool, int maxCount)
    {
        if (pool == null) return;
        if (maxCount <= 0) return;
        if (pool.options == null || pool.options.Count == 0) return;

        int count = UnityEngine.Random.Range(0, maxCount + 1);
        if (count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            var def = pool.options[UnityEngine.Random.Range(0, pool.options.Count)];

            int v = RollInt(def.valueRange);

            options.Add(new EquipmentOption
            {
                id = def.id,

                optionType = def.optionType,
                value = v,

                statType = def.statType,
                skillTags = def.skillTags,
                hitFlags = def.hitFlags,
                targetTags = def.targetTags,
            });
        }
    }



    private int RollInt(Vector2 range)
    {
        int min = Mathf.RoundToInt(Mathf.Min(range.x, range.y));
        int max = Mathf.RoundToInt(Mathf.Max(range.x, range.y));
        return UnityEngine.Random.Range(min, max + 1);
    }

}

[Serializable]
public class EquipmentOption
{
    // 신규/표준 키
    public string id;

    public EquipmentOptionType optionType;

    public int value;

    // FlatStat
    public EquipmentStatType statType;

    // SkillTagCooldownHaste
    public SkillTagFlags skillTags;

    // DamageEnhance
    public HitFilter hitFlags;
    public CharacterTagFlags targetTags;
}
public enum EquipmentOptionType
{
    None = 0,

    FlatStat = 1,
    SkillTagCooldownHaste = 2,
    DamageEnhance = 3,
}