using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


[Serializable]
public class HitFxEntry
{
    public HitFxType type;
    public VfxObject vfxPrefab;
}

[CreateAssetMenu(menuName = "Game/HitFxTable")]
public class HitFxTable : ScriptableObject
{
    static HitFxTable _instance;
    public static HitFxTable Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<HitFxTable>("HitFxTable");
            return _instance;
        }
    }

    [TableList] public List<HitFxEntry> list = new();

    public VfxObject Get(HitFxType type)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i].type == type)
                return list[i].vfxPrefab;
        return null;
    }

    public void Spawn(HitFxType type, Vector3 pos)
    {
        if (type == HitFxType.None) return;

        var prefab = Get(type);
        if (prefab == null) return;

        Instantiate(prefab, pos, Quaternion.identity);
    }
}
