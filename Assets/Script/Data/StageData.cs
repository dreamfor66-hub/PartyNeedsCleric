using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/StageData")]
public class StageData : ScriptableObject
{
    [TableList]
    public List<FloorData> floors;
}

[Serializable]
public class FloorData
{
    public List<MapData> maps;
    public float multiplier = 1f;
}
