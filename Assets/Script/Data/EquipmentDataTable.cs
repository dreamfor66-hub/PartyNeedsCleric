using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Outgame/EquipmentDataTable")]
public class EquipmentDataTable : ScriptableObject
{
    public List<EquipmentData> list = new List<EquipmentData>();

    public EquipmentData Resolve(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < list.Count; i++)
        {
            var d = list[i];
            if (d != null && d.id == id) return d;
        }
        return null;
    }
}