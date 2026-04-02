using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Outgame/StatIcons")]
public class StatIcons : ScriptableObject
{
    static StatIcons _instance;
    public static StatIcons Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<StatIcons>("StatIcons"); // Resources/StatIcons.asset
            return _instance;
        }
    }

    [Serializable]
    public struct Entry
    {
        [HorizontalGroup("Row"), HideLabel]
        public EquipmentStatType type;

        [HorizontalGroup("Row"), HideLabel]
        [PreviewField(48)]
        public Sprite icon;
    }

    [TableList(ShowIndexLabels = false)]
    public List<Entry> entries = new List<Entry>();

    public Sprite GetIcon(EquipmentStatType type)
    {
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].type == type)
                return entries[i].icon;

        return null;
    }
}
