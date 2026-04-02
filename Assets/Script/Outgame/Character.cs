using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Character
{
    // identity / base
    public string instanceId;
    public string dataId;

    [NonSerialized] public CharacterData Data;

    // instance values
    public string nickname;
    public int level = 1;

    public int atk;
    public int hp;

    // loadout (instance holds its own)
    public Equipment[] equippedEquipments = new Equipment[6];   // 0:Weapon 1:Head 2:Body 3:Gloves 4:Ring1 5:Ring2
    public string[] skillIds = new string[3];

    public void EnsureIds()
    {
        if (string.IsNullOrEmpty(instanceId))
            instanceId = Guid.NewGuid().ToString("N");
    }

    public void Init(CharacterData data, string forcedNickname = null, int startLevel = 1)
    {
        Data = data;
        dataId = data != null ? data.name : dataId;

        EnsureIds();

        nickname = string.IsNullOrEmpty(forcedNickname) ? CharacterNameGenerator.Roll() : forcedNickname;
        level = Mathf.Max(1, startLevel);

        if (equippedEquipments == null || equippedEquipments.Length != 6)
            equippedEquipments = new Equipment[6];

        if (skillIds == null || skillIds.Length != 3)
            skillIds = new string[3];

        RecalcStats();
    }

    public void BindData(PlayableDataTable table)
    {
        if (table == null) return;
        if (string.IsNullOrEmpty(dataId)) return;

        if (Data == null)
        {
            // commanders + characters 둘 다에서 찾음
            if (table.commanders != null)
            {
                for (int i = 0; i < table.commanders.Count; i++)
                {
                    var cd = table.commanders[i];
                    if (cd != null && cd.name == dataId) { Data = cd; break; }
                }
            }

            if (Data == null && table.characters != null)
            {
                for (int i = 0; i < table.characters.Count; i++)
                {
                    var cd = table.characters[i];
                    if (cd != null && cd.name == dataId) { Data = cd; break; }
                }
            }
        }

        if (equippedEquipments == null || equippedEquipments.Length != 6)
            equippedEquipments = new Equipment[6];

        if (skillIds == null || skillIds.Length != 3)
            skillIds = new string[3];

        // 장비 Data 바인딩
        for (int i = 0; i < equippedEquipments.Length; i++)
        {
            var eq = equippedEquipments[i];
            if (eq == null) continue;
            if (eq.Data == null) eq.BindData();
        }

        RecalcStats();
    }



    public void RecalcStats()
    {
        if (Data == null)
        {
            atk = 0;
            hp = 0;
            return;
        }

        // base * (1 + level*10%)
        float mul = 1f + (level * 0.1f);
        atk = Mathf.RoundToInt(Data.baseAtk * mul);
        hp = Mathf.RoundToInt(Data.baseHp * mul);
    }
}

public static class CharacterNameGenerator
{
    private static readonly string[] names =
    {
        "네일","오르카","레베카","루카","비비","소렌","아샤","리온","테오","미나",
        "하린","세라","카일","에린","로건","리아","오스틴","이브","노아","리아나"
    };

    public static string Roll()
    {
        return names[UnityEngine.Random.Range(0, names.Length)];
    }
}
