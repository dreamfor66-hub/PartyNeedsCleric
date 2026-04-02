using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Outgame/Equipment/EquipmentOptionPool")]
public class EquipmentOptionPool : ScriptableObject
{
    public List<EquipmentOptionData> options = new List<EquipmentOptionData>();

    public Dictionary<string, EquipmentOptionData> BuildMap()
    {
        var map = new Dictionary<string, EquipmentOptionData>();

        for (int i = 0; i < options.Count; i++)
        {
            var o = options[i];
            map[o.id] = o;
        }

        return map;
    }
}
